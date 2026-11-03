using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TDTS.Bots;
using TDTS.Data;

namespace TDTS.Managers
{
    /// <summary>
    /// Takes the turns of every seat no person is sitting in (offline: everyone but you;
    /// online: the empty seats). Each bot seat gets a strategy (Aggressive, Passive,
    /// Human-like or Random) and a BotBrain that ranks its moves; this just plays them
    /// out with a short "thinking" pause. Runs only where GameManager runs the rules:
    /// offline, or on the online host. GameManager adds it automatically if it's missing.
    /// </summary>
    public class BotController : MonoBehaviour
    {
        public static BotController Current { get; private set; }

        [SerializeField] private float thinkSeconds = 1.2f;

        [Tooltip("Strategy for each bot seat, in turn order. Repeats if there are more bots than entries.")]
        [SerializeField] private BotStrategy[] seatStrategies =
            { BotStrategy.Aggressive, BotStrategy.Passive, BotStrategy.HumanLike };

        [Tooltip("Write each bot's reasoning to the Console (it can mention hidden cards, so it isn't shown in the game).")]
        [SerializeField] private bool logThinking = true;

        private GameManager gm;
        private LiveBotTable table;
        private readonly Dictionary<PlayerData, BotBrain> brains = new Dictionary<PlayerData, BotBrain>();
        private readonly System.Random rng = new System.Random();
        private int handledTurn = -1;

        private void Awake() => Current = this;

        private void Start()
        {
            gm = GameManager.Instance;
            if (gm == null) return;
            table = new LiveBotTable(gm);

            gm.OnTurnChanged += HandleTurnChanged;
            gm.OnTradeOffered += HandleTradeOffered;
            gm.OnPeeked += HandlePeeked;
            gm.OnFlashlightSeen += HandleFlashlightSeen;
            gm.OnStateChanged += HandleStateChanged;
            HandleTurnChanged(); // the first turn may have started before we subscribed
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
            if (gm == null) return;
            gm.OnTurnChanged -= HandleTurnChanged;
            gm.OnTradeOffered -= HandleTradeOffered;
            gm.OnPeeked -= HandlePeeked;
            gm.OnFlashlightSeen -= HandleFlashlightSeen;
            gm.OnStateChanged -= HandleStateChanged;
        }

        /// <summary>The bot seat's brain, created the first time it's needed.</summary>
        private BotBrain BrainFor(PlayerData bot)
        {
            if (brains.TryGetValue(bot, out BotBrain brain)) return brain;

            int botNumber = 0;
            foreach (PlayerData p in gm.Players)
            {
                if (p == bot) break;
                if (!gm.IsHuman(p)) botNumber++;
            }
            BotStrategy strategy = seatStrategies.Length == 0
                ? BotStrategy.HumanLike
                : seatStrategies[botNumber % seatStrategies.Length];

            brain = new BotBrain(BotProfile.For(strategy), rng);
            brains[bot] = brain;
            return brain;
        }

        /// <summary>" (Aggressive bot)" for the seat labels, or "" for people.</summary>
        public string TagFor(PlayerData player) =>
            player == null || gm == null || gm.IsHuman(player) ? "" : $" ({BrainFor(player).Profile.Label} bot)";

        // ---------- Taking a turn ----------

        private void HandleTurnChanged()
        {
            PlayerData active = gm.ActivePlayer;
            if (active == null || gm.IsHuman(active) || handledTurn == gm.TurnNumber) return;

            handledTurn = gm.TurnNumber;
            StartCoroutine(TakeTurn(active, gm.TurnNumber));
        }

        private IEnumerator TakeTurn(PlayerData bot, int turn)
        {
            BotBrain brain = BrainFor(bot);
            brain.NewTurn();

            while (gm.TurnNumber == turn)
            {
                yield return new WaitForSeconds(GameSettings.BotThinkSeconds(thinkSeconds));
                if (gm.TurnNumber != turn) yield break;
                if (gm.PendingTrade != null) continue; // wait for the answer

                int actionsBefore = gm.ActionsLeft;
                BotMove played = PlayBestMove(bot);
                if (played == null)
                {
                    if (gm.TurnNumber == turn) yield break; // nothing worked; leave it
                    continue;
                }

                // A refused trade gives the action back, so don't ask the same player again this turn
                if (played.kind == MoveKind.Trade)
                {
                    while (gm.PendingTrade != null && gm.TurnNumber == turn) yield return null;
                    if (gm.TurnNumber == turn && gm.ActionsLeft == actionsBefore) brain.NoteTradeRefused(played.target);
                }
            }
        }

        /// <summary>Tries the brain's moves best first until the game accepts one. Returns the move played, or null.</summary>
        private BotMove PlayBestMove(PlayerData bot)
        {
            BotBrain brain = BrainFor(bot);
            foreach (BotMove move in brain.RankMoves(table, bot))
            {
                if (!TryMove(bot, move)) continue;
                if (logThinking) Debug.Log($"[Bot] {bot.playerName} ({brain.Profile.Label}): {move.kind} - {move.reason}");
                return move;
            }
            return null;
        }

        private bool TryMove(PlayerData bot, BotMove move) => move.kind switch
        {
            MoveKind.Scavenge => gm.DrawCard(bot),
            MoveKind.Trade => gm.OfferTrade(bot, move.handIndex, move.target, move.targetCardIndex),
            MoveKind.PlayCard => gm.PlayCard(bot, move.handIndex, move.target),
            MoveKind.Peek => gm.PeekCard(bot, move.handIndex),
            MoveKind.Steal => gm.StealFrom(bot, move.target, move.targetCardIndex),
            MoveKind.Swap => gm.SwapWith(bot, move.handIndex, move.target),
            MoveKind.Convert => false, // conversion is only a Balance Simulator test so far
            _ => gm.PassTurn(bot),
        };

        // ---------- Memory ----------

        private void HandlePeeked(PlayerData player, int handIndex)
        {
            if (gm.IsHuman(player) || handIndex < 0 || handIndex >= player.hand.Count) return;
            BrainFor(player).Memory.Saw(player.hand[handIndex]);
        }

        private void HandleFlashlightSeen(PlayerData player, CardData card)
        {
            if (!gm.IsHuman(player)) BrainFor(player).Memory.Saw(card);
        }

        private void HandleStateChanged(GameState previous, GameState next)
        {
            if (next != GameState.DayPhase) return;
            foreach (var pair in brains) pair.Value.OnNewDay(pair.Key);
        }

        // ---------- Answering trades ----------

        private void HandleTradeOffered()
        {
            GameManager.TradeOffer trade = gm.PendingTrade;
            if (trade == null || gm.IsHuman(trade.to)) return;
            StartCoroutine(AnswerTrade(trade));
        }

        private IEnumerator AnswerTrade(GameManager.TradeOffer trade)
        {
            yield return new WaitForSeconds(GameSettings.BotThinkSeconds(thinkSeconds));
            if (gm.PendingTrade != trade) yield break;

            BotBrain brain = BrainFor(trade.to);
            bool accept = brain.WantsTrade(trade.to, trade.card, trade.wanted);
            if (logThinking)
                Debug.Log($"[Bot] {trade.to.playerName} ({brain.Profile.Label}): {(accept ? "accepts" : "refuses")} " +
                          $"{trade.from.playerName}'s {trade.card.FrontName} for its {trade.wanted.FrontName}");
            gm.RespondToTrade(accept);
        }
    }
}

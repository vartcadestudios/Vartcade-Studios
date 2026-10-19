using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TDTS.Data;

namespace TDTS.Managers
{
    public enum GameState { Init, DayPhase, NightPhase, Sundown }

    // Spec V12
    public enum DayRule { Normal, TradingOnly, Blackout }

    // Spec G6 to G13: the 8 Rondo results
    public enum RondoResult { Purge, TradingOnly, HungerDisaster, Tornado, DoomsdayClock, Blackout, Outbreak, PeacefulDay }

    /// <summary>
    /// Core state machine for game flow, following the FC01 Digital Port Spec.
    /// All transitions go through TryTransition/ResolveX methods. Never set state directly.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Live Game State")]
        [SerializeField] private GameState currentState = GameState.Init;
        [SerializeField] private int currentDay = 1;
        [SerializeField] private int bunkerCapacity = 2;

        [Header("Players")]
        [Tooltip("Spec V4: turn order. Shuffled at setup.")]
        [SerializeField] private List<PlayerData> players = new List<PlayerData>();

        [Header("Actions")]
        [Tooltip("Rulebook: 1 action per Day turn, 2 per Night turn. Untick to test freely.")]
        [SerializeField] private bool limitActions = true;

        // The spec has no hand limit, but the hand row only has 6 slots for now.
        public const int MaxHandSize = 6;
        public const int StartingHandSize = 2;
        public const int MaxPlayers = 6;
        public const int RondoStartDay = 6;
        public const int SupplyToSurvive = 3;
        public const int PurgeCount = 5;

        [Header("Deck")]
        [SerializeField] private DeckRecipe deckRecipe = new DeckRecipe();
        [Tooltip("0 = a new random game every time. Any other number deals the same game again.")]
        [SerializeField] private int randomSeed = 0;

        [Header("Testing")]
        [Tooltip("PLACEHOLDER until online play: simple bots take the turns of every seat except yours.")]
        [SerializeField] private bool botsPlayOtherSeats = true;
        [Tooltip("Start the game on a later Day to test the Rondo (6+) or Sundown (10) quickly.")]
        [Range(1, 10)]
        [SerializeField] private int startOnDay = 1;

        private readonly List<CardData> deck = new List<CardData>();
        private readonly List<CardData> discardPile = new List<CardData>();
        private readonly List<CardData> removedFromGame = new List<CardData>();
        private readonly List<string> playLog = new List<string>();
        private System.Random rng;
        private PlayerData localPlayer;
        private PlayerData selectedTarget;
        private int selectedCardIndex = -1;

        // Spec V2, V6, V7, V11, V12
        private int sundownDay = 10;
        private int actionsLeft;
        private int peeksThisTurn;
        private DayRule dayRule = DayRule.Normal;
        private readonly List<RondoResult> rondoUsed = new List<RondoResult>();
        private RondoResult? todaysRondo;

        // Spec V4/V5: whose turn it is and who has already gone this phase
        private int activeIndex = -1;
        private int turnDirection = 1;
        private int turnNumber;
        private readonly HashSet<PlayerData> hadTurn = new HashSet<PlayerData>();
        private CardData stolenThisTurn;

        private TradeOffer pendingTrade;
        private float tradeOfferedAt;
        public const float TradeTimeoutSeconds = 30f; // spec R9

        /// <summary>Spec zone Z5: a card offered to another player, waiting for their answer.</summary>
        public class TradeOffer
        {
            public PlayerData from;
            public PlayerData to;
            public CardData card;
        }

        public GameState CurrentState => currentState;
        public int CurrentDay => currentDay;
        public int SundownDay => sundownDay;
        public int BunkerCapacity => bunkerCapacity;
        public int BunkerSeated => players.Count(p => p.isSeated);
        public IReadOnlyList<PlayerData> Players => players;
        public int DeckCount => deck.Count;
        public int DiscardCount => discardPile.Count;
        public bool LimitActions => limitActions;
        public int ActionsLeft => actionsLeft;
        public int PeeksThisTurn => peeksThisTurn;
        public PlayerData SelectedTarget => selectedTarget;
        public int SelectedCardIndex => selectedCardIndex;
        public IReadOnlyList<string> PlayLog => playLog;
        public PlayerData ActivePlayer => activeIndex >= 0 && activeIndex < players.Count ? players[activeIndex] : null;
        public bool IsLocalTurn => ActivePlayer != null && ActivePlayer == localPlayer;
        public int TurnNumber => turnNumber;
        public TradeOffer PendingTrade => pendingTrade;
        public DayRule CurrentDayRule => dayRule;
        public RondoResult? TodaysRondo => todaysRondo;
        public bool TurnOrderReversed => turnDirection < 0;

        /// <summary>Night, or a Blackout Day: other players can't see what the active player does.</summary>
        public bool EyesClosed =>
            currentState == GameState.NightPhase ||
            (currentState == GameState.DayPhase && dayRule == DayRule.Blackout);

        // The player sitting at this device. Placeholder: the first test player registered.
        public PlayerData LocalPlayer => localPlayer;

        /// <summary>The player sitting this many seats after you in turn order (1 = next), or null.</summary>
        public PlayerData GetPlayerAfterLocal(int seatsAfter)
        {
            if (localPlayer == null || seatsAfter <= 0 || seatsAfter >= players.Count) return null;
            int me = players.IndexOf(localPlayer);
            return players[(me + seatsAfter) % players.Count];
        }

        public event Action<GameState, GameState> OnStateChanged;
        public event Action OnHandsChanged;
        public event Action<CardData> OnCardPlayed;
        public event Action OnDeckChanged;
        public event Action OnPlayLogChanged;
        public event Action OnTargetChanged; // also fires when the selected hand card changes
        public event Action OnTurnChanged;
        public event Action OnTradeOffered;
        public event Action<RondoResult> OnRondoSpun;
        /// <summary>The local player learned the back of a card (Peek, Steal-Peek or Flashlight).</summary>
        public event Action<PlayerData, CardData> OnCardRevealedToYou;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (botsPlayOtherSeats && GetComponent<BotController>() == null)
                gameObject.AddComponent<BotController>();

            SetUpGame();
            TryTransition(GameState.DayPhase);
        }

        private void Update()
        {
            // R9: no answer in time counts as a decline
            if (pendingTrade != null && Time.time - tradeOfferedAt > TradeTimeoutSeconds)
            {
                AddLog("No answer in time.");
                RespondToTrade(false, -1);
            }
        }

        public float TradeSecondsLeft => pendingTrade == null ? 0f : Mathf.Max(0f, TradeTimeoutSeconds - (Time.time - tradeOfferedAt));

        /// <summary>Spec setup S1 to S5.</summary>
        private void SetUpGame()
        {
            rng = randomSeed == 0 ? new System.Random() : new System.Random(randomSeed);

            // PLACEHOLDER: 4 test players on this device until the online lobby
            if (players.Count == 0)
            {
                RegisterPlayer("You");
                RegisterPlayer("Player 2");
                RegisterPlayer("Player 3");
                RegisterPlayer("Player 4");
            }
            localPlayer = players[0];

            // S1: random seating
            DeckBuilder.Shuffle(players, rng);

            // S2, S3: pick the Key and shuffle it into the 72 cards
            deck.Clear();
            discardPile.Clear();
            removedFromGame.Clear();
            deck.AddRange(DeckBuilder.BuildDrawPile(deckRecipe, rng, removedFromGame));

            // S4: deal one card at a time around the table
            foreach (var p in players) p.hand.Clear();
            for (int round = 0; round < StartingHandSize; round++)
                foreach (var p in players)
                    GiveCard(p, TakeTopCard());

            // S5
            currentDay = Mathf.Clamp(startOnDay, 1, 10);
            sundownDay = 10;
            rondoUsed.Clear();

            OnDeckChanged?.Invoke();
            OnHandsChanged?.Invoke();
        }

        public void RegisterPlayer(string playerName)
        {
            if (players.Count >= MaxPlayers) return;
            players.Add(new PlayerData(playerName));
        }

        // ---------- Cards ----------

        private CardData TakeTopCard()
        {
            CardData card = deck[deck.Count - 1];
            deck.RemoveAt(deck.Count - 1);
            return card;
        }

        /// <summary>Puts a card in a hand. The new owner hasn't seen its back yet (spec R6).</summary>
        private static void GiveCard(PlayerData to, CardData card)
        {
            card.peeked = false;
            to.hand.Add(card);
        }

        private CardData TakeRandomCard(PlayerData from)
        {
            int i = rng.Next(from.hand.Count);
            CardData card = from.hand[i];
            from.hand.RemoveAt(i);
            return card;
        }

        // ---------- Log ----------

        private void AddLog(string line)
        {
            playLog.Add(line);
            if (playLog.Count > 20) playLog.RemoveAt(0);
            Debug.Log($"[GameManager] {line}");
            OnPlayLogChanged?.Invoke();
        }

        /// <summary>
        /// Logs a player's action. With eyes closed (Night, Blackout) only the acting player
        /// sees the details; everyone else just sees that something happened (spec C1).
        /// </summary>
        private void LogAction(PlayerData actor, string details)
        {
            if (EyesClosed && actor != localPlayer)
                AddLog($"{actor.playerName} did something in the dark.");
            else
                AddLog(details);
        }

        /// <summary>Hand contents moved, so a selected card index may now point at a different card.</summary>
        private void NotifyHandsChanged()
        {
            selectedCardIndex = -1;
            OnHandsChanged?.Invoke();
        }

        // ---------- Action checks ----------

        /// <summary>Common checks for any player action: right phase, your turn, AP left.</summary>
        private bool CanAct(PlayerData player, GameState phase, string actionName, bool isPeek = false)
        {
            if (player == null) return false;

            if (currentState != phase)
            {
                AddLog($"{actionName} is a {(phase == GameState.DayPhase ? "Day" : "Night")} action.");
                return false;
            }
            if (player != ActivePlayer)
            {
                AddLog($"It's {ActivePlayer?.playerName}'s turn.");
                return false;
            }
            if (pendingTrade != null)
            {
                AddLog("Waiting for an answer to the trade.");
                return false;
            }
            if (!HasAction()) return false;

            // R4: the last Night action must be a peek if you haven't peeked yet
            if (phase == GameState.NightPhase && !isPeek && MustPeekNow(player))
            {
                AddLog("You must peek at least once tonight. Tap one of your cards.");
                return false;
            }
            return true;
        }

        private bool MustPeekNow(PlayerData player) =>
            limitActions && actionsLeft == 1 && peeksThisTurn == 0 && player.hand.Count > 0;

        private bool HasAction()
        {
            if (!limitActions || actionsLeft > 0) return true;
            AddLog("No actions left this turn.");
            return false;
        }

        /// <summary>Spends 1 AP. With no AP left, the turn passes to the next player.</summary>
        private void SpendAction()
        {
            if (!limitActions) return;
            actionsLeft = Mathf.Max(0, actionsLeft - 1);
            if (actionsLeft == 0) EndTurn();
        }

        // ---------- Day actions ----------

        /// <summary>P3 Scavenge: draws the top card of the pool (Day).</summary>
        public bool DrawCard(PlayerData player)
        {
            if (!CanAct(player, GameState.DayPhase, "Scavenge")) return false;

            if (dayRule == DayRule.TradingOnly)
            {
                AddLog("Trading Only today: no Scavenge.");
                return false;
            }
            if (deck.Count == 0)
            {
                AddLog("The draw pile is empty.");
                return false;
            }
            if (player.hand.Count >= MaxHandSize)
            {
                AddLog("Your hand is full.");
                return false;
            }

            GiveCard(player, TakeTopCard());
            LogAction(player, $"{player.playerName} scavenged a card.");
            OnDeckChanged?.Invoke();
            NotifyHandsChanged();
            SpendAction();
            return true;
        }

        /// <summary>P1 Offer trade: puts 1 card on offer to another player (Day). They answer with RespondToTrade.</summary>
        public bool OfferTrade(PlayerData from, int handIndex, PlayerData to)
        {
            if (!CanAct(from, GameState.DayPhase, "Trade")) return false;

            if (to == null || to == from)
            {
                AddLog("Tap a player first, then Trade.");
                return false;
            }
            if (handIndex < 0 || handIndex >= from.hand.Count)
            {
                AddLog("Tap one of your cards first, then Trade.");
                return false;
            }
            if (to.hand.Count == 0)
            {
                AddLog($"{to.playerName} has no cards to trade.");
                return false;
            }

            CardData card = from.hand[handIndex];
            from.hand.RemoveAt(handIndex);
            pendingTrade = new TradeOffer { from = from, to = to, card = card };
            tradeOfferedAt = Time.time;

            LogAction(from, $"{from.playerName} offers {to.playerName} a {card.FrontName}.");
            NotifyHandsChanged();
            OnTradeOffered?.Invoke();
            return true;
        }

        /// <summary>
        /// P2 Accept/decline trade. Accepting gives back the card at handIndex and costs the
        /// offerer 1 AP; declining returns the offered card and the offerer keeps their AP (spec R9).
        /// </summary>
        public void RespondToTrade(bool accept, int handIndex)
        {
            if (pendingTrade == null) return;

            TradeOffer trade = pendingTrade;
            pendingTrade = null;

            if (!accept || handIndex < 0 || handIndex >= trade.to.hand.Count)
            {
                trade.from.hand.Add(trade.card); // still theirs, so they keep what they knew about it
                AddLog($"{trade.to.playerName} declined the trade.");
                NotifyHandsChanged();
                return;
            }

            CardData given = trade.to.hand[handIndex];
            trade.to.hand.RemoveAt(handIndex);
            GiveCard(trade.to, trade.card);
            GiveCard(trade.from, given);

            LogAction(trade.from, $"{trade.from.playerName} traded a {trade.card.FrontName} for {trade.to.playerName}'s {given.FrontName}.");
            NotifyHandsChanged();
            SpendAction();
        }

        // ---------- Night actions ----------

        /// <summary>P6 Peek / P8 Steal-Peek: look at the back of one of your own cards (Night).</summary>
        public bool PeekCard(PlayerData player, int handIndex)
        {
            if (player == null || handIndex < 0 || handIndex >= player.hand.Count) return false;
            if (!CanAct(player, GameState.NightPhase, "Peek", isPeek: true)) return false;

            CardData card = player.hand[handIndex];
            bool stealPeek = card == stolenThisTurn && peeksThisTurn == 0; // R7
            card.peeked = true;
            peeksThisTurn++;

            // Only the owner learns the back, so the details go to their screen only
            string what = $"{card.FrontName} is really {card.BackName}";
            LogAction(player, stealPeek
                ? $"{player.playerName} Steal-Peeked: the stolen {what}."
                : $"{player.playerName} peeked: {what}.");
            if (player == localPlayer) OnCardRevealedToYou?.Invoke(player, card);

            OnHandsChanged?.Invoke(); // selection stays: no card moved
            SpendAction();
            return true;
        }

        /// <summary>P7 Steal: takes a random face-down card from the target's hand (Night).</summary>
        public bool StealFrom(PlayerData thief, PlayerData victim)
        {
            if (!CanAct(thief, GameState.NightPhase, "Steal")) return false;

            if (victim == null || victim == thief)
            {
                AddLog("Tap a player first, then Steal.");
                return false;
            }
            if (victim.hand.Count == 0)
            {
                AddLog($"{victim.playerName} has no cards.");
                return false;
            }
            if (thief.hand.Count >= MaxHandSize)
            {
                AddLog("Your hand is full.");
                return false;
            }

            CardData stolen = TakeRandomCard(victim);
            GiveCard(thief, stolen);
            stolenThisTurn = stolen;

            LogAction(thief, $"{thief.playerName} stole a {stolen.FrontName} from {victim.playerName}.");
            NotifyHandsChanged();
            SpendAction();
            return true;
        }

        /// <summary>P9 Swap: your chosen card for a random face-down card from the target (Night).</summary>
        public bool SwapWith(PlayerData player, int handIndex, PlayerData target)
        {
            if (!CanAct(player, GameState.NightPhase, "Swap")) return false;

            if (target == null || target == player)
            {
                AddLog("Tap a player first, then Swap.");
                return false;
            }
            if (handIndex < 0 || handIndex >= player.hand.Count)
            {
                AddLog("Tap one of your peeked cards to pick it, then Swap.");
                return false;
            }
            if (target.hand.Count == 0)
            {
                AddLog($"{target.playerName} has no cards.");
                return false;
            }

            CardData mine = player.hand[handIndex];
            player.hand.RemoveAt(handIndex);
            CardData theirs = TakeRandomCard(target);
            GiveCard(player, theirs);
            GiveCard(target, mine);

            LogAction(player, $"{player.playerName} swapped a {mine.FrontName} for {target.playerName}'s {theirs.FrontName}.");
            NotifyHandsChanged();
            SpendAction();
            return true;
        }

        // ---------- Playing action cards ----------

        /// <summary>
        /// P4 / P10: plays a card from a player's hand. Morning cards are played by their front
        /// in the Day, Night cards by their back at Night once the owner has peeked.
        /// Some cards need a target player (Swap Deck, Flashlight, the two Targeted Skips).
        /// </summary>
        public bool PlayCard(PlayerData player, int handIndex, PlayerData target = null)
        {
            if (player == null || handIndex < 0 || handIndex >= player.hand.Count) return false;

            CardData card = player.hand[handIndex];
            bool isDay = currentState == GameState.DayPhase;
            string cardName = isDay ? card.FrontName : card.BackName;

            bool allowed =
                (isDay && card.HasMorningFront) ||
                (currentState == GameState.NightPhase && card.HasNightBack && card.peeked);

            if (!allowed)
            {
                AddLog(currentState == GameState.NightPhase && card.HasNightBack
                    ? "Peek at a Night card before playing it."
                    : $"{cardName} can't be played during {currentState}.");
                return false;
            }
            if (!CanAct(player, currentState, "Play card")) return false;
            if (isDay && dayRule == DayRule.TradingOnly)
            {
                AddLog("Trading Only today: no Action Cards.");
                return false;
            }
            if (!NeedsTarget(card, isDay))
            {
                target = null;
            }
            else if (target == null || target == player)
            {
                AddLog($"Tap a player first, then play {cardName}.");
                return false;
            }

            // Flashlight stays in hand until its batteries run out (R10)
            bool keepInHand = !isDay && card.nightAction == NightActionType.Flashlight && card.batteries > 1;
            if (!keepInHand)
            {
                player.hand.RemoveAt(handIndex);
                discardPile.Add(card);
            }

            LogAction(player, target != null
                ? $"{player.playerName} played {cardName} on {target.playerName}."
                : $"{player.playerName} played {cardName}.");

            if (isDay) ResolveMorningCard(card.morningAction, player, target);
            else ResolveNightCard(card, player, target);

            OnCardPlayed?.Invoke(card);
            OnDeckChanged?.Invoke();
            NotifyHandsChanged();
            SpendAction();
            return true;
        }

        public static bool NeedsTarget(CardData card, bool isDay) =>
            isDay ? card.morningAction == MorningActionType.SwapDeck : card.HasNightBack;

        /// <summary>G19 to G23.</summary>
        private void ResolveMorningCard(MorningActionType action, PlayerData player, PlayerData target)
        {
            switch (action)
            {
                case MorningActionType.SwapDeck:
                    (player.hand, target.hand) = (target.hand, player.hand);
                    foreach (var c in player.hand) c.peeked = false;
                    foreach (var c in target.hand) c.peeked = false;
                    AddLog($"{player.playerName} and {target.playerName} swapped their whole hands.");
                    break;

                case MorningActionType.ReverseTurn:
                    turnDirection = -turnDirection;
                    AddLog("Turn order reversed!");
                    break;

                case MorningActionType.SkipNextPlayer:
                    PlayerData next = players[(activeIndex + turnDirection + players.Count) % players.Count];
                    next.daySkip = true;
                    AddLog($"{next.playerName} will lose their next Day turn.");
                    break;

                case MorningActionType.EveryonePlusOne:
                    int dealt = 0;
                    foreach (PlayerData p in PlayersInTurnOrderFrom(player))
                    {
                        if (deck.Count == 0) break;
                        if (p.hand.Count >= MaxHandSize) continue;
                        GiveCard(p, TakeTopCard());
                        dealt++;
                    }
                    AddLog($"Everyone +1 Card: {dealt} cards handed out.");
                    break;

                case MorningActionType.EveryoneSwitches:
                    var hands = players.Select(p => p.hand).ToList();
                    for (int i = 0; i < players.Count; i++)
                    {
                        int to = (i + turnDirection + players.Count) % players.Count;
                        players[to].hand = hands[i];
                        foreach (var c in hands[i]) c.peeked = false;
                    }
                    AddLog("Everyone passed their hand to the next player.");
                    break;
            }
        }

        /// <summary>G24 to G26.</summary>
        private void ResolveNightCard(CardData card, PlayerData player, PlayerData target)
        {
            switch (card.nightAction)
            {
                case NightActionType.Flashlight:
                    card.batteries--;
                    if (target.hand.Count == 0) break;
                    CardData seen = target.hand[rng.Next(target.hand.Count)];
                    if (player == localPlayer)
                    {
                        AddLog($"Flashlight: one of {target.playerName}'s cards is {seen.BackName}. ({card.batteries} batteries left)");
                        OnCardRevealedToYou?.Invoke(target, seen);
                    }
                    break;

                case NightActionType.TargetedNightSkip:
                    target.nightSkip = true;
                    if (!EyesClosed || player == localPlayer) AddLog($"{target.playerName} will lose their next Night turn.");
                    break;

                case NightActionType.TargetedDaySkip:
                    target.daySkip = true;
                    if (!EyesClosed || player == localPlayer) AddLog($"{target.playerName} will lose their next Day turn.");
                    break;
            }
        }

        private IEnumerable<PlayerData> PlayersInTurnOrderFrom(PlayerData first)
        {
            int start = players.IndexOf(first);
            for (int step = 0; step < players.Count; step++)
                yield return players[(start + step * turnDirection + players.Count * players.Count) % players.Count];
        }

        // ---------- Ending a turn ----------

        /// <summary>P5 Pass (Day) / P11 End Night turn: ends the turn. At Night you must have peeked (R4).</summary>
        public bool PassTurn(PlayerData player)
        {
            if (player == null || player != ActivePlayer || pendingTrade != null) return false;

            if (currentState == GameState.NightPhase && peeksThisTurn == 0 && player.hand.Count > 0 && limitActions)
            {
                AddLog("You must peek at least once before ending your Night turn.");
                return false;
            }

            LogAction(player, $"{player.playerName} {(currentState == GameState.NightPhase ? "ended their turn" : "passed")}.");
            EndTurn();
            return true;
        }

        // ---------- Local player helpers (used by the UI) ----------

        public void DrawForLocalPlayer() { DrawCard(LocalPlayer); }
        public void StealForLocalPlayer() { StealFrom(LocalPlayer, selectedTarget); }
        public void SwapSelectedForLocalPlayer() { SwapWith(LocalPlayer, selectedCardIndex, selectedTarget); }
        public void PassForLocalPlayer() { PassTurn(LocalPlayer); }
        public void TradeSelectedForLocalPlayer() { OfferTrade(LocalPlayer, selectedCardIndex, selectedTarget); }
        // Kept so the old Skip button binding still compiles; at Night it now ends your turn.
        public void SkipForLocalPlayer() { PassTurn(LocalPlayer); }

        public void PlaySelectedForLocalPlayer()
        {
            if (selectedCardIndex < 0)
            {
                AddLog("Tap one of your cards first, then Play card.");
                return;
            }
            PlayCard(LocalPlayer, selectedCardIndex, selectedTarget);
        }

        /// <summary>The local player answers a trade a bot offered them: accept gives away their selected card.</summary>
        public void AnswerTradeForLocalPlayer(bool accept)
        {
            if (pendingTrade == null || pendingTrade.to != localPlayer) return;
            if (accept && selectedCardIndex < 0)
            {
                AddLog("Tap the card you'll give in return, then Accept.");
                return;
            }
            RespondToTrade(accept, selectedCardIndex);
        }

        // ---------- Selection ----------

        /// <summary>Tap a player zone to pick who you'll trade with, steal from or target. Tap again to clear.</summary>
        public void SelectTarget(PlayerData player)
        {
            if (player == null || player == LocalPlayer) return;
            selectedTarget = (selectedTarget == player) ? null : player;
            OnTargetChanged?.Invoke();
        }

        /// <summary>Tap one of your cards to pick it for Trade, Swap or Play card. Tap again to clear.</summary>
        public void SelectCard(int handIndex)
        {
            selectedCardIndex = (selectedCardIndex == handIndex) ? -1 : handIndex;
            OnTargetChanged?.Invoke();
        }

        // ---------- Turn flow ----------

        /// <summary>Steps 2 and 6: the first player in turn order starts the phase.</summary>
        private void StartPhaseTurns()
        {
            hadTurn.Clear();
            activeIndex = turnDirection > 0 ? 0 : players.Count - 1;
            BeginTurn();
        }

        /// <summary>Steps 3 and 7 (G14, G15): skip check (R8), then refill AP for the active player.</summary>
        private void BeginTurn()
        {
            PlayerData p = ActivePlayer;
            hadTurn.Add(p);
            turnNumber++;
            selectedTarget = null;
            selectedCardIndex = -1;
            stolenThisTurn = null;
            peeksThisTurn = 0;

            bool isDay = currentState == GameState.DayPhase;
            if ((isDay && p.daySkip) || (!isDay && p.nightSkip))
            {
                if (isDay) p.daySkip = false;
                else p.nightSkip = false;
                AddLog($"{p.playerName} loses this {(isDay ? "Day" : "Night")} turn.");
                actionsLeft = 0;
                OnTurnChanged?.Invoke();
                EndTurn();
                return;
            }

            actionsLeft = isDay ? 1 : 2;
            OnTargetChanged?.Invoke();
            OnTurnChanged?.Invoke();
        }

        /// <summary>Steps 5 and 9 (G16): next player who hasn't gone this phase, or end the phase.</summary>
        private void EndTurn()
        {
            if (currentState != GameState.DayPhase && currentState != GameState.NightPhase) return;

            for (int step = 1; step < players.Count; step++)
            {
                int i = ((activeIndex + step * turnDirection) % players.Count + players.Count) % players.Count;
                if (hadTurn.Contains(players[i])) continue;

                activeIndex = i;
                BeginTurn();
                return;
            }

            if (currentState == GameState.DayPhase) ResolveDayPhase();
            else ResolveNightPhase();
        }

        // ---------- Phase flow ----------

        public void ResolveDayPhase()
        {
            if (currentState != GameState.DayPhase) return;
            TryTransition(GameState.NightPhase);
        }

        /// <summary>Step 10 (G17): next Day, or Sundown once Day reaches the Sundown Day.</summary>
        public void ResolveNightPhase()
        {
            if (currentState != GameState.NightPhase) return;

            if (currentDay >= sundownDay)
            {
                TryTransition(GameState.Sundown);
            }
            else
            {
                currentDay++;
                TryTransition(GameState.DayPhase);
            }
        }

        // ---------- Rondo (spec G5 to G13) ----------

        /// <summary>Step 1: from Day 6, spin the Rondo for a result that hasn't come up yet.</summary>
        private void SpinRondo()
        {
            var options = ((RondoResult[])Enum.GetValues(typeof(RondoResult))).Where(r => !rondoUsed.Contains(r)).ToList();
            if (options.Count == 0) return;

            RondoResult result = options[rng.Next(options.Count)];
            rondoUsed.Add(result);
            todaysRondo = result;

            switch (result)
            {
                case RondoResult.Purge:
                    int purged = Mathf.Min(PurgeCount, deck.Count);
                    for (int i = 0; i < purged; i++) removedFromGame.Add(TakeTopCard());
                    AddLog($"RONDO - Purge: {purged} cards removed from the deck for good.");
                    break;

                case RondoResult.TradingOnly:
                    dayRule = DayRule.TradingOnly;
                    AddLog("RONDO - Trading Only: no Scavenge and no Action Cards today.");
                    break;

                case RondoResult.HungerDisaster:
                    int lostFood = LoseOneCardEach(c => c.IsRealSupply && c.backSupply == SupplyKind.Food);
                    AddLog($"RONDO - Hunger Disaster: {lostFood} Food cards lost.");
                    break;

                case RondoResult.Tornado:
                    var pool = players.SelectMany(p => p.hand).ToList();
                    var sizes = players.Select(p => p.hand.Count).ToList();
                    DeckBuilder.Shuffle(pool, rng);
                    int next = 0;
                    for (int i = 0; i < players.Count; i++)
                    {
                        players[i].hand = new List<CardData>();
                        for (int n = 0; n < sizes[i]; n++) GiveCard(players[i], pool[next++]);
                    }
                    AddLog("RONDO - Tornado: everyone's cards were scattered and re-dealt.");
                    break;

                case RondoResult.DoomsdayClock:
                    sundownDay = Mathf.Max(currentDay, sundownDay - 1);
                    AddLog($"RONDO - Doomsday Clock: Sundown now comes on Day {sundownDay}.");
                    break;

                case RondoResult.Blackout:
                    dayRule = DayRule.Blackout;
                    AddLog("RONDO - Blackout: eyes stay closed during the Day too.");
                    break;

                case RondoResult.Outbreak:
                    int lostMeds = LoseOneCardEach(c => c.IsRealSupply &&
                        (c.backSupply == SupplyKind.Medicine || c.backSupply == SupplyKind.Entertainment));
                    AddLog($"RONDO - Outbreak: {lostMeds} Medicine/Entertainment cards lost.");
                    break;

                case RondoResult.PeacefulDay:
                    AddLog("RONDO - Peaceful Day: nothing happens.");
                    break;
            }

            OnRondoSpun?.Invoke(result);
            OnDeckChanged?.Invoke();
            NotifyHandsChanged();
        }

        /// <summary>Each player discards 1 random card matching the rule, if they have one. Returns how many were lost.</summary>
        private int LoseOneCardEach(Func<CardData, bool> matches)
        {
            int lost = 0;
            foreach (PlayerData p in players)
            {
                var candidates = p.hand.Where(matches).ToList();
                if (candidates.Count == 0) continue;

                CardData card = candidates[rng.Next(candidates.Count)];
                p.hand.Remove(card);
                discardPile.Add(card);
                lost++;
            }
            return lost;
        }

        // ---------- Sundown (spec G18, R1, R2) ----------

        private void DecideSurvivors()
        {
            foreach (var p in players)
            {
                p.isSeated = false;
                p.resultReason = $"only {p.RealSupplyCount} real Supply";
            }

            int seatsLeft = bunkerCapacity;

            // R1: the Key holder takes a seat no matter what
            PlayerData keyHolder = players.FirstOrDefault(p => p.HoldsKey);
            if (keyHolder != null)
            {
                keyHolder.isSeated = true;
                keyHolder.resultReason = "held the Bunker Key";
                seatsLeft--;
            }

            // Everyone else needs 3+ real Supply; the most Supply wins the remaining seats
            var qualifiers = players
                .Where(p => !p.isSeated && p.RealSupplyCount >= SupplyToSurvive)
                .OrderByDescending(p => p.RealSupplyCount)
                .ToList();

            int index = 0;
            while (seatsLeft > 0 && index < qualifiers.Count)
            {
                int supply = qualifiers[index].RealSupplyCount;
                var tied = qualifiers.Skip(index).TakeWhile(p => p.RealSupplyCount == supply).ToList();

                if (tied.Count <= seatsLeft)
                {
                    foreach (var p in tied)
                    {
                        p.isSeated = true;
                        p.resultReason = $"{supply} real Supply";
                    }
                    seatsLeft -= tied.Count;
                }
                else
                {
                    // R2: a tie for the last seat leaves it empty
                    foreach (var p in tied) p.resultReason = $"{supply} real Supply, tied for the last seat";
                    break;
                }
                index += tied.Count;
            }

            for (int i = index; i < qualifiers.Count; i++)
                if (!qualifiers[i].isSeated && !qualifiers[i].resultReason.Contains("tied"))
                    qualifiers[i].resultReason = $"{qualifiers[i].RealSupplyCount} real Supply, but the bunker was full";

            AddLog($"SUNDOWN. {BunkerSeated} of {players.Count} made it into the bunker.");
        }

        private void TryTransition(GameState next)
        {
            if (currentState == next) return;

            GameState previous = currentState;
            currentState = next;

            // Fresh phase: empty play space, nothing selected, no trade waiting.
            playLog.Clear();
            selectedTarget = null;
            selectedCardIndex = -1;
            if (pendingTrade != null)
            {
                pendingTrade.from.hand.Add(pendingTrade.card);
                pendingTrade = null;
            }

            if (next == GameState.DayPhase)
            {
                dayRule = DayRule.Normal;
                todaysRondo = null;
                AddLog($"Day {currentDay} of {sundownDay}. Turn order: {string.Join(" > ", OrderedNames())}");
                if (currentDay >= RondoStartDay) SpinRondo();
            }
            else if (next == GameState.NightPhase)
            {
                AddLog("Night falls. Eyes closed: each player takes 2 actions and must peek at least once.");
            }
            else if (next == GameState.Sundown)
            {
                DecideSurvivors();
            }

            Debug.Log($"[GameManager] State transition: {previous} -> {next} (Day {currentDay}, Sundown Day {sundownDay})");
            OnPlayLogChanged?.Invoke();
            OnTargetChanged?.Invoke();
            OnStateChanged?.Invoke(previous, next);

            if (next == GameState.DayPhase || next == GameState.NightPhase)
            {
                StartPhaseTurns();
            }
            else
            {
                activeIndex = -1;
                actionsLeft = 0;
                OnTurnChanged?.Invoke();
            }
        }

        private IEnumerable<string> OrderedNames()
        {
            if (turnDirection > 0) return players.Select(p => p.playerName);
            return Enumerable.Reverse(players).Select(p => p.playerName);
        }
    }
}

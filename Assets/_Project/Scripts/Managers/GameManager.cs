using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TDTS.Data;
using TDTS.Networking;

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
    /// Runs offline and on the online host only; an online client hands the table to ClientGameView.
    /// </summary>
    public class GameManager : MonoBehaviour, IGameView
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
        private readonly List<LogEntry> playLog = new List<LogEntry>();
        private System.Random rng;
        private PlayerData localPlayer;
        private readonly HashSet<PlayerData> humans = new HashSet<PlayerData>();
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
        private bool stoleOrSwapped; // one Steal or Swap a night

        private TradeOffer pendingTrade;
        private float tradeOfferedAt;
        public const float TradeTimeoutSeconds = 30f; // spec R9

        /// <summary>Spec zone Z5: a card offered to another player for one of theirs, waiting for their answer.</summary>
        public class TradeOffer
        {
            public PlayerData from;
            public PlayerData to;
            public CardData card;   // what's on offer
            public CardData wanted; // the card of theirs the offerer picked
        }

        /// <summary>
        /// One play log line. With a viewer set, only that player reads the text; everyone else
        /// reads forOthers, or nothing when it is null (spec C1).
        /// </summary>
        private struct LogEntry
        {
            public string text;
            public PlayerData viewer;
            public string forOthers;
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
        public bool StoleOrSwappedThisTurn => stoleOrSwapped;
        public PlayerData SelectedTarget => selectedTarget;
        public int SelectedCardIndex => selectedCardIndex;
        public IReadOnlyList<string> PlayLog => LogFor(localPlayer);
        public PlayerData ActivePlayer => activeIndex >= 0 && activeIndex < players.Count ? players[activeIndex] : null;
        public bool IsLocalTurn => ActivePlayer != null && ActivePlayer == localPlayer;
        public int TurnNumber => turnNumber;
        public TradeOffer PendingTrade => pendingTrade;
        public bool HasPendingTrade => pendingTrade != null;
        public PlayerData TradeTo => pendingTrade?.to;
        public DayRule CurrentDayRule => dayRule;
        public RondoResult? TodaysRondo => todaysRondo;
        public bool TurnOrderReversed => turnDirection < 0;

        /// <summary>Night, or a Blackout Day: other players can't see what the active player does.</summary>
        public bool EyesClosed =>
            currentState == GameState.NightPhase ||
            (currentState == GameState.DayPhase && dayRule == DayRule.Blackout);

        // The player sitting at this device (the host's own seat when online).
        public PlayerData LocalPlayer => localPlayer;

        /// <summary>A person plays this seat (here or on another device), so bots leave it alone.</summary>
        public bool IsHuman(PlayerData player) => player != null && humans.Contains(player);

        /// <summary>
        /// Online host: this person left the table. A bot takes over their seat so the game keeps
        /// moving, including a turn or a trade answer they were in the middle of.
        /// </summary>
        public void ReleaseSeat(PlayerData player)
        {
            if (player == null || player == localPlayer || !humans.Remove(player)) return;
            AddLog($"{player.playerName} left the table. A bot takes their seat.");
            if (ActivePlayer == player) OnTurnChanged?.Invoke();
            if (pendingTrade != null && pendingTrade.to == player) OnTradeOffered?.Invoke();
        }

        public int HandCount(PlayerData player) => player?.hand.Count ?? 0;

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
        /// <summary>The local player peeked at the card at this hand index.</summary>
        public event Action<int> OnLocalPeek;
        /// <summary>Any player peeked at the card at this hand index (the host tells that player's device).</summary>
        public event Action<PlayerData, int> OnPeeked;

        private void Awake()
        {
            // Online client: the host runs the rules, this device only shows its own seat.
            if (NetworkGameBridge.IsOnlineClient)
            {
                gameObject.AddComponent<ClientGameView>();
                enabled = false;
                return;
            }

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            GameView.Current = this;
        }

        private void Start()
        {
            if (botsPlayOtherSeats && GetComponent<BotController>() == null)
                gameObject.AddComponent<BotController>();

            SetUpGame();
            TryTransition(GameState.DayPhase);

            if (NetworkGameBridge.IsOnlineHost) NetworkGameBridge.SpawnForHost();
        }

        private void Update()
        {
            // R9: no answer in time counts as a decline
            if (pendingTrade != null && Time.time - tradeOfferedAt > TradeTimeoutSeconds)
            {
                AddLog("No answer in time.");
                RespondToTrade(false);
            }
        }

        public float TradeSecondsLeft => pendingTrade == null ? 0f : Mathf.Max(0f, TradeTimeoutSeconds - (Time.time - tradeOfferedAt));

        /// <summary>Spec setup S1 to S5.</summary>
        private void SetUpGame()
        {
            rng = randomSeed == 0 ? new System.Random() : new System.Random(randomSeed);

            // Online: everyone from the lobby table, in join order. Offline: you plus 3 bots.
            // Empty seats up to 4 are bots either way.
            localPlayer = null;
            humans.Clear();
            if (players.Count == 0)
            {
                SessionManager sessions = SessionManager.Instance;
                if (sessions != null && sessions.InSession)
                {
                    foreach (SessionManager.SeatInfo seat in sessions.GetSeats())
                    {
                        RegisterPlayer(seat.name);
                        PlayerData added = players[players.Count - 1];
                        added.accountId = seat.playerId;
                        humans.Add(added);
                        if (seat.isYou) localPlayer = added;
                    }
                }
                else
                {
                    string savedName = SessionManager.SavedPlayerName;
                    RegisterPlayer(string.IsNullOrWhiteSpace(savedName) ? "You" : savedName.Trim());
                }

                while (players.Count < SessionManager.MinPlayers)
                    RegisterPlayer($"Bot {players.Count}");
            }
            localPlayer ??= players[0];
            humans.Add(localPlayer);

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

        private void AddLog(string line) => AddLog(new LogEntry { text = line });

        private void AddLog(LogEntry entry)
        {
            // Tapping the same wrong thing again shouldn't fill the log with the same hint
            if (entry.viewer != null && playLog.Count > 0)
            {
                LogEntry last = playLog[playLog.Count - 1];
                if (last.viewer == entry.viewer && last.text == entry.text) return;
            }

            playLog.Add(entry);
            if (playLog.Count > 40) playLog.RemoveAt(0);
            Debug.Log($"[GameManager] {entry.text}{(entry.viewer != null ? $" (only {entry.viewer.playerName} sees this)" : "")}");
            OnPlayLogChanged?.Invoke();
        }

        /// <summary>A line only this player sees, like "finish your moves first" or a Flashlight result.</summary>
        private void Tell(PlayerData player, string line) => AddLog(new LogEntry { text = line, viewer = player });

        /// <summary>
        /// Logs a player's action. With eyes closed (Night, Blackout) only the acting player
        /// sees the details; everyone else just sees that something happened (spec C1).
        /// </summary>
        private void LogAction(PlayerData actor, string details)
        {
            if (EyesClosed)
                AddLog(new LogEntry { text = details, viewer = actor, forOthers = $"{actor.playerName} did something in the dark." });
            else
                AddLog(details);
        }

        /// <summary>A result everyone sees by day, but only the acting player sees with eyes closed.</summary>
        private void LogSecret(PlayerData actor, string line)
        {
            if (EyesClosed) Tell(actor, line);
            else AddLog(line);
        }

        /// <summary>The play log as this player is allowed to read it.</summary>
        public List<string> LogFor(PlayerData seat)
        {
            var lines = new List<string>(playLog.Count);
            foreach (LogEntry e in playLog)
            {
                string line = e.viewer == null || e.viewer == seat ? e.text : e.forOthers;
                if (line != null) lines.Add(line);
            }
            return lines;
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
                Tell(player, $"{actionName} is a {(phase == GameState.DayPhase ? "Day" : "Night")} action.");
                return false;
            }
            if (player != ActivePlayer)
            {
                Tell(player, $"It's {ActivePlayer?.playerName}'s turn.");
                return false;
            }
            if (pendingTrade != null)
            {
                Tell(player, "Waiting for an answer to the trade.");
                return false;
            }
            if (!HasAction(player)) return false;

            // R4: the last Night action must be a peek if you haven't peeked yet
            if (phase == GameState.NightPhase && !isPeek && MustPeekNow(player))
            {
                Tell(player, "You must peek at least once tonight. Tap one of your cards, then Peek.");
                return false;
            }
            return true;
        }

        private bool MustPeekNow(PlayerData player) =>
            limitActions && actionsLeft == 1 && peeksThisTurn == 0 && player.hand.Count > 0;

        private bool HasAction(PlayerData player)
        {
            if (!limitActions || actionsLeft > 0) return true;
            Tell(player, "No actions left this turn.");
            return false;
        }

        /// <summary>Spends 1 AP. With no AP left, the turn passes to the next player.</summary>
        private void SpendAction()
        {
            if (!limitActions) return;
            actionsLeft = Mathf.Max(0, actionsLeft - 1);
            if (actionsLeft == 0) EndTurn();
            else EndTurnIfStuck();
        }

        /// <summary>
        /// There's no Pass: players use all their actions. This is true while the player still
        /// has at least one action they could take, so a turn only ends early when nothing is possible.
        /// </summary>
        public bool HasAnyMove(PlayerData player)
        {
            if (!limitActions) return true;
            bool othersHaveCards = players.Any(p => p != player && p.hand.Count > 0);

            if (currentState == GameState.DayPhase)
            {
                bool cardsAllowed = dayRule != DayRule.TradingOnly;
                return (cardsAllowed && deck.Count > 0)                              // Scavenge
                    || (player.hand.Count > 0 && othersHaveCards)                    // Trade
                    || (cardsAllowed && player.hand.Any(c => c.HasMorningFront));    // Play card
            }
            if (currentState == GameState.NightPhase)
            {
                return (othersHaveCards && !stoleOrSwapped)                          // Steal, Swap (one a night)
                    || player.hand.Count > 0;                                        // Peek (or Play card)
            }
            return false;
        }

        /// <summary>The active player can't do anything with the actions they have left, so move on.</summary>
        private void EndTurnIfStuck()
        {
            PlayerData p = ActivePlayer;
            if (p == null || pendingTrade != null || HasAnyMove(p)) return;
            AddLog($"{p.playerName} has no moves left.");
            EndTurn();
        }

        // ---------- Day actions ----------

        /// <summary>P3 Scavenge: draws the top card of the pool (Day).</summary>
        public bool DrawCard(PlayerData player)
        {
            if (!CanAct(player, GameState.DayPhase, "Scavenge")) return false;

            if (dayRule == DayRule.TradingOnly)
            {
                Tell(player, "Trading Only today: no Scavenge.");
                return false;
            }
            if (deck.Count == 0)
            {
                Tell(player, "The draw pile is empty.");
                return false;
            }

            GiveCard(player, TakeTopCard());
            LogAction(player, $"{player.playerName} scavenged a card.");
            OnDeckChanged?.Invoke();
            NotifyHandsChanged();
            SpendAction();
            return true;
        }

        /// <summary>
        /// P1 Offer trade (Day): your card at handIndex for the card of theirs at wantedIndex.
        /// Fronts are on show, so you choose which of their cards you want. They accept or refuse.
        /// </summary>
        public bool OfferTrade(PlayerData from, int handIndex, PlayerData to, int wantedIndex)
        {
            if (!CanAct(from, GameState.DayPhase, "Trade")) return false;

            if (to == null || to == from)
            {
                Tell(from, "Tap a player first, then Trade.");
                return false;
            }
            if (handIndex < 0 || handIndex >= from.hand.Count)
            {
                Tell(from, "Tap one of your cards first, then Trade.");
                return false;
            }
            if (to.hand.Count == 0)
            {
                Tell(from, $"{to.playerName} has no cards to trade.");
                return false;
            }
            if (wantedIndex < 0 || wantedIndex >= to.hand.Count)
            {
                Tell(from, $"Pick which of {to.playerName}'s cards you want.");
                return false;
            }

            CardData card = from.hand[handIndex];
            CardData wanted = to.hand[wantedIndex];
            from.hand.RemoveAt(handIndex);
            pendingTrade = new TradeOffer { from = from, to = to, card = card, wanted = wanted };
            tradeOfferedAt = Time.time;

            LogAction(from, $"{from.playerName} offers {to.playerName} {A(card.FrontName)} for their {wanted.FrontName}.");
            NotifyHandsChanged();
            OnTradeOffered?.Invoke();
            return true;
        }

        /// <summary>"a Food", "an Entertainment".</summary>
        private static string A(string name) => ("AEIOU".IndexOf(char.ToUpperInvariant(name[0])) >= 0 ? "an " : "a ") + name;

        /// <summary>Where the card the offerer asked for sits in the answering player's hand, or -1.</summary>
        public int TradeWantedIndex => pendingTrade == null ? -1 : pendingTrade.to.hand.IndexOf(pendingTrade.wanted);

        /// <summary>
        /// P2 Accept/refuse trade. Accepting swaps the offered card for the one the offerer picked and
        /// costs the offerer 1 AP; refusing returns the offered card and the offerer keeps their AP (spec R9).
        /// </summary>
        public void RespondToTrade(bool accept)
        {
            if (pendingTrade == null) return;

            TradeOffer trade = pendingTrade;
            pendingTrade = null;

            int handIndex = trade.to.hand.IndexOf(trade.wanted);
            if (!accept || handIndex < 0)
            {
                trade.from.hand.Add(trade.card); // still theirs, so they keep what they knew about it
                AddLog($"{trade.to.playerName} declined the trade.");
                NotifyHandsChanged();
                EndTurnIfStuck();
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

        /// <summary>
        /// P6 Peek / P8 Steal-Peek: look at the back of one of your own cards (Night).
        /// Each peek uses 1 action, so peeking at two cards ends your Night turn.
        /// </summary>
        public bool PeekCard(PlayerData player, int handIndex)
        {
            if (player == null) return false;
            if (handIndex < 0 || handIndex >= player.hand.Count)
            {
                Tell(player, "Tap one of your cards first, then Peek.");
                return false;
            }
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
            OnPeeked?.Invoke(player, handIndex);
            if (player == localPlayer) OnLocalPeek?.Invoke(handIndex);
            SpendAction();
            return true;
        }

        /// <summary>Only one Steal or Swap a night; the other action is for peeking (or playing a card).</summary>
        private bool CanStealOrSwap(PlayerData player)
        {
            if (!stoleOrSwapped) return true;
            Tell(player, "You can only Steal or Swap once a night. Peek at a card instead.");
            return false;
        }

        /// <summary>P7 Steal: the thief picks one of the target's face-down cards and takes it (Night).</summary>
        public bool StealFrom(PlayerData thief, PlayerData victim, int victimCardIndex)
        {
            if (!CanAct(thief, GameState.NightPhase, "Steal") || !CanStealOrSwap(thief)) return false;

            if (victim == null || victim == thief)
            {
                Tell(thief, "Tap a player first, then Steal.");
                return false;
            }
            if (victim.hand.Count == 0)
            {
                Tell(thief, $"{victim.playerName} has no cards.");
                return false;
            }
            if (victimCardIndex < 0 || victimCardIndex >= victim.hand.Count)
            {
                Tell(thief, $"Pick one of {victim.playerName}'s cards to steal.");
                return false;
            }

            CardData stolen = victim.hand[victimCardIndex];
            victim.hand.RemoveAt(victimCardIndex);
            GiveCard(thief, stolen);
            stolenThisTurn = stolen;
            stoleOrSwapped = true;

            LogAction(thief, $"{thief.playerName} stole a {stolen.FrontName} from {victim.playerName}.");
            NotifyHandsChanged();
            SpendAction();
            return true;
        }

        /// <summary>P9 Swap: your chosen card for a random face-down card from the target (Night).</summary>
        public bool SwapWith(PlayerData player, int handIndex, PlayerData target)
        {
            if (!CanAct(player, GameState.NightPhase, "Swap") || !CanStealOrSwap(player)) return false;

            if (target == null || target == player)
            {
                Tell(player, "Tap a player first, then Swap.");
                return false;
            }
            if (handIndex < 0 || handIndex >= player.hand.Count)
            {
                Tell(player, "Tap one of your peeked cards to pick it, then Swap.");
                return false;
            }
            if (target.hand.Count == 0)
            {
                Tell(player, $"{target.playerName} has no cards.");
                return false;
            }

            CardData mine = player.hand[handIndex];
            player.hand.RemoveAt(handIndex);
            CardData theirs = TakeRandomCard(target);
            GiveCard(player, theirs);
            GiveCard(target, mine);
            stoleOrSwapped = true;

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
                Tell(player, currentState == GameState.NightPhase && card.HasNightBack
                    ? "Peek at a Night card before playing it."
                    : isDay
                        ? $"{cardName} isn't an action card. By Day you can only play Morning Action cards."
                        : $"{cardName} has no Night action to play.");
                return false;
            }
            if (!CanAct(player, currentState, "Play card")) return false;
            if (isDay && dayRule == DayRule.TradingOnly)
            {
                Tell(player, "Trading Only today: no Action Cards.");
                return false;
            }
            if (!NeedsTarget(card, isDay))
            {
                target = null;
            }
            else if (target == null || target == player)
            {
                Tell(player, $"Tap a player first, then play {cardName}.");
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
                    Tell(player, $"Flashlight: one of {target.playerName}'s cards is {seen.BackName}. ({card.batteries} batteries left)");
                    if (player == localPlayer) OnCardRevealedToYou?.Invoke(target, seen);
                    break;

                case NightActionType.TargetedNightSkip:
                    target.nightSkip = true;
                    LogSecret(player, $"{target.playerName} will lose their next Night turn.");
                    break;

                case NightActionType.TargetedDaySkip:
                    target.daySkip = true;
                    LogSecret(player, $"{target.playerName} will lose their next Day turn.");
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

        /// <summary>
        /// P5 Pass (Day) / P11 End Night turn. There's no Pass button any more: people must use their
        /// actions, so this only goes through when they have no move left (or actions aren't limited).
        /// Bots may still call it when none of their picks worked. At Night you must have peeked (R4).
        /// </summary>
        public bool PassTurn(PlayerData player)
        {
            if (player == null || player != ActivePlayer || pendingTrade != null) return false;

            if (IsHuman(player) && HasAnyMove(player))
            {
                Tell(player, "Finish your moves first.");
                return false;
            }

            if (currentState == GameState.NightPhase && peeksThisTurn == 0 && player.hand.Count > 0 && limitActions)
            {
                Tell(player, "You must peek at least once before ending your Night turn.");
                return false;
            }

            LogAction(player, $"{player.playerName} {(currentState == GameState.NightPhase ? "ended their turn" : "passed")}.");
            EndTurn();
            return true;
        }

        // ---------- Local player helpers (used by the UI) ----------

        public void DrawForLocalPlayer() { DrawCard(LocalPlayer); }
        public void StealForLocalPlayer(int victimCardIndex) { StealFrom(LocalPlayer, selectedTarget, victimCardIndex); }
        public void SwapSelectedForLocalPlayer() { SwapWith(LocalPlayer, selectedCardIndex, selectedTarget); }
        public void PassForLocalPlayer() { PassTurn(LocalPlayer); }
        public void TradeSelectedForLocalPlayer(int wantedIndex) { OfferTrade(LocalPlayer, selectedCardIndex, selectedTarget, wantedIndex); }
        // Kept so the old Skip button binding still compiles; at Night it now ends your turn.
        public void SkipForLocalPlayer() { PassTurn(LocalPlayer); }

        public void PeekForLocalPlayer(int handIndex) { PeekCard(LocalPlayer, handIndex); }
        public void PlaySelectedForLocalPlayer() { PlayChosenCard(LocalPlayer, selectedCardIndex, selectedTarget); }
        public void AnswerTradeForLocalPlayer(bool accept) { AnswerTrade(LocalPlayer, accept); }

        /// <summary>Play card button: the card this player picked, on the player they picked.</summary>
        public bool PlayChosenCard(PlayerData player, int handIndex, PlayerData target)
        {
            if (handIndex < 0)
            {
                Tell(player, "Tap one of your cards first, then Play card.");
                return false;
            }
            return PlayCard(player, handIndex, target);
        }

        /// <summary>A person answers a trade offered to them: accept gives away the card the offerer picked.</summary>
        public void AnswerTrade(PlayerData player, bool accept)
        {
            if (pendingTrade == null || pendingTrade.to != player) return;
            RespondToTrade(accept);
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
            stoleOrSwapped = false;
            peeksThisTurn = 0;

            bool isDay = currentState == GameState.DayPhase;
            if ((isDay && p.daySkip) || (!isDay && p.nightSkip))
            {
                if (isDay) p.daySkip = false;
                else p.nightSkip = false;
                // The skipped player hears why; the card was played with eyes closed, so not by whom
                string phase = isDay ? "Day" : "Night";
                AddLog(new LogEntry
                {
                    text = $"<color=#D93B3B><b>You lose this {phase} turn.</b></color> Someone played a {phase} Skip on you last night.",
                    viewer = p,
                    forOthers = $"{p.playerName} loses this {phase} turn.",
                });
                actionsLeft = 0;
                OnTurnChanged?.Invoke();
                EndTurn();
                return;
            }

            actionsLeft = isDay ? 1 : 2;
            OnTargetChanged?.Invoke();
            OnTurnChanged?.Invoke();
            EndTurnIfStuck();
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
                AddLog("Night falls. Eyes closed: each player takes 2 actions, peeks at least once, and may Steal or Swap once.");
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

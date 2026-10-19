using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TDTS.Data;

namespace TDTS.Managers
{
    public enum GameState { Init, DayPhase, NightPhase, Sundown }

    /// <summary>
    /// Core state machine for game flow. All transitions go through
    /// TryTransition/ResolveX methods. Never set state directly.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Live Game State")]
        [SerializeField] private GameState currentState = GameState.Init;
        [SerializeField] private int currentDay = 1;
        [SerializeField] private int bunkerCapacity = 3;

        [Header("Players")]
        [SerializeField] private List<PlayerData> players = new List<PlayerData>();

        [Header("Actions")]
        [Tooltip("Rulebook: one main action per phase. Untick to test freely.")]
        [SerializeField] private bool limitActions = true;

        // The spec has no hand limit, but the hand row only has 6 slots for now.
        public const int MaxHandSize = 6;
        public const int StartingHandSize = 2;
        public const int MaxPlayers = 6;

        [Header("Deck")]
        [SerializeField] private DeckRecipe deckRecipe = new DeckRecipe();
        [Tooltip("0 = a new random game every time. Any other number deals the same game again.")]
        [SerializeField] private int randomSeed = 0;

        private readonly List<CardData> deck = new List<CardData>();
        private readonly List<CardData> removedFromGame = new List<CardData>();
        [Header("Testing")]
        [Tooltip("PLACEHOLDER until online play: simple bots take the turns of every seat except yours.")]
        [SerializeField] private bool botsPlayOtherSeats = true;

        private readonly List<CardData> discardPile = new List<CardData>();
        private System.Random rng;
        private PlayerData localPlayer;
        private readonly List<string> playLog = new List<string>();
        private PlayerData selectedTarget;
        private int selectedCardIndex = -1;
        private int actionsLeft;

        // Spec V4/V5: whose turn it is, and how many players have had their turn this phase
        private int activeIndex = -1;
        private int turnDirection = 1;
        private int turnsTaken;
        private int turnNumber;

        private TradeOffer pendingTrade;

        /// <summary>Spec zone Z5: a card offered to another player, waiting for their answer.</summary>
        public class TradeOffer
        {
            public PlayerData from;
            public PlayerData to;
            public CardData card;
        }

        public GameState CurrentState => currentState;
        public int CurrentDay => currentDay;
        public int BunkerCapacity => bunkerCapacity;
        public int BunkerSeated => players.Count(p => p.isSeated);
        public IReadOnlyList<PlayerData> Players => players;
        public int DeckCount => deck.Count;
        public bool LimitActions => limitActions;
        public int ActionsLeft => actionsLeft;
        public PlayerData SelectedTarget => selectedTarget;
        public int SelectedCardIndex => selectedCardIndex;
        public IReadOnlyList<string> PlayLog => playLog;
        public PlayerData ActivePlayer => activeIndex >= 0 && activeIndex < players.Count ? players[activeIndex] : null;
        public bool IsLocalTurn => ActivePlayer != null && ActivePlayer == localPlayer;
        public int TurnNumber => turnNumber;
        public TradeOffer PendingTrade => pendingTrade;

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
            TryTransition(GameState.DayPhase); // clears the log, so the setup report comes after
            AddLog("The Bunker Key has been shuffled into the deck.");
            AddLog($"Turn order: {string.Join(" > ", players.Select(p => p.playerName))}");
            AddLog($"Dealt {StartingHandSize} cards each. {deck.Count} cards left in the pool.");
        }

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
            removedFromGame.Clear();
            deck.AddRange(DeckBuilder.BuildDrawPile(deckRecipe, rng, removedFromGame));

            // S4: deal one card at a time around the table
            foreach (var p in players) p.hand.Clear();
            for (int round = 0; round < StartingHandSize; round++)
                foreach (var p in players)
                    GiveCard(p, TakeTopCard());

            // S5
            currentDay = 1;

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

        // ---------- Actions ----------

        /// <summary>Common checks for any player action: right phase, your turn, AP left.</summary>
        private bool CanAct(PlayerData player, GameState phase, string actionName)
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
            return HasAction();
        }

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

        private void AddLog(string line)
        {
            playLog.Add(line);
            if (playLog.Count > 20) playLog.RemoveAt(0);
            Debug.Log($"[GameManager] {line}");
            OnPlayLogChanged?.Invoke();
        }

        /// <summary>Hand contents moved, so a selected card index may now point at a different card.</summary>
        private void NotifyHandsChanged()
        {
            selectedCardIndex = -1;
            OnHandsChanged?.Invoke();
        }

        /// <summary>P3 Scavenge: draws the top card of the pool (Day).</summary>
        public bool DrawCard(PlayerData player)
        {
            if (!CanAct(player, GameState.DayPhase, "Scavenge")) return false;

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
            AddLog($"{player.playerName} scavenged a card.");
            OnDeckChanged?.Invoke();
            NotifyHandsChanged();
            SpendAction();
            return true;
        }

        /// <summary>P1 Offer trade: puts 1 card on offer to the selected player (Day). They answer with RespondToTrade.</summary>
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

            AddLog($"{from.playerName} offers {to.playerName} a {card.FrontName}.");
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

            AddLog($"{trade.from.playerName} traded a {trade.card.FrontName} for {trade.to.playerName}'s {given.FrontName}.");
            NotifyHandsChanged();
            SpendAction();
        }

        /// <summary>Steal: takes a random card from the selected target (Night).</summary>
        public bool StealFromTarget(PlayerData thief)
        {
            if (!CanAct(thief, GameState.NightPhase, "Steal")) return false;

            if (selectedTarget == null || selectedTarget == thief)
            {
                AddLog("Tap a player first, then Steal.");
                return false;
            }
            if (selectedTarget.hand.Count == 0)
            {
                AddLog($"{selectedTarget.playerName} has no cards.");
                return false;
            }
            if (thief.hand.Count >= MaxHandSize)
            {
                AddLog("Your hand is full.");
                return false;
            }

            PlayerData victim = selectedTarget;
            int i = rng.Next(victim.hand.Count);
            CardData stolen = victim.hand[i];
            victim.hand.RemoveAt(i);
            GiveCard(thief, stolen);

            AddLog($"{thief.playerName} stole a card from {victim.playerName}.");
            NotifyHandsChanged();
            SpendAction();
            return true;
        }

        /// <summary>Skip: uses one Night action and does nothing.</summary>
        public bool SkipAction(PlayerData player)
        {
            if (!CanAct(player, GameState.NightPhase, "Skip")) return false;

            AddLog($"{player.playerName} skipped.");
            SpendAction();
            return true;
        }

        /// <summary>
        /// P4 / P10: plays a card from a player's hand. Morning cards are played by their front
        /// in the Day, Night cards by their back at Night once the owner has peeked.
        /// </summary>
        public bool PlayCard(PlayerData player, int handIndex)
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
                AddLog($"{cardName} can't be played during {currentState}.");
                return false;
            }
            if (!CanAct(player, currentState, "Play card")) return false;

            player.hand.RemoveAt(handIndex);
            discardPile.Add(card);

            // Card effects (G19 to G26) come in Phase 5
            AddLog($"{player.playerName} played {cardName}.");
            OnCardPlayed?.Invoke(card);
            NotifyHandsChanged();
            SpendAction();
            return true;
        }

        /// <summary>P5 Pass: ends the turn without acting.</summary>
        public bool PassTurn(PlayerData player)
        {
            if (player == null || player != ActivePlayer || pendingTrade != null) return false;

            AddLog($"{player.playerName} passed.");
            EndTurn();
            return true;
        }

        // Void wrappers so these can be used from On Click() bindings in the Inspector.
        public void DrawForLocalPlayer() { DrawCard(LocalPlayer); }
        public void StealForLocalPlayer() { StealFromTarget(LocalPlayer); }
        public void SkipForLocalPlayer() { SkipAction(LocalPlayer); }
        public void PassForLocalPlayer() { PassTurn(LocalPlayer); }
        public void PlaySelectedForLocalPlayer() { PlaySelectedCard(LocalPlayer); }
        public void TradeSelectedForLocalPlayer() { OfferTrade(LocalPlayer, selectedCardIndex, selectedTarget); }

        private void PlaySelectedCard(PlayerData player)
        {
            if (selectedCardIndex < 0)
            {
                AddLog("Tap one of your cards first, then Play card.");
                return;
            }
            PlayCard(player, selectedCardIndex);
        }

        // ---------- Selection ----------

        /// <summary>Tap a player zone to pick who you'll trade with or steal from. Tap again to clear.</summary>
        public void SelectTarget(PlayerData player)
        {
            if (player == null || player == LocalPlayer) return;
            selectedTarget = (selectedTarget == player) ? null : player;
            OnTargetChanged?.Invoke();
        }

        /// <summary>Tap one of your cards to pick it for Trade or Play card. Tap again to clear.</summary>
        public void SelectCard(int handIndex)
        {
            selectedCardIndex = (selectedCardIndex == handIndex) ? -1 : handIndex;
            OnTargetChanged?.Invoke();
        }

        // ---------- Turn flow ----------

        /// <summary>Steps 2 and 6: the first player in turn order starts the phase.</summary>
        private void StartPhaseTurns()
        {
            turnsTaken = 0;
            activeIndex = 0;
            BeginTurn();
        }

        /// <summary>Steps 3 and 7 (G14, G15): refill AP for the active player.</summary>
        private void BeginTurn()
        {
            actionsLeft = currentState == GameState.NightPhase ? 2 : 1;
            selectedTarget = null;
            selectedCardIndex = -1;
            turnNumber++;

            OnTargetChanged?.Invoke();
            OnTurnChanged?.Invoke();
        }

        /// <summary>Steps 5 and 9 (G16): pass to the next player, or end the phase when everyone has gone.</summary>
        private void EndTurn()
        {
            if (currentState != GameState.DayPhase && currentState != GameState.NightPhase) return;

            turnsTaken++;
            if (turnsTaken >= players.Count)
            {
                if (currentState == GameState.DayPhase) ResolveDayPhase();
                else ResolveNightPhase();
                return;
            }

            activeIndex = (activeIndex + turnDirection + players.Count) % players.Count;
            BeginTurn();
        }

        // ---------- Phase flow ----------

        public void ResolveDayPhase()
        {
            if (currentState != GameState.DayPhase) return;
            TryTransition(GameState.NightPhase);
        }

        public void ResolveNightPhase()
        {
            if (currentState != GameState.NightPhase) return;

            if (currentDay >= 10)
            {
                TryTransition(GameState.Sundown);
            }
            else
            {
                currentDay++;
                TryTransition(GameState.DayPhase);
            }
        }

        public bool SeatPlayer(string playerName)
        {
            if (BunkerSeated >= bunkerCapacity) return false;

            var player = players.FirstOrDefault(p => p.playerName == playerName);
            if (player == null) return false;

            player.isSeated = true;
            return true;
        }

        public void SeatPlayerButtonHandler(string playerName)
        {
            bool success = SeatPlayer(playerName);
            if (!success)
                Debug.Log($"[GameManager] Seat attempt failed for {playerName}: bunker full or player not found.");
        }

        public void UnseatPlayer(string playerName)
        {
            var player = players.FirstOrDefault(p => p.playerName == playerName);
            if (player != null) player.isSeated = false;
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

            Debug.Log($"[GameManager] State transition: {previous} -> {next} (Day {currentDay}, Bunker {BunkerSeated}/{bunkerCapacity})");
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
    }
}

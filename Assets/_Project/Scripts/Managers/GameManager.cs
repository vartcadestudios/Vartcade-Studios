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
        private System.Random rng;
        private PlayerData localPlayer;
        private readonly List<string> playLog = new List<string>();
        private PlayerData selectedTarget;
        private int actionsLeft;

        public GameState CurrentState => currentState;
        public int CurrentDay => currentDay;
        public int BunkerCapacity => bunkerCapacity;
        public int BunkerSeated => players.Count(p => p.isSeated);
        public IReadOnlyList<PlayerData> Players => players;
        public int DeckCount => deck.Count;
        public bool LimitActions => limitActions;
        public int ActionsLeft => actionsLeft;
        public PlayerData SelectedTarget => selectedTarget;
        public IReadOnlyList<string> PlayLog => playLog;

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
        public event Action OnTargetChanged;

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

        private bool HasAction()
        {
            if (!limitActions || actionsLeft > 0) return true;
            AddLog("No actions left this phase.");
            return false;
        }

        private void SpendAction()
        {
            if (limitActions) actionsLeft = Mathf.Max(0, actionsLeft - 1);
        }

        private void AddLog(string line)
        {
            playLog.Add(line);
            if (playLog.Count > 20) playLog.RemoveAt(0);
            Debug.Log($"[GameManager] {line}");
            OnPlayLogChanged?.Invoke();
        }

        /// <summary>Scavenge: draws the top card of the deck (Day phase only).</summary>
        public bool DrawCard(PlayerData player)
        {
            if (player == null) return false;

            if (currentState != GameState.DayPhase)
            {
                AddLog("Scavenge is a Day action.");
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
            if (!HasAction()) return false;

            GiveCard(player, TakeTopCard());
            SpendAction();

            AddLog($"{player.playerName} scavenged a card.");
            OnDeckChanged?.Invoke();
            OnHandsChanged?.Invoke();
            return true;
        }

        /// <summary>Steal: takes a random card from the selected target (Night phase only).</summary>
        public bool StealFromTarget(PlayerData thief)
        {
            if (thief == null) return false;

            if (currentState != GameState.NightPhase)
            {
                AddLog("Steal is a Night action.");
                return false;
            }
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
            if (!HasAction()) return false;

            int i = UnityEngine.Random.Range(0, selectedTarget.hand.Count);
            CardData stolen = selectedTarget.hand[i];
            selectedTarget.hand.RemoveAt(i);
            GiveCard(thief, stolen);
            SpendAction();

            AddLog($"{thief.playerName} stole a card from {selectedTarget.playerName}.");
            OnHandsChanged?.Invoke();
            return true;
        }

        /// <summary>Skip: uses your Night action and does nothing.</summary>
        public bool SkipAction(PlayerData player)
        {
            if (player == null) return false;

            if (currentState != GameState.NightPhase)
            {
                AddLog("Skip is a Night action.");
                return false;
            }
            if (!HasAction()) return false;

            SpendAction();
            AddLog($"{player.playerName} skipped.");
            return true;
        }

        /// <summary>
        /// Plays an action card from a player's hand into the play space.
        /// Morning cards only work in the Day phase, Night cards only at Night.
        /// </summary>
        public bool PlayCard(PlayerData player, int handIndex)
        {
            if (player == null || handIndex < 0 || handIndex >= player.hand.Count) return false;

            CardData card = player.hand[handIndex];

            // Morning cards are played by their front in the Day,
            // Night cards by their back at Night, once the owner has peeked.
            bool isDay = currentState == GameState.DayPhase;
            bool allowed =
                (isDay && card.HasMorningFront) ||
                (currentState == GameState.NightPhase && card.HasNightBack && card.peeked);
            string cardName = isDay ? card.FrontName : card.BackName;

            if (!allowed)
            {
                AddLog($"{cardName} can't be played during {currentState}.");
                return false;
            }
            if (!HasAction()) return false;

            player.hand.RemoveAt(handIndex);
            SpendAction();

            AddLog($"{player.playerName} played {cardName}.");
            OnCardPlayed?.Invoke(card);
            OnHandsChanged?.Invoke();
            return true;
        }

        // Void wrappers so these can be used from On Click() bindings in the Inspector.
        public void DrawForLocalPlayer() { DrawCard(LocalPlayer); }
        public void StealForLocalPlayer() { StealFromTarget(LocalPlayer); }
        public void SkipForLocalPlayer() { SkipAction(LocalPlayer); }

        // ---------- Target selection ----------

        /// <summary>Tap a player zone to pick who you'll steal from. Tap again to clear.</summary>
        public void SelectTarget(PlayerData player)
        {
            if (player == null || player == LocalPlayer) return;
            selectedTarget = (selectedTarget == player) ? null : player;
            OnTargetChanged?.Invoke();
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

            // Fresh phase: one action, empty play space, no target selected.
            actionsLeft = (next == GameState.DayPhase || next == GameState.NightPhase) ? 1 : 0;
            playLog.Clear();
            selectedTarget = null;

            Debug.Log($"[GameManager] State transition: {previous} -> {next} (Day {currentDay}, Bunker {BunkerSeated}/{bunkerCapacity})");
            OnPlayLogChanged?.Invoke();
            OnTargetChanged?.Invoke();
            OnStateChanged?.Invoke(previous, next);
        }
    }
}
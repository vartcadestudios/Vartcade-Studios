using System;
using System.Collections.Generic;
using UnityEngine;
using TDTS.Data;
using TDTS.Managers;

namespace TDTS.Networking
{
    /// <summary>
    /// The table as an online client sees it. Holds only what the host's last SeatSnapshot said,
    /// so other players are a name and the fronts of their cards. Buttons become requests to the host, which
    /// runs the rules and answers with a new snapshot. GameManager adds this on clients.
    /// </summary>
    public class ClientGameView : MonoBehaviour, IGameView
    {
        public static ClientGameView Instance { get; private set; }

        private NetworkGameBridge bridge;
        private bool hasSnapshot;
        private SeatSnapshot snap;

        private readonly List<PlayerData> players = new List<PlayerData>();
        private readonly List<string> playLog = new List<string>();
        private PlayerData localPlayer;
        private PlayerData selectedTarget;
        private int selectedCardIndex = -1;
        private float tradeEndsAt;
        private string handSignature = "";

        public event Action<GameState, GameState> OnStateChanged;
        public event Action OnHandsChanged;
        public event Action OnDeckChanged;
        public event Action OnPlayLogChanged;
        public event Action OnTargetChanged;
        public event Action OnTurnChanged;
        public event Action<RondoResult> OnRondoSpun;
        public event Action<int> OnLocalPeek;

        private void Awake()
        {
            Instance = this;
            GameView.Current = this;
            playLog.Add("Waiting for the host...");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Attach(NetworkGameBridge b) => bridge = b;

        public void Detach(NetworkGameBridge b)
        {
            if (bridge == b) bridge = null;
        }

        // ---------- State ----------

        public GameState CurrentState => hasSnapshot ? snap.state : GameState.Init;
        public int CurrentDay => hasSnapshot ? snap.day : 1;
        public int SundownDay => hasSnapshot ? snap.sundownDay : 10;
        public int BunkerCapacity => hasSnapshot ? snap.bunkerCapacity : 2;
        public int BunkerSeated
        {
            get
            {
                int n = 0;
                foreach (PlayerData p in players) if (p.isSeated) n++;
                return n;
            }
        }
        public int DeckCount => snap.deckCount;
        public bool LimitActions => !hasSnapshot || snap.limitActions;
        public int ActionsLeft => snap.actionsLeft;
        public int PeeksThisTurn => snap.peeksThisTurn;
        public int TurnNumber => snap.turnNumber;
        public DayRule CurrentDayRule => snap.dayRule;
        public RondoResult? TodaysRondo => hasSnapshot && snap.rondo >= 0 ? (RondoResult)snap.rondo : (RondoResult?)null;
        public bool EyesClosed => snap.eyesClosed;

        public IReadOnlyList<PlayerData> Players => players;
        public PlayerData LocalPlayer => localPlayer;
        public PlayerData ActivePlayer => Seat(hasSnapshot ? snap.activeSeat : -1);
        public bool IsLocalTurn => ActivePlayer != null && ActivePlayer == localPlayer;

        public PlayerData GetPlayerAfterLocal(int seatsAfter)
        {
            if (localPlayer == null || seatsAfter <= 0 || seatsAfter >= players.Count) return null;
            return players[(snap.yourSeat + seatsAfter) % players.Count];
        }

        public int HandCount(PlayerData player)
        {
            int seat = players.IndexOf(player);
            return seat >= 0 && snap.handCounts != null && seat < snap.handCounts.Length ? snap.handCounts[seat] : 0;
        }

        public IReadOnlyList<string> PlayLog => playLog;

        public bool HasPendingTrade => hasSnapshot && snap.tradeFrom >= 0;
        public PlayerData TradeTo => HasPendingTrade ? Seat(snap.tradeTo) : null;
        public float TradeSecondsLeft => HasPendingTrade ? Mathf.Max(0f, tradeEndsAt - Time.time) : 0f;
        public int TradeWantedIndex => HasPendingTrade ? snap.tradeWanted : -1;

        public PlayerData SelectedTarget => selectedTarget;
        public int SelectedCardIndex => selectedCardIndex;

        private PlayerData Seat(int seat) => seat >= 0 && seat < players.Count ? players[seat] : null;
        private int SeatOf(PlayerData player) => player == null ? -1 : players.IndexOf(player);

        // ---------- Snapshots from the host ----------

        public void Apply(SeatSnapshot next)
        {
            GameState previousState = CurrentState;
            int previousTurn = snap.turnNumber;
            int previousRondo = hasSnapshot ? snap.rondo : -1;
            int previousDay = snap.day;
            bool hadTrade = HasPendingTrade;

            snap = next;
            hasSnapshot = true;

            // Keep the same PlayerData per seat so a picked target stays picked between snapshots
            int count = next.names?.Length ?? 0;
            while (players.Count < count) players.Add(new PlayerData(""));
            if (players.Count > count) players.RemoveRange(count, players.Count - count);
            for (int i = 0; i < count; i++)
            {
                players[i].playerName = next.names[i];
                players[i].hand.Clear();
                players[i].isSeated = next.seated != null && i < next.seated.Length && next.seated[i];
                players[i].resultReason = next.resultReasons != null && i < next.resultReasons.Length ? next.resultReasons[i] : "";
            }

            localPlayer = Seat(next.yourSeat);
            if (localPlayer != null && next.hand != null)
                foreach (CardSnapshot card in next.hand) localPlayer.hand.Add(card.ToCard());

            // Everyone else's hand holds just the fronts, which are public
            int nextFront = 0;
            for (int i = 0; i < count && next.fronts != null && next.handCounts != null && i < next.handCounts.Length; i++)
            {
                int cards = next.handCounts[i];
                if (players[i] != localPlayer)
                    for (int n = 0; n < cards && nextFront + n < next.fronts.Length; n++)
                        players[i].hand.Add(next.fronts[nextFront + n].ToCard());
                nextFront += cards;
            }

            playLog.Clear();
            if (next.log != null) playLog.AddRange(next.log);

            if (HasPendingTrade && !hadTrade) tradeEndsAt = Time.time + next.tradeSecondsLeft;

            // The host clears picks when hands move or a new turn starts; do the same here
            string signature = HandSignature();
            bool handMoved = signature != handSignature;
            handSignature = signature;
            if (handMoved) selectedCardIndex = -1;
            if (next.turnNumber != previousTurn || next.state != previousState)
            {
                selectedTarget = null;
                selectedCardIndex = -1;
            }
            if (selectedTarget != null && !players.Contains(selectedTarget)) selectedTarget = null;

            if (next.state != previousState) OnStateChanged?.Invoke(previousState, next.state);
            // The same result can come up two days running, so a new day counts as a new spin
            if (next.rondo >= 0 && (next.rondo != previousRondo || next.day != previousDay)) OnRondoSpun?.Invoke((RondoResult)next.rondo);
            OnDeckChanged?.Invoke();
            OnHandsChanged?.Invoke();
            OnPlayLogChanged?.Invoke();
            OnTargetChanged?.Invoke();
            OnTurnChanged?.Invoke();
            if (next.peekIndex >= 0) OnLocalPeek?.Invoke(next.peekIndex);
        }

        private string HandSignature()
        {
            if (localPlayer == null) return "";
            var parts = new List<string>();
            foreach (CardData c in localPlayer.hand) parts.Add(c.FrontName);
            return string.Join("|", parts);
        }

        // ---------- Requests to the host ----------

        private void Say(string line)
        {
            playLog.Add(line);
            OnPlayLogChanged?.Invoke();
        }

        private bool Ready()
        {
            if (bridge != null && bridge.IsSpawned) return true;
            Say("Not connected to the host.");
            return false;
        }

        public void DrawForLocalPlayer()
        {
            if (Ready()) bridge.DrawRpc();
        }

        public void TradeSelectedForLocalPlayer(int wantedIndex)
        {
            if (Ready()) bridge.OfferTradeRpc(selectedCardIndex, SeatOf(selectedTarget), wantedIndex);
        }

        public void PlaySelectedForLocalPlayer()
        {
            if (Ready()) bridge.PlayCardRpc(selectedCardIndex, SeatOf(selectedTarget));
        }

        public void StealForLocalPlayer(int victimCardIndex)
        {
            if (Ready()) bridge.StealRpc(SeatOf(selectedTarget), victimCardIndex);
        }

        public void SwapSelectedForLocalPlayer()
        {
            if (Ready()) bridge.SwapRpc(selectedCardIndex, SeatOf(selectedTarget));
        }

        public void PassForLocalPlayer()
        {
            if (Ready()) bridge.PassRpc();
        }

        public void PeekForLocalPlayer(int handIndex)
        {
            if (Ready()) bridge.PeekRpc(handIndex);
        }

        public void AnswerTradeForLocalPlayer(bool accept)
        {
            if (Ready()) bridge.AnswerTradeRpc(accept);
        }

        // ---------- Picks stay on this device ----------

        public void SelectTarget(PlayerData player)
        {
            if (player == null || player == localPlayer) return;
            selectedTarget = selectedTarget == player ? null : player;
            OnTargetChanged?.Invoke();
        }

        public void SelectCard(int handIndex)
        {
            selectedCardIndex = selectedCardIndex == handIndex ? -1 : handIndex;
            OnTargetChanged?.Invoke();
        }
    }
}

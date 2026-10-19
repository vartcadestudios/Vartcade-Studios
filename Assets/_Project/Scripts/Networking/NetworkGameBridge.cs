using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using TDTS.Data;
using TDTS.Managers;

namespace TDTS.Networking
{
    /// <summary>
    /// Online play, host-authoritative. The host spawns this in the Game scene. Each client tells it
    /// who they are, then asks for actions with RPCs; the host checks every request by running the
    /// normal GameManager rules for that client's seat. After any change the host sends each client
    /// a snapshot of only what their seat may see (SeatSnapshot).
    /// </summary>
    public class NetworkGameBridge : NetworkBehaviour
    {
        public const string PrefabPath = "NetworkGameBridge"; // under a Resources folder

        private static GameObject prefab;

        private GameManager gm;
        private readonly Dictionary<ulong, PlayerData> seatOf = new Dictionary<ulong, PlayerData>();
        private readonly Dictionary<PlayerData, int> pendingPeek = new Dictionary<PlayerData, int>();
        private bool dirty;

        public static bool IsOnline => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        public static bool IsOnlineHost => IsOnline && NetworkManager.Singleton.IsServer;
        public static bool IsOnlineClient => IsOnline && !NetworkManager.Singleton.IsServer;

        /// <summary>Every device must register the same network prefabs before connecting.</summary>
        public static void RegisterPrefab(NetworkManager manager)
        {
            if (prefab == null) prefab = Resources.Load<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[Bridge] Missing Resources/{PrefabPath} prefab.");
                return;
            }
            if (!manager.NetworkConfig.Prefabs.Contains(prefab))
                manager.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = prefab });
        }

        /// <summary>Host only: put the bridge on the network once every device has loaded the Game scene.</summary>
        public static void SpawnForHost()
        {
            SessionManager sessions = SessionManager.Instance;
            if (sessions == null || sessions.EveryoneLoadedGame) Spawn();
            else sessions.OnEveryoneLoadedGame += Spawn;

            static void Spawn()
            {
                if (SessionManager.Instance != null) SessionManager.Instance.OnEveryoneLoadedGame -= Spawn;
                if (!IsOnlineHost || prefab == null || FindAnyObjectByType<NetworkGameBridge>() != null) return;
                Instantiate(prefab).GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                gm = GameManager.Instance;
                if (gm == null) return;
                gm.OnStateChanged += HandleStateChanged;
                gm.OnHandsChanged += MarkDirty;
                gm.OnDeckChanged += MarkDirty;
                gm.OnPlayLogChanged += MarkDirty;
                gm.OnTurnChanged += MarkDirty;
                gm.OnTradeOffered += MarkDirty;
                gm.OnRondoSpun += HandleRondoSpun;
                gm.OnPeeked += HandlePeeked;
                NetworkManager.OnClientDisconnectCallback += HandleClientLeft;
            }
            else
            {
                ClientGameView view = ClientGameView.Instance;
                if (view != null) view.Attach(this);
                HelloRpc(SessionManager.Instance != null ? SessionManager.Instance.LocalPlayerId ?? "" : "");
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                if (NetworkManager != null) NetworkManager.OnClientDisconnectCallback -= HandleClientLeft;
                if (gm == null) return;
                gm.OnStateChanged -= HandleStateChanged;
                gm.OnHandsChanged -= MarkDirty;
                gm.OnDeckChanged -= MarkDirty;
                gm.OnPlayLogChanged -= MarkDirty;
                gm.OnTurnChanged -= MarkDirty;
                gm.OnTradeOffered -= MarkDirty;
                gm.OnRondoSpun -= HandleRondoSpun;
                gm.OnPeeked -= HandlePeeked;
            }
        }

        private void MarkDirty() => dirty = true;
        private void HandleStateChanged(GameState previous, GameState next) => dirty = true;
        private void HandleRondoSpun(RondoResult result) => dirty = true;

        private void HandlePeeked(PlayerData player, int handIndex)
        {
            pendingPeek[player] = handIndex;
            dirty = true;
        }

        private void HandleClientLeft(ulong clientId)
        {
            if (!seatOf.TryGetValue(clientId, out PlayerData seat)) return;
            seatOf.Remove(clientId);
            gm.ReleaseSeat(seat);
        }

        // Batch everything that changed this frame into one snapshot per client
        private void LateUpdate()
        {
            if (!IsServer || !dirty || gm == null) return;
            dirty = false;
            foreach (var pair in seatOf.ToList()) SendSnapshot(pair.Key, pair.Value);
            pendingPeek.Clear();
        }

        // ---------- Client -> host requests ----------

        /// <summary>A client says which lobby player it is, so the host knows its seat.</summary>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void HelloRpc(string accountId, RpcParams rpcParams = default)
        {
            ulong client = rpcParams.Receive.SenderClientId;
            PlayerData seat = gm?.Players.FirstOrDefault(p => !string.IsNullOrEmpty(accountId) && p.accountId == accountId);
            if (seat == null)
            {
                Debug.LogWarning($"[Bridge] Client {client} isn't at this table ({accountId}).");
                return;
            }
            seatOf[client] = seat;
            Debug.Log($"[Bridge] Client {client} plays {seat.playerName}.");
            SendSnapshot(client, seat);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void DrawRpc(RpcParams rpcParams = default)
        {
            if (TrySeat(rpcParams, out PlayerData me)) gm.DrawCard(me);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void OfferTradeRpc(int handIndex, int targetSeat, RpcParams rpcParams = default)
        {
            if (TrySeat(rpcParams, out PlayerData me)) gm.OfferTrade(me, handIndex, SeatAt(targetSeat));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void AnswerTradeRpc(bool accept, int handIndex, RpcParams rpcParams = default)
        {
            if (TrySeat(rpcParams, out PlayerData me)) gm.AnswerTrade(me, accept, handIndex);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void PeekRpc(int handIndex, RpcParams rpcParams = default)
        {
            if (TrySeat(rpcParams, out PlayerData me)) gm.PeekCard(me, handIndex);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void StealRpc(int targetSeat, int victimCardIndex, RpcParams rpcParams = default)
        {
            if (TrySeat(rpcParams, out PlayerData me)) gm.StealFrom(me, SeatAt(targetSeat), victimCardIndex);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SwapRpc(int handIndex, int targetSeat, RpcParams rpcParams = default)
        {
            if (TrySeat(rpcParams, out PlayerData me)) gm.SwapWith(me, handIndex, SeatAt(targetSeat));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void PlayCardRpc(int handIndex, int targetSeat, RpcParams rpcParams = default)
        {
            if (TrySeat(rpcParams, out PlayerData me)) gm.PlayChosenCard(me, handIndex, SeatAt(targetSeat));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void PassRpc(RpcParams rpcParams = default)
        {
            if (TrySeat(rpcParams, out PlayerData me)) gm.PassTurn(me);
        }

        private bool TrySeat(RpcParams rpcParams, out PlayerData seat)
        {
            seat = null;
            return gm != null && seatOf.TryGetValue(rpcParams.Receive.SenderClientId, out seat);
        }

        private PlayerData SeatAt(int seat) => seat >= 0 && seat < gm.Players.Count ? gm.Players[seat] : null;

        // ---------- Host -> client snapshots ----------

        private void SendSnapshot(ulong client, PlayerData seat)
        {
            if (!NetworkManager.ConnectedClients.ContainsKey(client)) return;
            SnapshotRpc(BuildSnapshot(seat), RpcTarget.Single(client, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void SnapshotRpc(SeatSnapshot snapshot, RpcParams rpcParams)
        {
            ClientGameView view = ClientGameView.Instance;
            if (view != null) view.Apply(snapshot);
        }

        /// <summary>Only what this seat may know: its own cards, everyone's counts, and its own view of the log.</summary>
        private SeatSnapshot BuildSnapshot(PlayerData seat)
        {
            IReadOnlyList<PlayerData> players = gm.Players;
            int IndexOf(PlayerData p)
            {
                for (int i = 0; i < players.Count; i++) if (players[i] == p) return i;
                return -1;
            }

            GameManager.TradeOffer trade = gm.PendingTrade;
            return new SeatSnapshot
            {
                yourSeat = IndexOf(seat),
                state = gm.CurrentState,
                day = gm.CurrentDay,
                sundownDay = gm.SundownDay,
                bunkerCapacity = gm.BunkerCapacity,
                deckCount = gm.DeckCount,
                limitActions = gm.LimitActions,
                actionsLeft = gm.ActionsLeft,
                peeksThisTurn = gm.ActivePlayer == seat ? gm.PeeksThisTurn : 0,
                activeSeat = IndexOf(gm.ActivePlayer),
                turnNumber = gm.TurnNumber,
                dayRule = gm.CurrentDayRule,
                rondo = gm.TodaysRondo.HasValue ? (int)gm.TodaysRondo.Value : -1,
                eyesClosed = gm.EyesClosed,

                names = players.Select(p => p.playerName).ToArray(),
                handCounts = players.Select(p => p.hand.Count).ToArray(),
                // Results are only public once Sundown has decided them
                seated = players.Select(p => gm.CurrentState == GameState.Sundown && p.isSeated).ToArray(),
                resultReasons = players.Select(p => gm.CurrentState == GameState.Sundown ? p.resultReason : "").ToArray(),

                hand = seat.hand.Select(CardSnapshot.From).ToArray(),

                tradeFrom = trade != null ? IndexOf(trade.from) : -1,
                tradeTo = trade != null ? IndexOf(trade.to) : -1,
                tradeSecondsLeft = gm.TradeSecondsLeft,

                log = gm.LogFor(seat).ToArray(),
                peekIndex = pendingPeek.TryGetValue(seat, out int peek) ? peek : -1,
            };
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            ClientGameView view = ClientGameView.Instance;
            if (view != null) view.Detach(this);
        }
    }
}

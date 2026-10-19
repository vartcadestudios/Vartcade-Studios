using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TDTS.Networking
{
    /// <summary>
    /// Online play, one device per player (spec C1). Signs in to Unity Gaming Services, hosts or joins
    /// a session by its short join code (Relay, so no port forwarding), and lets the host start the game
    /// for everyone. Lives across scenes; created on demand by the Lobby.
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance { get; private set; }

        public const int MinPlayers = 4; // empty seats are filled by bots
        public const int MaxPlayers = 6;
        private const string NameKey = "name";
        private const string PlayerNamePref = "TDTS.PlayerName";

        [SerializeField] private string gameSceneName = "Game";

        private ISession session;
        private Task signIn;

        public ISession Session => session;
        public bool InSession => session != null;
        public bool IsHost => session != null && session.IsHost;
        public string JoinCode => session?.Code ?? "";
        public string LocalPlayerId => session?.CurrentPlayer?.Id;

        /// <summary>Fires whenever the player list, host or code changes.</summary>
        public event Action OnSessionChanged;
        /// <summary>A line for the Lobby status bar (errors included).</summary>
        public event Action<string> OnStatus;

        /// <summary>Host: every device has finished loading the Game scene, so it is safe to spawn into it.</summary>
        public bool EveryoneLoadedGame { get; private set; }
        public event Action OnEveryoneLoadedGame;

        public static SessionManager GetOrCreate()
        {
            if (Instance == null)
                new GameObject("SessionManager").AddComponent<SessionManager>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureNetworkManager();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static string SavedPlayerName
        {
            get => PlayerPrefs.GetString(PlayerNamePref, "");
            set => PlayerPrefs.SetString(PlayerNamePref, value);
        }

        // ---------- Setup ----------

        /// <summary>The session hands Relay to Netcode through NetworkManager.Singleton, so make one if the scene has none.</summary>
        private static void EnsureNetworkManager()
        {
            if (NetworkManager.Singleton == null)
            {
                var go = new GameObject("NetworkManager");
                DontDestroyOnLoad(go);
                var transport = go.AddComponent<UnityTransport>();
                var manager = go.AddComponent<NetworkManager>();
                manager.NetworkConfig = new NetworkConfig
                {
                    NetworkTransport = transport,
                    EnableSceneManagement = true,
                    ConnectionApproval = false,
                };
            }
            NetworkGameBridge.RegisterPrefab(NetworkManager.Singleton);
        }

        private Task SignInAsync()
        {
            return signIn ??= SignInOnce();
        }

        private async Task SignInOnce()
        {
            Status("Connecting to Unity services...");
            var options = new InitializationOptions();
#if UNITY_EDITOR
            // Multiplayer Play Mode runs several players on one PC; each needs its own anonymous account.
            options.SetProfile("p" + Guid.NewGuid().ToString("N").Substring(0, 12));
#endif
            await UnityServices.InitializeAsync(options);
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        // ---------- Host / join / leave ----------

        public async Task<bool> HostAsync(string playerName)
        {
            return await Run("Creating a table...", async () =>
            {
                var options = new SessionOptions
                {
                    MaxPlayers = MaxPlayers,
                    IsPrivate = true, // join by code only
                    PlayerProperties = NameProperty(playerName),
                }.WithRelayNetwork();
                Attach(await MultiplayerService.Instance.CreateSessionAsync(options));
                Status($"Table open. Share the code {session.Code} with your group.");
            }, playerName);
        }

        public async Task<bool> JoinAsync(string code, string playerName)
        {
            code = (code ?? "").Trim().ToUpperInvariant();
            if (code.Length == 0)
            {
                Status("Type the join code first.");
                return false;
            }

            return await Run($"Joining {code}...", async () =>
            {
                var options = new JoinSessionOptions { PlayerProperties = NameProperty(playerName) };
                Attach(await MultiplayerService.Instance.JoinSessionByCodeAsync(code, options));
                Status("Joined. Waiting for the host to start.");
            }, playerName);
        }

        public async Task LeaveAsync()
        {
            ISession leaving = session;
            Detach();
            if (leaving != null)
            {
                try { await leaving.LeaveAsync(); }
                catch (Exception e) { Debug.LogWarning($"[Session] Leave failed: {e.Message}"); }
            }
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
            Status("Left the table.");
        }

        /// <summary>Host only: lock the table and move every device to the Game scene together.</summary>
        public async void StartGame()
        {
            if (!IsHost) return;

            try
            {
                IHostSession host = session.AsHost();
                host.IsLocked = true;
                await host.SavePropertiesAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Session] Could not lock the table: {e.Message}");
            }

            EveryoneLoadedGame = false;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }

        private void HandleLoadEventCompleted(string sceneName, LoadSceneMode mode, List<ulong> loaded, List<ulong> timedOut)
        {
            if (sceneName != gameSceneName) return;
            if (timedOut.Count > 0) Debug.LogWarning($"[Session] {timedOut.Count} player(s) took too long to load the game.");
            EveryoneLoadedGame = true;
            OnEveryoneLoadedGame?.Invoke();
        }

        // ---------- Players ----------

        public struct SeatInfo
        {
            public string playerId;
            public string name;
            public bool isHost;
            public bool isYou;
        }

        /// <summary>Everyone at the table in join order, which is the same on every device.</summary>
        public List<SeatInfo> GetSeats()
        {
            var seats = new List<SeatInfo>();
            if (session == null) return seats;

            foreach (IReadOnlyPlayer player in session.Players)
            {
                string name = player.Properties != null && player.Properties.TryGetValue(NameKey, out PlayerProperty p)
                    ? p.Value
                    : "";
                seats.Add(new SeatInfo
                {
                    playerId = player.Id,
                    name = string.IsNullOrWhiteSpace(name) ? $"Player {seats.Count + 1}" : name,
                    isHost = player.Id == session.Host,
                    isYou = player.Id == LocalPlayerId,
                });
            }
            return seats;
        }

        // ---------- Helpers ----------

        private async Task<bool> Run(string startMessage, Func<Task> work, string playerName)
        {
            if (session != null)
            {
                Status("You are already at a table. Leave it first.");
                return false;
            }

            SavedPlayerName = playerName;
            try
            {
                await SignInAsync();
                Status(startMessage);
                await work();
                return true;
            }
            catch (Exception e)
            {
                if (signIn != null && signIn.IsFaulted) signIn = null; // let a failed sign-in retry
                Debug.LogException(e);
                Status(Explain(e));
                return false;
            }
        }

        private static Dictionary<string, PlayerProperty> NameProperty(string playerName)
        {
            string name = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim();
            if (name.Length > 16) name = name.Substring(0, 16);
            return new Dictionary<string, PlayerProperty>
            {
                { NameKey, new PlayerProperty(name, VisibilityPropertyOptions.Member) },
            };
        }

        private void Attach(ISession joined)
        {
            session = joined;
            session.Changed += HandleChanged;
            session.PlayerJoined += HandlePlayerEvent;
            session.PlayerHasLeft += HandlePlayerEvent;
            session.RemovedFromSession += HandleRemoved;
            session.Deleted += HandleRemoved;
            OnSessionChanged?.Invoke();
        }

        private void Detach()
        {
            if (session == null) return;
            session.Changed -= HandleChanged;
            session.PlayerJoined -= HandlePlayerEvent;
            session.PlayerHasLeft -= HandlePlayerEvent;
            session.RemovedFromSession -= HandleRemoved;
            session.Deleted -= HandleRemoved;
            session = null;
            OnSessionChanged?.Invoke();
        }

        private void HandleChanged() => OnSessionChanged?.Invoke();
        private void HandlePlayerEvent(string playerId) => OnSessionChanged?.Invoke();

        private void HandleRemoved()
        {
            Detach();
            Status("The table was closed.");
        }

        private void Status(string message)
        {
            Debug.Log($"[Session] {message}");
            OnStatus?.Invoke(message);
        }

        private static string Explain(Exception e)
        {
            string text = e.Message ?? "";
            if (text.Contains("not linked") || text.Contains("projectId") || text.Contains("Project ID"))
                return "This project isn't linked to Unity Cloud (Edit > Project Settings > Services).";
            if (text.Contains("404") || text.Contains("not found") || text.Contains("NotFound"))
                return "No table with that code. Check the letters and try again.";
            if (text.Contains("full") || text.Contains("Full"))
                return "That table is full (6 players).";
            if (text.Contains("locked") || text.Contains("Locked"))
                return "That game has already started.";
            return $"Couldn't connect: {text}";
        }
    }
}

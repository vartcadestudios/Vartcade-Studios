using System;
using System.Collections.Generic;
using TDTS.Data;

namespace TDTS.Managers
{
    /// <summary>
    /// What the table UI reads and the buttons it presses, seen from the player at this device.
    /// Offline and on the host this is GameManager, which runs the rules. On an online client it is
    /// ClientGameView, fed by the host's per-seat snapshots, so it never holds other players' cards.
    /// </summary>
    public interface IGameView
    {
        GameState CurrentState { get; }
        int CurrentDay { get; }
        int SundownDay { get; }
        int BunkerCapacity { get; }
        int BunkerSeated { get; }
        int DeckCount { get; }
        bool LimitActions { get; }
        int ActionsLeft { get; }
        int PeeksThisTurn { get; }
        int TurnNumber { get; }
        DayRule CurrentDayRule { get; }
        RondoResult? TodaysRondo { get; }
        bool EyesClosed { get; }

        /// <summary>Everyone in turn order. Only LocalPlayer's hand holds real cards; use HandCount for the rest.</summary>
        IReadOnlyList<PlayerData> Players { get; }
        PlayerData LocalPlayer { get; }
        PlayerData ActivePlayer { get; }
        bool IsLocalTurn { get; }
        PlayerData GetPlayerAfterLocal(int seatsAfter);
        int HandCount(PlayerData player);

        /// <summary>The play log as this device's player is allowed to see it (spec C1).</summary>
        IReadOnlyList<string> PlayLog { get; }

        bool HasPendingTrade { get; }
        PlayerData TradeTo { get; }
        float TradeSecondsLeft { get; }

        PlayerData SelectedTarget { get; }
        int SelectedCardIndex { get; }

        event Action<GameState, GameState> OnStateChanged;
        event Action OnHandsChanged;
        event Action OnDeckChanged;
        event Action OnPlayLogChanged;
        event Action OnTargetChanged;
        event Action OnTurnChanged;
        event Action<RondoResult> OnRondoSpun;
        /// <summary>Your peek went through: flash the back of the card at this hand index.</summary>
        event Action<int> OnLocalPeek;

        // Buttons and taps for the player at this device
        void DrawForLocalPlayer();
        void TradeSelectedForLocalPlayer();
        void PlaySelectedForLocalPlayer();
        void StealForLocalPlayer();
        void SwapSelectedForLocalPlayer();
        void PassForLocalPlayer();
        void PeekForLocalPlayer(int handIndex);
        void AnswerTradeForLocalPlayer(bool accept);
        void SelectTarget(PlayerData player);
        void SelectCard(int handIndex);
    }

    /// <summary>The view the UI should read on this device.</summary>
    public static class GameView
    {
        // Enter Play Mode runs without a domain reload, so a destroyed view can linger here between runs.
        private static IGameView current;

        public static IGameView Current
        {
            get => current is UnityEngine.Object o && o == null ? null : current;
            set => current = value;
        }
    }
}

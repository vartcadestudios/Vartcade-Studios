using System.Collections.Generic;
using TDTS.Data;
using TDTS.Managers;

namespace TDTS.Bots
{
    /// <summary>
    /// What a bot is allowed to look at when it decides. The live game (GameManager) and the
    /// Balance Simulator both provide this, so the same BotBrain plays in either.
    /// Bots only read backs they have peeked or remember (BotMemory), never the real deck.
    /// </summary>
    public interface IBotTable
    {
        /// <summary>Every seat, in turn order.</summary>
        IReadOnlyList<PlayerData> Players { get; }
        bool IsHuman(PlayerData player);

        GameState State { get; }
        DayRule CurrentDayRule { get; }
        int CurrentDay { get; }
        int SundownDay { get; }
        int DeckCount { get; }
        bool TurnOrderReversed { get; }

        int ActionsLeft { get; }
        int PeeksThisTurn { get; }
        bool StoleOrSwappedThisTurn { get; }
        bool EyesClosed { get; }
    }
}

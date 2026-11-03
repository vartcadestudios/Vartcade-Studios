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

        /// <summary>R4: a peek is required every Night (a test switch can turn it off).</summary>
        bool PeekRequired { get; }

        /// <summary>True when qualifying needs 3 different Supply kinds rather than any 3 real Supply.</summary>
        bool NeedDifferentKinds { get; }

        /// <summary>Test rules: whether this thief may steal from this player at all.</summary>
        bool CanStealFrom(PlayerData thief, PlayerData victim);

        /// <summary>Test rule: a kept Morning card whose front has already been used.</summary>
        bool FrontUsed(CardData card);

        /// <summary>Test rule: playing a Morning card keeps it in your hand.</summary>
        bool KeepsMorningCards { get; }

        /// <summary>Test rule: cards given up for 1 real Supply (0 = conversion is off).</summary>
        int ConversionCost { get; }
        /// <summary>Real Supply cards still available to convert into.</summary>
        int ConversionsLeft { get; }
    }
}

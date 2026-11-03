using System.Collections.Generic;
using TDTS.Data;
using TDTS.Managers;

namespace TDTS.Bots
{
    /// <summary>The live game as a bot sees it.</summary>
    public class LiveBotTable : IBotTable
    {
        private readonly GameManager gm;

        public LiveBotTable(GameManager gm) => this.gm = gm;

        public IReadOnlyList<PlayerData> Players => gm.Players;
        public bool IsHuman(PlayerData player) => gm.IsHuman(player);
        public GameState State => gm.CurrentState;
        public DayRule CurrentDayRule => gm.CurrentDayRule;
        public int CurrentDay => gm.CurrentDay;
        public int SundownDay => gm.SundownDay;
        public int DeckCount => gm.DeckCount;
        public bool TurnOrderReversed => gm.TurnOrderReversed;
        public int ActionsLeft => gm.ActionsLeft;
        public int PeeksThisTurn => gm.PeeksThisTurn;
        public bool StoleOrSwappedThisTurn => gm.StoleOrSwappedThisTurn;
        public bool EyesClosed => gm.EyesClosed;
        public int ConversionCost => 0; // conversion is only a Balance Simulator test so far
        public int ConversionsLeft => 0;
        public bool PeekRequired => true;
        public bool NeedDifferentKinds => true;
        public bool CanStealFrom(PlayerData thief, PlayerData victim) => true;
        public bool FrontUsed(CardData card) => false;
        public bool KeepsMorningCards => false;
    }
}

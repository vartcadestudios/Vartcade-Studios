using System;
using TDTS.Bots;
using TDTS.Data;

namespace TDTS.Simulation
{
    /// <summary>The knobs the Balance Simulator can turn. Defaults match the real game.</summary>
    [Serializable]
    public class SimSettings
    {
        public int games = 500;
        public int seed = 0; // 0 = different games every run

        // One strategy per seat; the number of entries is the number of players (4 to 6)
        public BotStrategy[] seats = { BotStrategy.Aggressive, BotStrategy.Passive, BotStrategy.HumanLike, BotStrategy.Random };

        public int dayActions = 1;
        public int nightActions = 2;

        // Of the 54 cards without a Night back, how many have a real Supply back (the rest are Trash)
        public int supplyBacks = 36;

        public bool allCardsVisible = false; // every bot knows every back
        public bool alwaysDraw = false;      // a free card at the start of every Day turn
        public bool rondo = true;            // Rondo events from Day 6

        public const int NonNightBacks = DeckRecipe.CardCount - 18;

        public DeckRecipe BuildRecipe() => new DeckRecipe { supplyBacks = supplyBacks };
    }
}

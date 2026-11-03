using System;
using TDTS.Bots;
using TDTS.Data;

namespace TDTS.Simulation
{
    /// <summary>How drawing works in a simulated game.</summary>
    public enum DrawRule
    {
        Choice,       // the real game: Scavenge is one of your Day actions
        FreeEachDay,  // a free card at the start of every Day turn, on top of your actions
        NoDrawing,    // no Scavenge and no free card; only cards effects hand out new cards
        ForcedWhenLow,// with fewer than 3 cards, your Day turn starts with a draw that uses 1 action
        ForcedEvenDays// on Days 2, 4, 6, 8 and 10 your Day turn starts with a draw that uses 1 action
    }

    /// <summary>The knobs the Balance Simulator can turn. Defaults match the real game.</summary>
    [Serializable]
    public class SimSettings
    {
        public int games = 500;
        public int seed = 0; // 0 = different games every run

        // One strategy per seat; the number of entries is the number of players (4 to 6)
        public BotStrategy[] seats = { BotStrategy.Aggressive, BotStrategy.Passive, BotStrategy.HumanLike, BotStrategy.Random };

        // Casual players: nobody follows cards once they leave their hand, and everyone forgets
        // what they peeked (their own hand too) about a third of the time each Day
        public bool forgetfulPlayers = true;

        public int dayActions = 1;
        public int nightActions = 2;

        // Of the 54 cards without a Night back, how many have a real Supply back (the rest are Trash)
        public int supplyBacks = 36;

        public bool allCardsVisible = false; // every bot knows every back
        public DrawRule drawRule = DrawRule.Choice;

        // Resource conversion (Day action): give up this many cards for 1 real Supply. 0 = off.
        public int conversionCost = 0;
        public int conversionSupply = 12; // how many real Supply cards can be converted into in a game

        // R4: everyone must peek at least once a Night. With 1 Night action that peek is the whole turn.
        public bool peekRequired = true;

        // To qualify without the Key: 3 different Supply kinds (true, the rule) or any 3 real Supply (false, the old reading)
        public bool differentKinds = true;

        // Protection: once you've been stolen from, nobody can steal from you again until the next Night
        public bool stealProtection = false;

        public bool rondo = true;         // Rondo events from Day 6
        public bool strongRondo = false;  // from Day 4, Purge 10, Hunger/Outbreak take 2, Doomsday takes 2 days

        public const int NonNightBacks = DeckRecipe.CardCount - 18;

        public DeckRecipe BuildRecipe() => new DeckRecipe { supplyBacks = supplyBacks };
    }
}

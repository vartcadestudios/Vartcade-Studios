using System;
using System.Linq;
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

    /// <summary>How Steal works in a simulated game.</summary>
    public enum StealRule
    {
        PickCard,     // the real game: you choose which of their cards to take
        Blind,        // you take one of their cards at random
        AdjacentOnly  // you choose the card, but only from a player sitting next to you
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

        // Action cards in the 72: Morning fronts per type (Swap Deck, Reverse Turn, Skip Next Player,
        // Everyone +1 Card, Everyone Switches) and Night backs per type (Flashlight, Targeted Night Skip, Targeted Day Skip)
        public int[] morningFronts = { 4, 4, 4, 4, 4 };
        public int[] nightBacks = { 8, 6, 6 };

        // Of the cards without a Night back, how many have a real Supply back (the rest are Trash)
        public int supplyBacks = 36;

        public bool allCardsVisible = false; // every bot knows every back
        public DrawRule drawRule = DrawRule.Choice;

        // Resource conversion (Day action): give up this many cards for 1 real Supply. 0 = off.
        public int conversionCost = 0;
        public int conversionSupply = 12; // how many real Supply cards can be converted into in a game

        // R4: everyone must peek at least once a Night. With 1 Night action that peek is the whole turn.
        public bool peekRequired = true;

        // Free peeks each Night: they don't use a Night action. 0 = every peek uses an action (the game), 99 = all free
        public int freePeeks = 0;

        // To qualify without the Key: 3 different Supply kinds (true, the rule) or any 3 real Supply (false, the old reading)
        public bool differentKinds = true;

        // Protection: once you've been stolen from, nobody can steal from you again until the next Night
        public bool stealProtection = false;

        public StealRule stealRule = StealRule.PickCard;

        // A played Morning card stays in your hand (its back still counts); each front can only be used once
        public bool keepMorningCards = true;

        // Your first Night card each Night doesn't use an action
        public bool freeNightCard = false;

        // Doomsday Clock can't come up on the last Day, where it would do nothing
        public bool noDoomsdayOnLastDay = true;

        // From Day 2, the player furthest from surviving draws a free card at the start of each Day
        public bool catchUpDraw = true;

        // The Bunker Key is shuffled into the top half of the draw pile, so it's almost always found
        public bool keyInTopHalf = true;

        public int bunkerSeats = 0; // 0 = like the game: 2 seats, 3 with 5 or 6 players

        public int SeatsFor(int playerCount) => bunkerSeats > 0 ? bunkerSeats : playerCount >= 5 ? 3 : 2;

        public bool rondo = true;         // Rondo events from Day 6
        public bool strongRondo = false;  // from Day 4, Purge 10, Hunger/Outbreak take 2, Doomsday takes 2 days

        /// <summary>Backs left for Supply and Trash once the Night backs are placed.</summary>
        public int NonNightBacks => DeckRecipe.CardCount - nightBacks.Sum();

        public DeckRecipe BuildRecipe() => new DeckRecipe
        {
            morningFronts = (int[])morningFronts.Clone(),
            nightBacks = (int[])nightBacks.Clone(),
            supplyBacks = Math.Min(supplyBacks, NonNightBacks),
        };
    }
}

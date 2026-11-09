using System;
using System.Collections.Generic;

namespace TDTS.Data
{
    /// <summary>
    /// The exact card list: which back hides behind each front, instead of random front/back pairing.
    /// The game uses it (7 real / 4 Trash per kind, chosen from the Balance Simulator tests),
    /// and the Balance Simulator can change it to test other spreads.
    /// Supply-front cards: for each kind (Food, Water, Medicine, Entertainment), how many have a
    /// real Supply back of that kind, a Trash back, a Night Skip back or a Day Skip back.
    /// Morning-front cards: what their backs are, spread evenly across the five Morning cards.
    /// </summary>
    [Serializable]
    public class CardSpread
    {
        public bool use = true;

        // Backs of each kind's Supply-front cards
        public int realPerKind = 7;
        public int trashPerKind = 4;
        public int nightSkipPerKind = 1;
        public int daySkipPerKind = 1;

        // Backs of the Morning-front cards (should add up to the number of Morning fronts)
        public int morningSupply = 2;
        public int morningTrash = 6;
        public int morningFlashlight = 8;
        public int morningNightSkip = 2;
        public int morningDaySkip = 2;

        public int SupplyFrontsPerKind => realPerKind + trashPerKind + nightSkipPerKind + daySkipPerKind;

        public string Describe() =>
            $"Supply fronts: {realPerKind} real / {trashPerKind} Trash / {nightSkipPerKind} Night Skip / {daySkipPerKind} Day Skip per kind " +
            $"({realPerKind * 4} real, {trashPerKind * 4} Trash). Morning backs: {morningSupply} Supply, {morningTrash} Trash, " +
            $"{morningFlashlight} Flashlight, {morningNightSkip} Night Skip, {morningDaySkip} Day Skip";

        /// <summary>The shuffled draw pile: these cards plus 1 random Bunker Key (the other 3 Keys go out of the game).</summary>
        public List<CardData> Build(int[] morningFronts, Random rng, List<CardData> removedFromGame)
        {
            var pile = new List<CardData>();

            for (int k = 0; k < 4; k++)
            {
                var kind = (SupplyKind)k;
                for (int i = 0; i < realPerKind; i++) pile.Add(new CardData { front = FrontType.Supply, frontSupply = kind, back = BackType.Supply, backSupply = kind });
                for (int i = 0; i < trashPerKind; i++) pile.Add(new CardData { front = FrontType.Supply, frontSupply = kind, back = BackType.Trash });
                for (int i = 0; i < nightSkipPerKind; i++) pile.Add(new CardData { front = FrontType.Supply, frontSupply = kind, back = BackType.NightAction, nightAction = NightActionType.TargetedNightSkip });
                for (int i = 0; i < daySkipPerKind; i++) pile.Add(new CardData { front = FrontType.Supply, frontSupply = kind, back = BackType.NightAction, nightAction = NightActionType.TargetedDaySkip });
            }

            // Morning fronts interleaved by type (Swap, Reverse, Skip, +1, Switches, Swap, ...) so backs dealt
            // in order land evenly across the five Morning cards
            var mornings = new List<CardData>();
            var left = (int[])morningFronts.Clone();
            for (bool added = true; added;)
            {
                added = false;
                for (int t = 0; t < left.Length; t++)
                {
                    if (left[t] <= 0) continue;
                    left[t]--;
                    mornings.Add(new CardData { front = FrontType.MorningAction, morningAction = (MorningActionType)t });
                    added = true;
                }
            }

            var backs = new List<CardData>();
            for (int i = 0; i < morningSupply; i++) backs.Add(new CardData { back = BackType.Supply, backSupply = (SupplyKind)(i % 4) });
            for (int i = 0; i < morningTrash; i++) backs.Add(new CardData { back = BackType.Trash });
            for (int i = 0; i < morningFlashlight; i++) backs.Add(new CardData { back = BackType.NightAction, nightAction = NightActionType.Flashlight });
            for (int i = 0; i < morningNightSkip; i++) backs.Add(new CardData { back = BackType.NightAction, nightAction = NightActionType.TargetedNightSkip });
            for (int i = 0; i < morningDaySkip; i++) backs.Add(new CardData { back = BackType.NightAction, nightAction = NightActionType.TargetedDaySkip });
            while (backs.Count < mornings.Count) backs.Add(new CardData { back = BackType.Trash });

            for (int i = 0; i < mornings.Count; i++)
            {
                mornings[i].back = backs[i].back;
                mornings[i].backSupply = backs[i].backSupply;
                mornings[i].nightAction = backs[i].nightAction;
            }
            pile.AddRange(mornings);

            // The Key hides behind an ordinary Supply front, like in the game
            int keyInPlay = rng.Next(DeckRecipe.KeyDesigns);
            for (int i = 0; i < DeckRecipe.KeyDesigns; i++)
            {
                var key = new CardData { front = FrontType.Supply, frontSupply = (SupplyKind)i, back = BackType.BunkerKey, keyDesignIndex = i };
                if (i == keyInPlay) pile.Add(key);
                else removedFromGame.Add(key);
            }

            DeckBuilder.Shuffle(pile, rng);
            return pile;
        }
    }
}

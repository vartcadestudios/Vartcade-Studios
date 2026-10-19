using System.Collections.Generic;
using UnityEngine;

namespace TDTS.Data
{
    /// <summary>
    /// Builds the draw pile from a DeckRecipe (spec setup steps S2 and S3).
    /// Uses System.Random so a seed reproduces the same deck on every client.
    /// </summary>
    public static class DeckBuilder
    {
        /// <summary>
        /// Returns the shuffled draw pile: 72 cards plus 1 random Bunker Key.
        /// The 3 unused Keys are added to removedFromGame.
        /// </summary>
        public static List<CardData> BuildDrawPile(DeckRecipe recipe, System.Random rng, List<CardData> removedFromGame)
        {
            var fronts = BuildFronts(recipe);
            var backs = BuildBacks(recipe);

            // PLACEHOLDER (spec Open Items): fronts and backs are paired at random
            // until the team fixes which fronts hide which backs.
            Shuffle(backs, rng);

            var pile = new List<CardData>(DeckRecipe.CardCount + 1);
            for (int i = 0; i < DeckRecipe.CardCount; i++)
            {
                CardData card = fronts[i];
                card.back = backs[i].back;
                card.backSupply = backs[i].backSupply;
                card.nightAction = backs[i].nightAction;
                pile.Add(card);
            }

            // G2: pick 1 of the 4 Keys, the other 3 leave the game
            int keyInPlay = rng.Next(DeckRecipe.KeyDesigns);
            for (int i = 0; i < DeckRecipe.KeyDesigns; i++)
            {
                var key = new CardData { front = FrontType.BunkerKey, back = BackType.BunkerKey, keyDesignIndex = i };
                if (i == keyInPlay) pile.Add(key);
                else removedFromGame.Add(key);
            }

            // G3
            Shuffle(pile, rng);
            return pile;
        }

        private static List<CardData> BuildFronts(DeckRecipe recipe)
        {
            var fronts = new List<CardData>(DeckRecipe.CardCount);

            for (int v = 0; v < recipe.morningFronts.Length; v++)
                for (int i = 0; i < recipe.morningFronts[v]; i++)
                    fronts.Add(new CardData { front = FrontType.MorningAction, morningAction = (MorningActionType)v });

            if (fronts.Count > DeckRecipe.CardCount)
            {
                Debug.LogWarning($"[DeckBuilder] {fronts.Count} Morning fronts is more than {DeckRecipe.CardCount}; extra cards dropped.");
                fronts.RemoveRange(DeckRecipe.CardCount, fronts.Count - DeckRecipe.CardCount);
            }

            // PLACEHOLDER: Supply kinds split evenly until the team fixes the ratio
            for (int i = 0; fronts.Count < DeckRecipe.CardCount; i++)
                fronts.Add(new CardData { front = FrontType.Supply, frontSupply = (SupplyKind)(i % 4) });

            return fronts;
        }

        private static List<CardData> BuildBacks(DeckRecipe recipe)
        {
            var backs = new List<CardData>(DeckRecipe.CardCount);

            for (int v = 0; v < recipe.nightBacks.Length; v++)
                for (int i = 0; i < recipe.nightBacks[v]; i++)
                    backs.Add(new CardData { back = BackType.NightAction, nightAction = (NightActionType)v });

            for (int i = 0; i < recipe.supplyBacks && backs.Count < DeckRecipe.CardCount; i++)
                backs.Add(new CardData { back = BackType.Supply, backSupply = (SupplyKind)(i % 4) });

            if (backs.Count > DeckRecipe.CardCount)
            {
                Debug.LogWarning($"[DeckBuilder] {backs.Count} Night backs is more than {DeckRecipe.CardCount}; extra cards dropped.");
                backs.RemoveRange(DeckRecipe.CardCount, backs.Count - DeckRecipe.CardCount);
            }

            while (backs.Count < DeckRecipe.CardCount)
                backs.Add(new CardData { back = BackType.Trash });

            return backs;
        }

        /// <summary>Fisher-Yates shuffle.</summary>
        public static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}

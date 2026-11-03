using System;
using UnityEngine;

namespace TDTS.Data
{
    /// <summary>
    /// How many of each card go into the 72-card deck (spec 2.1).
    /// Edit in the Inspector on GameManager while the ratios are under playtest.
    /// </summary>
    [Serializable]
    public class DeckRecipe
    {
        public const int CardCount = 72;
        public const int KeyDesigns = 4;

        [Tooltip("Morning fronts per variant, in MorningActionType order: 4 each of Swap Deck, Reverse Turn, " +
                 "Skip Next Player, Everyone +1 Card, Everyone Switches. The rest of the fronts are Supply.")]
        public int[] morningFronts = { 4, 4, 4, 4, 4 };

        [Tooltip("Night backs per variant, in NightActionType order: 8 Flashlight, 6 Targeted Night Skip, 6 Targeted Day Skip.")]
        public int[] nightBacks = { 8, 6, 6 };

        [Tooltip("PLACEHOLDER (spec Open Items): Supply backs among the 52 non-Night backs. The rest (16) are Trash.")]
        public int supplyBacks = 36;
    }
}

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

        [Tooltip("Morning fronts per variant, in MorningActionType order. The rest of the fronts are Supply.")]
        public int[] morningFronts = { 4, 4, 4, 3, 3 };

        [Tooltip("Night backs per variant, in NightActionType order.")]
        public int[] nightBacks = { 6, 6, 6 };

        [Tooltip("PLACEHOLDER (spec Open Items): Supply backs among the non-Night backs. The rest are Trash.")]
        public int supplyBacks = 36;
    }
}

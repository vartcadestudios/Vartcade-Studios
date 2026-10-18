using System;

namespace TDTS.Data
{
    public enum CardType { Supply, Trash, BunkerKey, MorningAction, NightAction }

    // Rulebook 2.3: 6 variants x 3 copies
    public enum MorningActionType
    {
        SwapDeck,          // Swap deck with any player
        ReverseRotation,   // Reverse turn rotation
        SkipNextPlayer,    // Skip next player
        GiveOneCard,       // +1 card to another player
        GiveOneToEveryone, // +1 card to every player
        RotateCards        // Everyone switches cards per turn rotation
    }

    // Rulebook 2.4: 3 variants x 6 copies
    public enum NightActionType
    {
        Flashlight,    // See the backside of another player's card
        IndirectSwap,  // Swap 2 other players' decks
        ForceSkip      // Pick a player to skip the next Morning round
    }

    [Serializable]
    public class CardData
    {
        public CardType type;
        public bool isFoodSupply;               // Supply only: Hunger Disaster removes food-variant cards
        public MorningActionType morningAction; // MorningAction only
        public NightActionType nightAction;     // NightAction only
        public int keyDesignIndex;              // BunkerKey only: which of the 4 decoy fronts (0-3)

        public bool IsAction =>
            type == CardType.MorningAction || type == CardType.NightAction;

        // Supply, Trash and the Key look identical face-down
        public bool HasHiddenIdentity => !IsAction;

        public string DisplayName => type switch
        {
            CardType.Supply => "Supply",
            CardType.Trash => "Trash",
            CardType.BunkerKey => "Bunker Key",
            CardType.MorningAction => morningAction.ToString(),
            CardType.NightAction => nightAction.ToString(),
            _ => "?"
        };
    }
}
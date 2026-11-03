using System;

namespace TDTS.Data
{
    // Spec 2.1: every card has a front and a back. The back is its real value.
    public enum FrontType { Supply, MorningAction, BunkerKey }
    public enum BackType { Supply, Trash, NightAction, BunkerKey }

    public enum SupplyKind { Food, Water, Medicine, Entertainment }

    // 20 Morning fronts: 4 of each variant
    public enum MorningActionType
    {
        SwapDeck,         // Swap your whole hand with any player
        ReverseTurn,      // Reverse the turn order
        SkipNextPlayer,   // The next player loses their turn
        EveryonePlusOne,  // Every player gets +1 card
        EveryoneSwitches  // Everyone passes their hand along the turn order
    }

    // 20 Night backs: 8 Flashlight, 6 Targeted Night Skip, 6 Targeted Day Skip
    public enum NightActionType
    {
        Flashlight,        // One use: see the backs of every card in one hand, yours included
        TargetedNightSkip, // Skip a chosen player at night
        TargetedDaySkip    // Pick a player to skip the next Morning round
    }

    [Serializable]
    public class CardData
    {
        public FrontType front;
        public SupplyKind frontSupply;          // front == Supply only
        public MorningActionType morningAction; // front == MorningAction only

        public BackType back;
        public SupplyKind backSupply;           // back == Supply only
        public NightActionType nightAction;     // back == NightAction only

        public int keyDesignIndex;              // BunkerKey back only: which of the 4 Keys (0-3); its front is that Supply
        public bool peeked;                     // Spec V8: the owner has seen the back

        public bool IsKey => back == BackType.BunkerKey;
        public bool IsRealSupply => back == BackType.Supply;
        public bool HasMorningFront => front == FrontType.MorningAction;
        public bool HasNightBack => back == BackType.NightAction;

        public string FrontName => front switch
        {
            FrontType.Supply => frontSupply.ToString(),
            FrontType.MorningAction => NameOf(morningAction),
            FrontType.BunkerKey => $"Key {keyDesignIndex + 1}",
            _ => "?"
        };

        public string BackName => back switch
        {
            BackType.Supply => backSupply.ToString(),
            BackType.Trash => "Trash",
            BackType.NightAction => NameOf(nightAction),
            BackType.BunkerKey => "Bunker Key",
            _ => "?"
        };

        // Names as printed in the rulebook
        public static string NameOf(MorningActionType action) => action switch
        {
            MorningActionType.SwapDeck => "Swap Deck",
            MorningActionType.ReverseTurn => "Reverse Turn",
            MorningActionType.SkipNextPlayer => "Skip Next Player",
            MorningActionType.EveryonePlusOne => "Everyone +1 Card",
            MorningActionType.EveryoneSwitches => "Everyone Switches",
            _ => action.ToString()
        };

        public static string NameOf(NightActionType action) => action switch
        {
            NightActionType.Flashlight => "Flashlight",
            NightActionType.TargetedNightSkip => "Targeted Night Skip",
            NightActionType.TargetedDaySkip => "Targeted Day Skip",
            _ => action.ToString()
        };
    }
}

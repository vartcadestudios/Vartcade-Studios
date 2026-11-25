using System.Collections.Generic;
using UnityEngine;
using TDTS.Data;

namespace TDTS.UI
{
    /// <summary>
    /// The design team's card illustrations (Resources/Cards). Each card side maps to one image:
    ///   Fronts/front_food, front_swap_deck, ...      Supply and Morning fronts
    ///   Backs/back_food_supply, back_food_trash, back_food_key, back_flashlight, back_night_skip, back_day_skip
    ///   Days/day_01 ... day_10, night_01 ... night_10  the phase cards (PhaseCalendar)
    /// Every lookup returns null if the image is missing, so callers can fall back to the old coloured card.
    /// Images are 640 x 902 (card ratio 0.709), exported at about twice their largest size on screen
    /// so they stay sharp; mipmaps keep the small table cards smooth.
    /// </summary>
    public static class CardArt
    {
        public const float Aspect = 1240f / 1748f; // width / height of every card image

        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Front(CardData card)
        {
            if (card == null) return null;
            return card.front switch
            {
                FrontType.MorningAction => Load("Fronts/front_" + MorningKey(card.morningAction)),
                _ => Load("Fronts/front_" + KindKey(card.frontSupply)) // a Bunker Key's front is its Supply front
            };
        }

        public static Sprite Back(CardData card)
        {
            if (card == null) return null;
            switch (card.back)
            {
                case BackType.Supply: return Load($"Backs/back_{KindKey(card.backSupply)}_supply");
                case BackType.BunkerKey: return Load($"Backs/back_{KindKey(card.frontSupply)}_key");
                case BackType.Trash: return Load($"Backs/back_{KindKey(TrashKind(card))}_trash");
                case BackType.NightAction:
                    return card.nightAction switch
                    {
                        NightActionType.Flashlight => Load("Backs/back_flashlight"),
                        NightActionType.TargetedDaySkip => Load("Backs/back_day_skip"),
                        _ => Load("Backs/back_night_skip")
                    };
            }
            return null;
        }

        public static Sprite Side(CardData card, bool back) => back ? Back(card) : Front(card);

        /// <summary>The face-down back (draw pile): "TEN DAYS TO SUNDOWN" on the Night card design.</summary>
        public static Sprite FaceDown() => Load("Backs/back_facedown");

        /// <summary>The big Day / Night card shown when a phase starts (day is 1 to 10).</summary>
        public static Sprite Phase(int day, bool night) =>
            Load($"Days/{(night ? "night" : "day")}_{Mathf.Clamp(day, 1, 10):00}");

        /// <summary>
        /// Trash has art in each kind's colour. A Supply-front card uses its own kind; a Morning-front
        /// card has no kind, so it takes one from its Morning type (open question: confirm with the design team).
        /// </summary>
        private static SupplyKind TrashKind(CardData card) =>
            card.front == FrontType.MorningAction ? (SupplyKind)((int)card.morningAction % 4) : card.frontSupply;

        private static string KindKey(SupplyKind kind) => kind switch
        {
            SupplyKind.Food => "food",
            SupplyKind.Water => "water",
            SupplyKind.Medicine => "medicine",
            _ => "entertainment"
        };

        private static string MorningKey(MorningActionType action) => action switch
        {
            MorningActionType.SwapDeck => "swap_deck",
            MorningActionType.ReverseTurn => "reverse_turn",
            MorningActionType.SkipNextPlayer => "skip_next_player",
            MorningActionType.EveryonePlusOne => "everyone_plus_one",
            _ => "everyone_switches"
        };

        private static Sprite Load(string path)
        {
            if (cache.TryGetValue(path, out Sprite sprite)) return sprite;

            sprite = Resources.Load<Sprite>("Cards/" + path);
            if (sprite == null)
            {
                // Imported as a plain texture on some machine: make the sprite here instead
                Texture2D texture = Resources.Load<Texture2D>("Cards/" + path);
                if (texture != null)
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            }

            cache[path] = sprite; // null is cached too, so a missing image is only looked for once
            return sprite;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TDTS.Managers;

namespace TDTS.UI
{
    /// <summary>
    /// The sticker badge for a Rondo result (Resources/Rondo/rondo_*.png) and its little loop of motion:
    /// flames flicker, the tornado spins, the clock ticks, the dead bulb dims, and so on.
    /// With Reduce motion on, the badge stays still. RondoDial puts one on its banner.
    /// </summary>
    public class RondoBadge : MonoBehaviour
    {
        private RondoResult result;
        private RectTransform rect;
        private Image image;
        private Vector2 home;
        private float started;

        public static string FileFor(RondoResult result) => result switch
        {
            RondoResult.Purge => "rondo_purge",
            RondoResult.TradingOnly => "rondo_trading_only",
            RondoResult.HungerDisaster => "rondo_hunger",
            RondoResult.Tornado => "rondo_tornado",
            RondoResult.DoomsdayClock => "rondo_doomsday",
            RondoResult.Blackout => "rondo_blackout",
            RondoResult.Outbreak => "rondo_outbreak",
            _ => "rondo_peaceful"
        };

        public static Sprite SpriteFor(RondoResult result) => Resources.Load<Sprite>("Rondo/" + FileFor(result));

        /// <summary>Adds the badge to parent, or returns null if its image is missing.</summary>
        public static RondoBadge Create(Transform parent, RondoResult result, Vector2 anchor, Vector2 position, float size)
        {
            Sprite sprite = SpriteFor(result);
            if (sprite == null) return null;

            var rect = SundownArt.NewRect("Badge", parent, anchor, anchor, Vector2.zero, Vector2.zero);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = position;
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var badge = rect.gameObject.AddComponent<RondoBadge>();
            badge.result = result;
            badge.rect = rect;
            badge.image = image;
            badge.home = position;
            badge.started = Time.time;
            return badge;
        }

        private void Update()
        {
            float t = Time.time - started;

            // Pop in over the first quarter second
            float pop = Mathf.Clamp01(t / 0.25f);
            float popScale = pop < 1f ? Mathf.Lerp(0.3f, 1.12f, pop) : Mathf.Lerp(1.12f, 1f, Mathf.Clamp01((t - 0.25f) / 0.15f));
            if (GameSettings.ReduceMotion)
            {
                rect.localScale = Vector3.one * popScale;
                return;
            }

            float scale = 1f, angle = 0f;
            Vector2 offset = Vector2.zero;
            Color tint = Color.white;

            switch (result)
            {
                case RondoResult.Purge: // flames flicker
                    scale = 1f + 0.04f * Mathf.Sin(t * 18f) * Mathf.Sin(t * 7f);
                    break;
                case RondoResult.TradingOnly: // rocks back and forth like a swap
                    angle = 14f * Mathf.Sin(t * 3f);
                    break;
                case RondoResult.HungerDisaster: // nervous shake
                    angle = 3f * Mathf.Sin(t * 40f) * (0.5f + 0.5f * Mathf.Sin(t * 2f));
                    break;
                case RondoResult.Tornado: // spins
                    angle = -t * 220f;
                    break;
                case RondoResult.DoomsdayClock: // ticks once a second with a jolt
                    float tick = t - Mathf.Floor(t);
                    scale = 1f + 0.06f * Mathf.Max(0f, 1f - tick * 6f);
                    angle = tick < 0.08f ? -3f : 0f;
                    break;
                case RondoResult.Blackout: // slow dim flicker, kept under 3 flashes a second
                    float dim = Mathf.PerlinNoise(t * 2.5f, 0.3f);
                    tint = Color.Lerp(new Color(0.45f, 0.45f, 0.5f, 1f), Color.white, dim);
                    break;
                case RondoResult.Outbreak: // queasy wobble
                    angle = 6f * Mathf.Sin(t * 2.2f);
                    scale = 1f + 0.03f * Mathf.Sin(t * 4.4f);
                    break;
                default: // Peaceful Day: floats and turns gently
                    offset = new Vector2(0f, 8f * Mathf.Sin(t * 2f));
                    angle = 5f * Mathf.Sin(t * 1.3f);
                    break;
            }

            rect.localScale = Vector3.one * (popScale * scale);
            rect.localEulerAngles = new Vector3(0f, 0f, angle);
            rect.anchoredPosition = home + offset;
            image.color = tint;
        }
    }
}

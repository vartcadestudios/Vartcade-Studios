using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// Loads the v3 art (Resources/RoomV3, StreetV3, UI3, Dial, Wheel, Overlays) and places it.
    /// Positions are in the 1920 x 1080 reference frame the art was drawn in, measured from the top-left,
    /// and become anchors, so every layer stretches with the screen exactly like the rest of the UI.
    /// Every screen checks <see cref="Has"/> first and falls back to the older code-drawn look if the art is missing.
    /// </summary>
    public static class ArtKit
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string path)
        {
            if (Cache.TryGetValue(path, out Sprite sprite)) return sprite;
            sprite = Resources.Load<Sprite>(path);
            Cache[path] = sprite;
            return sprite;
        }

        public static bool Has(string path) => Get(path) != null;

        /// <summary>A full-screen image (backgrounds, frames, overlays).</summary>
        public static Image Full(Transform parent, string name, Sprite sprite, Color? tint = null)
        {
            var rect = NewRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = tint ?? Color.white;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// An image over a box of the reference frame (x, y = top-left corner, in 1920 x 1080 pixels).
        /// pivotPx is the turning point, also in reference pixels (null = the middle of the box).
        /// </summary>
        public static Image Box(Transform parent, string name, Sprite sprite, float x, float y, float w, float h, Vector2? pivotPx = null)
        {
            var rect = NewRect(name, parent, new Vector2(x / 1920f, 1f - (y + h) / 1080f), new Vector2((x + w) / 1920f, 1f - y / 1080f),
                Vector2.zero, Vector2.zero);
            if (pivotPx.HasValue)
            {
                Vector2 p = pivotPx.Value;
                rect.pivot = new Vector2((p.x - x) / w, 1f - (p.y - y) / h);
            }
            rect.anchoredPosition = Vector2.zero;
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        public static Image Box(Transform parent, string name, Sprite sprite, int[] r, Vector2? pivotPx = null)
            => Box(parent, name, sprite, r[0], r[1], r[2], r[3], pivotPx);

        /// <summary>A sprite button skin: 9-sliced when the sprite has borders, white tint so the art keeps its colours.</summary>
        public static void Skin(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.type = sprite != null && sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Color.white;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace TDTS.UI
{
    /// <summary>
    /// The sundown look shared by the Lobby and the Game table: palette, plus shapes and glows
    /// drawn in code at runtime, so the project needs no texture files (and no generated art).
    /// </summary>
    public static class SundownArt
    {
        // Night sky fading into a sundown glow
        public static readonly Color SkyTop = new Color32(0x0B, 0x0E, 0x1A, 255);
        public static readonly Color SkyMid = new Color32(0x1A, 0x1C, 0x33, 255);
        public static readonly Color SkyGlow = new Color32(0x6B, 0x33, 0x1E, 255);
        public static readonly Color SunColor = new Color32(0xF2, 0x8C, 0x3A, 255);

        public static readonly Color PanelColor = new Color32(0x12, 0x15, 0x22, 255);
        public static readonly Color ButtonColor = new Color32(0x2A, 0x31, 0x45, 255);
        public static readonly Color AccentColor = new Color32(0xE0, 0x7A, 0x2E, 255);
        public static readonly Color TextColor = new Color32(0xE8, 0xE4, 0xDA, 255);
        private static readonly Color CardBackEdge = new Color32(0x6B, 0x33, 0x1E, 255);
        private static readonly Color CardBackColor = new Color32(0x1A, 0x1C, 0x33, 255);

        private static Sprite roundedSprite;
        private static Sprite circleSprite;
        private static Texture2D glowTexture;
        private static Texture2D poolTexture;

        /// <summary>Turns an Image into a rounded panel. Bigger cornerScale = rounder corners.</summary>
        public static Image Rounded(Image image, Color color, float cornerScale = 24)
        {
            image.sprite = RoundedSprite();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 32f / cornerScale;
            image.color = color;
            return image;
        }

        /// <summary>Button colours for a white button graphic: hover lightens, press darkens, disabled fades.</summary>
        public static ColorBlock Tints(Color color)
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
            colors.selectedColor = color;
            colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            colors.disabledColor = new Color(color.r, color.g, color.b, 0.4f);
            colors.fadeDuration = 0.08f;
            return colors;
        }

        /// <summary>
        /// The back of a card, the same for every card: a sun setting behind a horizon in a rust frame.
        /// Purely decorative, so it never blocks taps.
        /// </summary>
        public static RectTransform CardBack(Transform parent, Vector2 size)
        {
            var card = NewRect("CardBack", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            card.sizeDelta = size;
            Decor(Rounded(card.gameObject.AddComponent<Image>(), CardBackEdge, 8));

            float edge = Mathf.Max(2f, size.x * 0.05f);
            var inner = NewRect("Inner", card, Vector2.zero, Vector2.one, new Vector2(edge, edge), new Vector2(-edge, -edge));
            Decor(Rounded(inner.gameObject.AddComponent<Image>(), CardBackColor, 6));

            float sun = Mathf.Min(size.x, size.y) * 0.42f;
            var disc = NewRect("Sun", inner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            disc.sizeDelta = new Vector2(sun, sun);
            Image discImage = Decor(disc.gameObject.AddComponent<Image>());
            discImage.sprite = CircleSprite();
            discImage.color = SunColor;

            // Hide the lower half of the sun below the horizon
            var below = NewRect("Ground", inner, Vector2.zero, new Vector2(1f, 0.5f), new Vector2(edge, edge), new Vector2(-edge, 0f));
            Decor(below.gameObject.AddComponent<Image>()).color = CardBackColor;
            var line = NewRect("Horizon", inner, new Vector2(0.12f, 0.5f), new Vector2(0.88f, 0.5f), new Vector2(0f, -1f), new Vector2(0f, 1f));
            Decor(line.gameObject.AddComponent<Image>()).color = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.8f);
            return card;
        }

        private static Image Decor(Image image)
        {
            image.raycastTarget = false;
            return image;
        }

        public static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        /// <summary>A white rounded square with 32px corners, 9-sliced so it stretches to any size.</summary>
        public static Sprite RoundedSprite()
        {
            if (roundedSprite != null) return roundedSprite;
            const int radius = 32, size = radius * 2 + 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0f, Mathf.Abs(x + 0.5f - size / 2f) - (size / 2f - radius));
                float dy = Mathf.Max(0f, Mathf.Abs(y + 0.5f - size / 2f) - (size / 2f - radius));
                float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            tex.Apply();
            roundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return roundedSprite;
        }

        /// <summary>A white disc. With Image.Type.Filled + Radial360 it also cuts pie wedges.</summary>
        public static Sprite CircleSprite()
        {
            if (circleSprite != null) return circleSprite;
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(size / 2f - d)));
            }
            tex.Apply();
            circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return circleSprite;
        }

        /// <summary>The Lobby sky: sundown glow at the bottom, night at the top.</summary>
        public static Texture2D SkyTexture() => SkyTexture(SkyGlow, SkyMid, SkyTop);

        /// <summary>A vertical sky gradient, bottom colour to top colour through a middle band.</summary>
        public static Texture2D SkyTexture(Color bottom, Color middle, Color top)
        {
            var tex = new Texture2D(1, 256, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 256; y++)
            {
                float t = y / 255f; // 0 = bottom
                Color c = t < 0.45f
                    ? Color.Lerp(bottom, middle, Mathf.SmoothStep(0f, 1f, t / 0.45f))
                    : Color.Lerp(middle, top, Mathf.SmoothStep(0f, 1f, (t - 0.45f) / 0.55f));
                tex.SetPixel(0, y, c);
            }
            tex.Apply();
            return tex;
        }

        /// <summary>A soft round glow: bright core fading out, used for the setting sun.</summary>
        public static Texture2D GlowTexture()
        {
            if (glowTexture != null) return glowTexture;
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                float core = d < 0.32f ? 1f : 0f;
                float halo = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(core * 0.9f + halo * 0.7f)));
            }
            tex.Apply();
            glowTexture = tex;
            return tex;
        }

        /// <summary>A soft pool of light with no hard edge, like a flashlight on a table. Stretch it to an oval.</summary>
        public static Texture2D PoolTexture()
        {
            if (poolTexture != null) return poolTexture;
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - d))));
            }
            tex.Apply();
            poolTexture = tex;
            return tex;
        }
    }
}

using UnityEngine;

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

        private static Sprite roundedSprite;
        private static Sprite circleSprite;
        private static Texture2D glowTexture;
        private static Texture2D beamTexture;

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

        /// <summary>A flashlight beam: narrow at the top, widening and fading toward the bottom.</summary>
        public static Texture2D BeamTexture()
        {
            if (beamTexture != null) return beamTexture;
            const int w = 256, h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float down = 1f - y / (h - 1f);                     // 0 at the lamp, 1 at the far end
                float halfWidth = Mathf.Lerp(0.04f, 0.5f, down);    // the cone widens as it falls
                float across = Mathf.Abs(x / (w - 1f) - 0.5f) / halfWidth;
                float edge = Mathf.Clamp01((1f - across) * 4f);      // soft sides
                float falloff = Mathf.Lerp(1f, 0.55f, down);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, edge * falloff));
            }
            tex.Apply();
            beamTexture = tex;
            return tex;
        }
    }
}

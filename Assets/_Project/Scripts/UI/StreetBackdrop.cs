using UnityEngine;
using UnityEngine.UI;
using TDTS.Managers;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// The v3 main-menu backdrop: the Filipino street at sundown (M-A) as painted layers.
    /// Clouds drift (A03), the crows cross the sky (A04) and the laundry swings (A05); a dark wash on
    /// each side keeps the buttons and the mirror readable. Sprites: Resources/StreetV3.
    /// </summary>
    public class StreetBackdrop : MonoBehaviour
    {
        private RectTransform clouds, crows, laundry;

        public static bool Available => ArtKit.Has("StreetV3/street_base");

        public static StreetBackdrop Create(Transform canvas)
        {
            var root = NewRect("StreetBackdrop", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.SetAsFirstSibling();
            var backdrop = root.gameObject.AddComponent<StreetBackdrop>();
            backdrop.Build(root);
            return backdrop;
        }

        private void Build(RectTransform root)
        {
            ArtKit.Full(root, "Street", ArtKit.Get("StreetV3/street_base"));
            clouds = ArtKit.Box(root, "Clouds", ArtKit.Get("StreetV3/street_clouds"), RoomArtLayout.StreetClouds).rectTransform;
            crows = ArtKit.Box(root, "Crows", ArtKit.Get("StreetV3/street_crows"), RoomArtLayout.StreetCrows).rectTransform;
            int[] l = RoomArtLayout.StreetLaundry;
            laundry = ArtKit.Box(root, "Laundry", ArtKit.Get("StreetV3/street_laundry"), l, new Vector2(l[0] + l[2] / 2f, l[1])).rectTransform;

            // Dark washes behind the buttons (left) and the mirror (right)
            Wash(root, "WashLeft", new Vector2(0f, 0f), new Vector2(0.55f, 1f), 0.78f, true);
            Wash(root, "WashRight", new Vector2(0.6f, 0f), new Vector2(1f, 1f), 0.55f, false);
            ScreenOverlays.Create(root, false); // OV10 warm + OV7 paper
        }

        private static void Wash(Transform parent, string name, Vector2 min, Vector2 max, float strength, bool darkOnLeft)
        {
            var rect = NewRect(name, parent, min, max, Vector2.zero, Vector2.zero);
            var image = rect.gameObject.AddComponent<RawImage>();
            var tex = new Texture2D(64, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < 64; x++)
            {
                float t = x / 63f;
                float a = darkOnLeft ? Mathf.Lerp(strength, 0f, Mathf.SmoothStep(0.55f, 1f, t)) : Mathf.Lerp(0f, strength, Mathf.SmoothStep(0f, 0.6f, t));
                tex.SetPixel(x, 0, new Color(0.055f, 0.03f, 0.024f, a));
            }
            tex.Apply();
            image.texture = tex;
            image.raycastTarget = false;
        }

        private void Update()
        {
            if (GameSettings.ReduceMotion) return;
            float t = Time.unscaledTime;
            clouds.anchoredPosition = new Vector2(Mathf.Sin(t * 0.05f) * 45f, 0f);
            crows.anchoredPosition = new Vector2(-Mathf.Repeat(t * 22f, 360f), -Mathf.Repeat(t * 22f, 360f) * 0.12f);
            laundry.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * 1.7f) * 1.4f);
        }
    }
}

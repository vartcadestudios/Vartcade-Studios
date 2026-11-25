using UnityEngine;
using UnityEngine.UI;
using TDTS.Managers;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// The v3 day/night dial at the left of the top bar (after the day-night GIFs in the reference zip):
    /// a porthole whose sky disc has day on top and night below and turns half a circle when Night falls
    /// and again when Day comes, rooftops in front, and a ring of 24 ticks that tick round during your turn.
    /// Sprites: Resources/Dial (dial_disc, dial_horizon, dial_frame, dial_tick).
    /// </summary>
    public class DayNightDial : MonoBehaviour
    {
        private const float Glass = 118f;          // the round window
        private const int TickCount = 24;
        private RectTransform disc;
        private Image[] ticks;
        private float angle;                       // 0 = Day, 180 = Night (the disc turns clockwise)
        private bool wasNight;

        public static bool Available => ArtKit.Has("Dial/dial_disc");

        public static DayNightDial Create(Transform parent, Vector2 anchor, Vector2 position)
        {
            if (!Available) return null;
            var root = NewRect("DayNightDial", parent, anchor, anchor, Vector2.zero, Vector2.zero);
            root.sizeDelta = new Vector2(200f, 200f);
            root.anchoredPosition = position;
            var dial = root.gameObject.AddComponent<DayNightDial>();
            dial.Build(root);
            return dial;
        }

        private void Build(RectTransform root)
        {
            ticks = new Image[TickCount];
            Sprite tick = ArtKit.Get("Dial/dial_tick");
            for (int i = 0; i < TickCount; i++)
            {
                float a = i * 360f / TickCount;
                var rect = NewRect("Tick", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                rect.sizeDelta = new Vector2(9f, 18f);
                rect.anchoredPosition = new Vector2(Mathf.Sin(a * Mathf.Deg2Rad), Mathf.Cos(a * Mathf.Deg2Rad)) * 91f;
                rect.localEulerAngles = new Vector3(0f, 0f, -a);
                ticks[i] = rect.gameObject.AddComponent<Image>();
                ticks[i].sprite = tick;
                ticks[i].raycastTarget = false;
            }

            var window = NewRect("Window", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            window.sizeDelta = new Vector2(Glass, Glass);
            Image mask = window.gameObject.AddComponent<Image>();
            mask.sprite = CircleSprite();
            mask.raycastTarget = false;
            window.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            disc = Layer("Disc", window, "Dial/dial_disc", Glass);
            Layer("Horizon", window, "Dial/dial_horizon", Glass);
            Layer("Frame", root, "Dial/dial_frame", Glass * 256f / 186f); // the frame's inner edge meets the glass
        }

        private static RectTransform Layer(string name, Transform parent, string sprite, float size)
        {
            var rect = NewRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rect.sizeDelta = new Vector2(size, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = ArtKit.Get(sprite);
            image.raycastTarget = false;
            return rect;
        }

        private void Update()
        {
            IGameView view = GameView.Current;
            if (view == null) return;
            bool still = GameSettings.ReduceMotion;

            // Each change of phase turns the disc half a circle, always the same way round
            bool night = view.EyesClosed;
            float target = night ? 180f : 0f;
            if (night != wasNight && !night) angle -= 360f; // Night -> Day keeps turning forward to 360 (= 0)
            wasNight = night;
            angle = still ? target : Mathf.MoveTowards(angle, target, 150f * Time.deltaTime);
            disc.localEulerAngles = new Vector3(0f, 0f, -angle);

            // The ticks tick round while it's your turn; dim otherwise
            bool yours = view.IsLocalTurn && view.CurrentState != GameState.Sundown;
            int lit = yours ? Mathf.FloorToInt(Time.time * 2f) % (TickCount + 1) : 0;
            for (int i = 0; i < TickCount; i++)
                ticks[i].color = new Color(1f, 1f, 1f, yours ? (i < lit ? 1f : 0.3f) : 0.18f);
        }
    }
}

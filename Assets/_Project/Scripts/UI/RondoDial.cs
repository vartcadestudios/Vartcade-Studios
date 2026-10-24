using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Managers;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// The Rondo as a dial in the middle of the table: one labelled wedge per result, each in its
    /// own colour, under a pointer. Covered until the Rondo's first day. Each morning it spins,
    /// lands, lights the result's wedge, and a banner says what it means.
    /// Takes over the board's old Rondo slot so it sits where the scene already left room for it.
    /// </summary>
    public class RondoDial : MonoBehaviour
    {
        public const float SpinSeconds = 2.6f;
        private const float Size = 196f;
        private const float BannerSeconds = 3.2f;

        private static readonly Color RimColor = new Color32(0x0B, 0x0E, 0x1A, 255);
        private static readonly Color DarkText = new Color32(0x1A, 0x1C, 0x33, 255);

        private IGameView view;
        private RectTransform wheel;
        private Image[] wedges;
        private GameObject cover;
        private GameObject chip;
        private TextMeshProUGUI chipText;

        private bool spinning;
        private float spinFrom, spinTo, spinStartedAt;
        private RondoResult? landed;

        private static int Count => Enum.GetValues(typeof(RondoResult)).Length;
        private static float WedgeAngle => 360f / Count;

        /// <summary>Builds the dial inside the board's Rondo slot, hiding what the scene had there.</summary>
        public static RondoDial Attach(GameObject slot)
        {
            Image old = slot.GetComponent<Image>();
            if (old != null) old.color = Color.clear;
            foreach (TextMeshProUGUI label in slot.GetComponentsInChildren<TextMeshProUGUI>(true))
                label.gameObject.SetActive(false);

            var dial = slot.AddComponent<RondoDial>();
            dial.Build();
            return dial;
        }

        // ---------- Words and colours for each result ----------

        public static string ShortName(RondoResult result) => result switch
        {
            RondoResult.Purge => "PURGE",
            RondoResult.TradingOnly => "TRADE ONLY",
            RondoResult.HungerDisaster => "HUNGER",
            RondoResult.Tornado => "TORNADO",
            RondoResult.DoomsdayClock => "DOOMSDAY",
            RondoResult.Blackout => "BLACKOUT",
            RondoResult.Outbreak => "OUTBREAK",
            RondoResult.PeacefulDay => "PEACE",
            _ => result.ToString().ToUpperInvariant(),
        };

        public static string Effect(RondoResult result) => result switch
        {
            RondoResult.Purge => "5 cards burn off the draw pile for good.",
            RondoResult.TradingOnly => "No Scavenge and no action cards today. Trade if you can.",
            RondoResult.HungerDisaster => "Everyone loses a real Food.",
            RondoResult.Tornado => "Every hand is shuffled together and dealt back out.",
            RondoResult.DoomsdayClock => "Sundown comes a day sooner.",
            RondoResult.Blackout => "Eyes stay closed all Day.",
            RondoResult.Outbreak => "Everyone loses a real Medicine or Entertainment.",
            RondoResult.PeacefulDay => "Nothing happens. Enjoy it while it lasts.",
            _ => "",
        };

        /// <summary>Each result's own colour, used by the dial, the banner and the sky's flash.</summary>
        public static Color ResultColor(RondoResult result) => result switch
        {
            RondoResult.Purge => new Color32(0xE2, 0x4B, 0x2A, 255),
            RondoResult.TradingOnly => new Color32(0xEF, 0x9F, 0x27, 255),
            RondoResult.HungerDisaster => new Color32(0x9C, 0x6A, 0x36, 255),
            RondoResult.Tornado => new Color32(0x85, 0xB7, 0xEB, 255),
            RondoResult.DoomsdayClock => new Color32(0xB8, 0x2E, 0x3A, 255),
            RondoResult.Blackout => new Color32(0x3E, 0x44, 0x6E, 255),
            RondoResult.Outbreak => new Color32(0x6E, 0xA6, 0x3A, 255),
            RondoResult.PeacefulDay => new Color32(0xFA, 0xD9, 0x8C, 255),
            _ => SunColor,
        };

        private static bool IsLight(Color c) => c.r * 0.3f + c.g * 0.59f + c.b * 0.11f > 0.55f;

        // ---------- Building ----------

        private void Build()
        {
            RectTransform root = (RectTransform)transform;

            Circle("Rim", root, Size + 16f, RimColor);
            Circle("Edge", root, Size + 6f, new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.55f));

            wheel = NewRect("Wheel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            wheel.sizeDelta = new Vector2(Size, Size);
            wedges = new Image[Count];
            for (int i = 0; i < Count; i++)
            {
                var result = (RondoResult)i;
                Color color = ResultColor(result);

                // A pie slice: a filled circle cut to one wedge, turned into place
                Image wedge = Circle($"Wedge {result}", wheel, Size, color);
                wedge.type = Image.Type.Filled;
                wedge.fillMethod = Image.FillMethod.Radial360;
                wedge.fillOrigin = (int)Image.Origin360.Top;
                wedge.fillClockwise = true;
                wedge.fillAmount = 1f / Count;
                wedge.rectTransform.localEulerAngles = new Vector3(0f, 0f, -i * WedgeAngle);
                wedges[i] = wedge;

                // Its name, across the middle of the slice; flipped on the lower half so it never reads upside down
                float angle = (i + 0.5f) * WedgeAngle;
                float radians = angle * Mathf.Deg2Rad;
                var label = NewText(ShortName(result), wheel, 13, FontStyles.Bold, TextAlignmentOptions.Center,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                label.rectTransform.sizeDelta = new Vector2(Size * 0.36f, 20f);
                label.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * (Size * 0.33f);
                bool lowerHalf = angle > 90f && angle < 270f;
                label.rectTransform.localEulerAngles = new Vector3(0f, 0f, -angle + (lowerHalf ? 180f : 0f));
                label.text = ShortName(result);
                label.color = IsLight(color) ? DarkText : TextColor;
                label.enableAutoSizing = true;
                label.fontSizeMin = 8f;
                label.fontSizeMax = 13f;
            }

            Circle("Hub", root, 50f, RimColor);
            var hubText = SundownFonts.UseTitle(NewText("HubText", root, 20, FontStyles.Normal, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-25f, -25f), new Vector2(25f, 25f)));
            hubText.text = "R";
            hubText.color = AccentColor;

            // Pointer: a small diamond biting into the rim at the top
            var pointer = NewRect("Pointer", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            pointer.sizeDelta = new Vector2(22f, 22f);
            pointer.anchoredPosition = new Vector2(0f, Size / 2f + 6f);
            pointer.localEulerAngles = new Vector3(0f, 0f, 45f);
            Image tip = Rounded(pointer.gameObject.AddComponent<Image>(), TextColor, 6);
            tip.raycastTarget = false;
            pointer.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.5f);

            // Covered until the Rondo wakes
            Image shade = Circle("Cover", root, Size + 18f, new Color(RimColor.r, RimColor.g, RimColor.b, 0.86f));
            cover = shade.gameObject;
            var waiting = NewText("Waiting", shade.transform, 20, FontStyles.Bold, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(20f, 20f), new Vector2(-20f, -20f));
            waiting.text = $"<size=130%>THE RONDO</size>\n<color=#A9AEBD>wakes on Day {GameManager.RondoStartDay}</color>";
            waiting.color = TextColor;

            // Today's result, in a pill under the dial
            var pill = NewRect("Result", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-120f, -22f), new Vector2(120f, 14f));
            Rounded(pill.gameObject.AddComponent<Image>(), RimColor, 18).raycastTarget = false;
            chipText = NewText("Text", pill, 18, FontStyles.Bold, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            chip = pill.gameObject;
            chip.SetActive(false);
        }

        private static Image Circle(string name, Transform parent, float size, Color color)
        {
            var rect = NewRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rect.sizeDelta = new Vector2(size, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = CircleSprite();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        // ---------- State ----------

        private void Start()
        {
            view = GameView.Current;
            if (view == null) return;
            view.OnRondoSpun += HandleSpun;
            view.OnStateChanged += HandleStateChanged;
            view.OnTurnChanged += Refresh;

            // Joining mid-game: show today's result without spinning
            if (view.TodaysRondo.HasValue) Land(view.TodaysRondo.Value);
            Refresh();
        }

        private void OnDestroy()
        {
            if (view == null) return;
            view.OnRondoSpun -= HandleSpun;
            view.OnStateChanged -= HandleStateChanged;
            view.OnTurnChanged -= Refresh;
        }

        private void HandleStateChanged(GameState previous, GameState next) => Refresh();

        private void Refresh()
        {
            if (view == null) return;
            cover.SetActive(view.CurrentDay < GameManager.RondoStartDay && !spinning);
            if (!spinning && !view.TodaysRondo.HasValue && landed.HasValue) Clear();
        }

        private void HandleSpun(RondoResult result)
        {
            float current = wheel.localEulerAngles.z;
            float target = (Count > 0 ? ((int)result + 0.5f) * WedgeAngle : 0f);
            spinFrom = current;
            spinTo = current + Mathf.Repeat(target - current, 360f) + 360f * 4f;
            spinStartedAt = Time.time;
            spinning = true;
            landed = result;
            cover.SetActive(false);
            chip.SetActive(true);
            chipText.text = "<color=#A9AEBD>The Rondo turns...</color>";
            foreach (Image w in wedges) w.color = WithAlpha(w.color, 1f);
        }

        private void Update()
        {
            if (!spinning) return;
            float t = Mathf.Clamp01((Time.time - spinStartedAt) / SpinSeconds);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            wheel.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(spinFrom, spinTo, eased));
            if (t < 1f) return;

            spinning = false;
            Land(landed.Value);
            StartCoroutine(Announce(landed.Value));
        }

        /// <summary>Stops on the result: its wedge stays bright, the rest dim, and the pill names it.</summary>
        private void Land(RondoResult result)
        {
            landed = result;
            wheel.localEulerAngles = new Vector3(0f, 0f, ((int)result + 0.5f) * WedgeAngle);
            for (int i = 0; i < wedges.Length; i++)
                wedges[i].color = WithAlpha(ResultColor((RondoResult)i), i == (int)result ? 1f : 0.35f);
            cover.SetActive(false);
            chip.SetActive(true);
            chipText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(ResultColor(result))}>{BoardController.RondoName(result)}</color>";
        }

        private void Clear()
        {
            landed = null;
            chip.SetActive(false);
            for (int i = 0; i < wedges.Length; i++) wedges[i].color = ResultColor((RondoResult)i);
        }

        // ---------- The banner ----------

        /// <summary>A big card across the middle of the screen naming the result and what it does.</summary>
        private IEnumerator Announce(RondoResult result)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) yield break;

            var banner = NewRect("RondoBanner", canvas.rootCanvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-520f, -130f), new Vector2(520f, 130f));
            banner.SetAsLastSibling();
            var fade = banner.gameObject.AddComponent<CanvasGroup>();
            fade.blocksRaycasts = false;
            fade.alpha = 0f;

            Rounded(banner.gameObject.AddComponent<Image>(), new Color(RimColor.r, RimColor.g, RimColor.b, 0.94f), 24).raycastTarget = false;
            var stripe = NewRect("Stripe", banner, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 14f), new Vector2(10f, -14f));
            Rounded(stripe.gameObject.AddComponent<Image>(), ResultColor(result), 4).raycastTarget = false;

            var eyebrow = NewText("Eyebrow", banner, 22, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), Vector2.one, new Vector2(30f, -66f), new Vector2(-30f, -24f));
            eyebrow.text = $"DAY {view.CurrentDay}  ·  THE RONDO LANDS ON";
            eyebrow.characterSpacing = 14f;
            eyebrow.color = AccentColor;

            var name = SundownFonts.UseTitle(NewText("Name", banner, 76, FontStyles.Normal, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(30f, 60f), new Vector2(-30f, -60f)));
            name.text = BoardController.RondoName(result).ToUpperInvariant();
            name.color = ResultColor(result) == ResultColor(RondoResult.Blackout) ? TextColor : ResultColor(result);

            var effect = NewText("Effect", banner, 26, FontStyles.Normal, TextAlignmentOptions.Center,
                Vector2.zero, new Vector2(1f, 0f), new Vector2(30f, 22f), new Vector2(-30f, 66f));
            effect.text = Effect(result);
            effect.color = TextColor;

            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.25f)
            {
                fade.alpha = t;
                banner.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, t);
                yield return null;
            }
            fade.alpha = 1f;
            banner.localScale = Vector3.one;
            yield return new WaitForSeconds(BannerSeconds);
            for (float t = 1f; t > 0f; t -= Time.deltaTime / 0.5f)
            {
                fade.alpha = t;
                yield return null;
            }
            Destroy(banner.gameObject);
        }

        private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}

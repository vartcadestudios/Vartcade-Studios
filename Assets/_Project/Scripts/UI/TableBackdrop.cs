using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Managers;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// The table's backdrop, drawn in code behind the Game canvas:
    /// - Sky clock: the sun sinks a notch each day toward the horizon, leaving a trail of past days.
    /// - Rondo sun: from the Rondo's first day the sun is the Rondo wheel; it spins to each result
    ///   and the sky flashes that result's colour.
    /// - Night flip: with eyes closed (Night, Blackout) the sky goes dark, the sun drops away, the
    ///   opponents' side of the table dims and a flashlight beam stays on your own hand.
    /// BoardController adds this at runtime; it finds the canvas pieces by name.
    /// </summary>
    public class TableBackdrop : MonoBehaviour
    {
        private const float HorizonHeight = 0.64f;  // of the screen, from the bottom
        private const float SunRadius = 72f;
        private const float SunRise = 290f;          // Day 1 height above the horizon
        private const float SunDrift = 420f;         // Day 1 sits this far left of centre
        private const float SpinSeconds = 2.6f;
        private const float NightFadeSpeed = 1.4f;

        private static readonly Color GroundDay = new Color32(0x12, 0x15, 0x22, 255);
        private static readonly Color GroundNight = new Color32(0x06, 0x07, 0x0D, 255);
        private static readonly Color Accent = new Color32(0xE0, 0x7A, 0x2E, 255);
        private static readonly Color WedgeDark = new Color32(0xC2, 0x5A, 0x22, 255);
        private static readonly Color WedgeLit = new Color32(0xFA, 0xC7, 0x75, 255);
        private static readonly Color TrailPast = new Color32(0xE0, 0x7A, 0x2E, 150);
        private static readonly Color TrailAhead = new Color32(0x5C, 0x62, 0x74, 150);
        private static readonly Color TrailSundown = new Color32(0xD9, 0x3B, 0x3B, 220);
        private static readonly Color PanelColor = new Color32(0x12, 0x15, 0x22, 190);
        private static readonly Color BeamColor = new Color32(0xF2, 0xE3, 0xB0, 255);
        private static readonly Color TextColor = new Color32(0xE8, 0xE4, 0xDA, 255);

        private IGameView view;

        private RectTransform skyRect;
        private RawImage duskSky, nightSky;
        private Image resultFlash;
        private CanvasGroup stars;
        private RectTransform sun, wheel, pointer;
        private RawImage sunGlow;
        private Image[] wedges;
        private RectTransform trail;
        private Image ground, horizonLine;
        private TextMeshProUGUI caption;
        private RectTransform captionRect;
        private Image tableDimmer;
        private RawImage beam;

        private Vector2 sunPosition;
        private bool sunPlaced;
        private float night;
        private int trailDay = -1, trailSundown = -1;

        private bool spinning;
        private float spinFrom, spinTo, spinStartedAt;
        private RondoResult? shownResult;
        private float flashStartedAt = -10f;

        /// <summary>The sun is the Rondo wheel, so the board's old wheel can stay hidden.</summary>
        public bool ShowsRondo => wheel != null;

        private static int WedgeCount => Enum.GetValues(typeof(RondoResult)).Length;
        private static float WedgeAngle => 360f / WedgeCount;

        private void Awake()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[TableBackdrop] No canvas in the scene.");
                enabled = false;
                return;
            }

            BuildBackdrop(canvas.transform);
            BuildNightPieces(canvas.transform);
            RestylePlayerZones();
        }

        private void Start()
        {
            view = GameView.Current;
            if (view == null) return;
            view.OnStateChanged += HandleStateChanged;
            view.OnTurnChanged += Refresh;
            view.OnRondoSpun += HandleRondoSpun;
            Refresh();
        }

        private void OnDestroy()
        {
            if (view == null) return;
            view.OnStateChanged -= HandleStateChanged;
            view.OnTurnChanged -= Refresh;
            view.OnRondoSpun -= HandleRondoSpun;
        }

        private void HandleStateChanged(GameState previous, GameState next) => Refresh();

        // ---------- Building ----------

        private void BuildBackdrop(Transform canvas)
        {
            var root = NewRect("TableBackdrop", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            root.SetAsFirstSibling(); // behind everything on the table

            // Sky: dusk deepens toward Sundown, night fades in with eyes closed
            skyRect = NewRect("Sky", root, new Vector2(0f, HorizonHeight), Vector2.one, Vector2.zero, Vector2.zero);
            Raw("DaySky", skyRect, SkyTexture(SkyGlow, SkyMid, SkyTop), Color.white);
            duskSky = Raw("DuskSky", skyRect,
                SkyTexture(new Color32(0x9A, 0x2E, 0x1A, 255), new Color32(0x3E, 0x18, 0x2A, 255), SkyMid), Color.clear);
            nightSky = Raw("NightSky", skyRect,
                SkyTexture(new Color32(0x10, 0x14, 0x28, 255), new Color32(0x08, 0x0A, 0x16, 255), new Color32(0x03, 0x04, 0x09, 255)), Color.clear);
            resultFlash = Fill("RondoFlash", skyRect, Color.clear);
            BuildStars(skyRect);

            // Past days and days to come, along the sun's path
            trail = NewRect("DayTrail", root, new Vector2(0.5f, HorizonHeight), new Vector2(0.5f, HorizonHeight), Vector2.zero, Vector2.zero);

            // The sun, which becomes the Rondo wheel
            sun = NewRect("Sun", root, new Vector2(0.5f, HorizonHeight), new Vector2(0.5f, HorizonHeight), Vector2.zero, Vector2.zero);
            sunGlow = Raw("Glow", sun, GlowTexture(), new Color(SunColor.r, SunColor.g, SunColor.b, 0.45f));
            Centre(sunGlow.rectTransform, SunRadius * 6f);

            wheel = NewRect("Wheel", sun, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Centre(wheel, SunRadius * 2f);
            wedges = new Image[WedgeCount];
            for (int i = 0; i < wedges.Length; i++)
            {
                Image wedge = Fill($"Wedge {(RondoResult)i}", wheel, SunColor);
                wedge.sprite = CircleSprite();
                wedge.type = Image.Type.Filled;
                wedge.fillMethod = Image.FillMethod.Radial360;
                wedge.fillOrigin = (int)Image.Origin360.Top;
                wedge.fillClockwise = true;
                wedge.fillAmount = 1f / wedges.Length;
                wedge.rectTransform.localEulerAngles = new Vector3(0f, 0f, -i * WedgeAngle);
                wedges[i] = wedge;
            }
            Image hub = Fill("Hub", wheel, WedgeDark);
            hub.sprite = CircleSprite();
            Centre(hub.rectTransform, SunRadius * 0.38f);

            pointer = NewRect("Pointer", sun, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Centre(pointer, 24f);
            pointer.anchoredPosition = new Vector2(0f, SunRadius + 8f);
            pointer.localEulerAngles = new Vector3(0f, 0f, 45f);
            Image pointerImage = Fill("Shape", pointer, TextColor);
            pointerImage.sprite = RoundedSprite();
            pointerImage.type = Image.Type.Sliced;
            pointerImage.pixelsPerUnitMultiplier = 8f;

            // The table: covers the lower half of a sun sitting on the horizon
            var groundRect = NewRect("Ground", root, Vector2.zero, new Vector2(1f, HorizonHeight), Vector2.zero, Vector2.zero);
            ground = Fill("Surface", groundRect, GroundDay);
            horizonLine = Fill("Horizon", groundRect, new Color(Accent.r, Accent.g, Accent.b, 0.6f));
            var lineRect = horizonLine.rectTransform;
            lineRect.anchorMin = new Vector2(0f, 1f);
            lineRect.anchorMax = Vector2.one;
            lineRect.offsetMin = new Vector2(0f, -2f);
            lineRect.offsetMax = Vector2.zero;

            // Day and Rondo result, riding beside the sun
            captionRect = NewRect("Caption", root, new Vector2(0.5f, HorizonHeight), new Vector2(0.5f, HorizonHeight), Vector2.zero, Vector2.zero);
            captionRect.pivot = new Vector2(0f, 0.5f);
            captionRect.sizeDelta = new Vector2(520f, 44f);
            caption = captionRect.gameObject.AddComponent<TextMeshProUGUI>();
            caption.fontSize = 28;
            caption.alignment = TextAlignmentOptions.MidlineLeft;
            caption.color = TextColor;
            caption.raycastTarget = false;
        }

        private void BuildStars(RectTransform sky)
        {
            var field = NewRect("Stars", sky, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            stars = field.gameObject.AddComponent<CanvasGroup>();
            stars.alpha = 0f;
            stars.blocksRaycasts = false;

            var rng = new System.Random(10); // the same sky every game
            for (int i = 0; i < 90; i++)
            {
                float x = (float)rng.NextDouble(), y = 0.12f + 0.88f * (float)rng.NextDouble();
                float size = 2f + 3f * (float)rng.NextDouble();
                var star = Fill("Star", field, new Color(0.91f, 0.89f, 0.85f, 0.35f + 0.6f * (float)rng.NextDouble()));
                star.sprite = CircleSprite();
                var rect = star.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(x, y);
                rect.sizeDelta = new Vector2(size, size);
            }
        }

        private void BuildNightPieces(Transform canvas)
        {
            // Dims the opponents and the middle of the table, but they stay tappable for Steal and Swap
            Transform table = canvas.Find("TableArea");
            if (table != null)
            {
                tableDimmer = Fill("NightDimmer", table, Color.clear);
                tableDimmer.transform.SetAsLastSibling();
            }

            // A flashlight beam falling on your own cards, behind them
            Transform hand = canvas.Find("HandArea");
            if (hand != null)
            {
                var beamRect = NewRect("Flashlight", hand, Vector2.zero, new Vector2(0f, 1f), new Vector2(20f, 0f), new Vector2(1080f, 140f));
                beamRect.SetAsFirstSibling();
                beam = beamRect.gameObject.AddComponent<RawImage>();
                beam.texture = BeamTexture();
                beam.color = Color.clear;
                beam.raycastTarget = false;
            }
        }

        /// <summary>Opponent panels become smoky glass so the sky shows through.</summary>
        private static void RestylePlayerZones()
        {
            foreach (PlayerZoneController zone in FindObjectsByType<PlayerZoneController>())
            {
                Image image = zone.GetComponent<Image>();
                if (image == null) continue;
                image.sprite = RoundedSprite();
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 32f / 18f;
                image.color = PanelColor; // read as the normal colour in PlayerZoneController.Start
            }
        }

        // ---------- State ----------

        private void Refresh()
        {
            if (view == null) return;
            if (view.CurrentDay != trailDay || view.SundownDay != trailSundown) BuildTrail();

            bool rondoDays = view.CurrentDay >= GameManager.RondoStartDay;
            pointer.gameObject.SetActive(rondoDays);
            for (int i = 0; i < wedges.Length; i++)
            {
                Color c = !rondoDays ? SunColor
                    : !spinning && shownResult.HasValue && (int)shownResult.Value == i ? WedgeLit
                    : i % 2 == 0 ? SunColor : WedgeDark;
                wedges[i].color = c;
            }

            if (!view.TodaysRondo.HasValue && !spinning) shownResult = null;
            UpdateCaption();
        }

        private void UpdateCaption()
        {
            string text = view.CurrentState == GameState.Sundown
                ? "<b>Sundown</b>"
                : $"Day <b>{view.CurrentDay}</b> of {view.SundownDay}";
            if (spinning) text += "  ·  The Rondo turns...";
            else if (shownResult.HasValue) text += $"  ·  <color=#FAC775>{BoardController.RondoName(shownResult.Value)}</color>";
            else if (view.EyesClosed && view.CurrentState == GameState.NightPhase) text += "  ·  Night";
            caption.text = text;
        }

        /// <summary>Where the sun sits on a given day: high and to the left on Day 1, on the horizon at Sundown.</summary>
        private Vector2 SunPositionOn(float day)
        {
            float t = Mathf.Clamp01((day - 1f) / Mathf.Max(1f, view.SundownDay - 1f));
            float eased = 1f - (1f - t) * (1f - t) * 0.25f - (1f - t) * 0.75f; // drops a little faster near the end
            return new Vector2(-SunDrift * (1f - t), SunRise * (1f - eased));
        }

        private void BuildTrail()
        {
            trailDay = view.CurrentDay;
            trailSundown = view.SundownDay;
            for (int i = trail.childCount - 1; i >= 0; i--) Destroy(trail.GetChild(i).gameObject);

            for (int day = 1; day <= view.SundownDay; day++)
            {
                if (day == view.CurrentDay) continue; // the sun is there
                bool last = day == view.SundownDay;
                Color color = last ? TrailSundown : day < view.CurrentDay ? TrailPast : TrailAhead;
                Image dot = Fill($"Day {day}", trail, color);
                dot.sprite = CircleSprite();
                float size = last ? 18f : day < view.CurrentDay ? 14f : 10f;
                Centre(dot.rectTransform, size);
                dot.rectTransform.anchoredPosition = SunPositionOn(day);
            }
        }

        private void HandleRondoSpun(RondoResult result)
        {
            // Turn the wheel a few times and stop with the result's wedge under the pointer
            float current = wheel.localEulerAngles.z;
            float landing = ((int)result + 0.5f) * WedgeAngle;
            float extra = Mathf.Repeat(landing - current, 360f);
            spinFrom = current;
            spinTo = current + extra + 360f * 4f;
            spinStartedAt = Time.time;
            spinning = true;
            shownResult = result;
            Refresh();
        }

        // ---------- Animation ----------

        private void Update()
        {
            if (view == null) return;

            // Sun glides to today's spot; at Sundown it sits half under the horizon
            Vector2 target = SunPositionOn(view.CurrentDay);
            if (view.CurrentState == GameState.Sundown) target = new Vector2(0f, -SunRadius * 0.35f);
            target.y -= night * (SunRadius * 3.4f); // eyes closed: the sun drops out of sight
            sunPosition = sunPlaced ? Vector2.Lerp(sunPosition, target, 1f - Mathf.Exp(-3f * Time.deltaTime)) : target;
            sunPlaced = true;
            sun.anchoredPosition = sunPosition;
            captionRect.anchoredPosition = new Vector2(sunPosition.x + SunRadius + 26f, Mathf.Max(sunPosition.y, 30f));

            // Night fades in and out
            float wantNight = view.EyesClosed ? 1f : 0f;
            if (!Mathf.Approximately(night, wantNight))
            {
                night = Mathf.MoveTowards(night, wantNight, NightFadeSpeed * Time.deltaTime);
                ApplyNight();
            }

            float progress = Mathf.Clamp01((view.CurrentDay - 1f) / Mathf.Max(1f, view.SundownDay - 1f));
            duskSky.color = new Color(1f, 1f, 1f, progress * 0.85f);

            if (spinning) Spin();

            float flash = 1f - (Time.time - flashStartedAt) / 1.8f;
            resultFlash.color = flash > 0f && shownResult.HasValue
                ? WithAlpha(ResultColor(shownResult.Value), 0.32f * flash * flash)
                : Color.clear;
        }

        private void Spin()
        {
            float t = Mathf.Clamp01((Time.time - spinStartedAt) / SpinSeconds);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            wheel.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(spinFrom, spinTo, eased));
            if (t < 1f) return;

            spinning = false;
            flashStartedAt = Time.time;
            Refresh();
        }

        private void ApplyNight()
        {
            nightSky.color = new Color(1f, 1f, 1f, night);
            stars.alpha = night;
            sunGlow.color = new Color(SunColor.r, SunColor.g, SunColor.b, 0.45f * (1f - night));
            ground.color = Color.Lerp(GroundDay, GroundNight, night);
            horizonLine.color = new Color(Accent.r, Accent.g, Accent.b, Mathf.Lerp(0.6f, 0.12f, night));
            if (tableDimmer != null) tableDimmer.color = new Color(0f, 0f, 0f, 0.5f * night);
            if (beam != null) beam.color = WithAlpha(BeamColor, 0.2f * night);
        }

        /// <summary>The sky's flash when the wheel lands: each result has its own mood.</summary>
        private static Color ResultColor(RondoResult result) => result switch
        {
            RondoResult.Purge => new Color32(0xE2, 0x4B, 0x2A, 255),
            RondoResult.TradingOnly => new Color32(0xEF, 0x9F, 0x27, 255),
            RondoResult.HungerDisaster => new Color32(0x8A, 0x5A, 0x2B, 255),
            RondoResult.Tornado => new Color32(0x85, 0xB7, 0xEB, 255),
            RondoResult.DoomsdayClock => new Color32(0xD9, 0x3B, 0x3B, 255),
            RondoResult.Blackout => new Color32(0x10, 0x12, 0x20, 255),
            RondoResult.Outbreak => new Color32(0x63, 0x99, 0x22, 255),
            RondoResult.PeacefulDay => new Color32(0xFA, 0xC7, 0x75, 255),
            _ => SunColor,
        };

        // ---------- Small helpers ----------

        private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        private static void Centre(RectTransform rect, float size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = Vector2.zero;
        }

        private static RawImage Raw(string name, Transform parent, Texture texture, Color color)
        {
            var image = NewRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image Fill(string name, Transform parent, Color color)
        {
            var image = NewRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}

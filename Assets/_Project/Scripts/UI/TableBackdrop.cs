using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Managers;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// The table's backdrop, drawn in code behind the Game canvas:
    /// - Sundown sky: the sun sinks toward the horizon day by day and the sky reddens.
    ///   When the Rondo dial lands, the sky flashes that result's colour.
    /// - Night flip: with eyes closed (Night, Blackout) the sky goes dark, the sun drops away, the
    ///   opponents' side of the table dims and a pool of flashlight stays on your own hand.
    /// BoardController adds this at runtime; it finds the canvas pieces by name.
    /// </summary>
    public class TableBackdrop : MonoBehaviour
    {
        private const float HorizonHeight = 0.64f;  // of the screen, from the bottom
        private const float SunRadius = 72f;
        private const float SunRise = 290f;          // Day 1 height above the horizon
        private const float SunDrift = 420f;         // Day 1 sits this far left of centre
        private const float NightFadeSpeed = 1.4f;

        private static readonly Color GroundDay = new Color32(0x12, 0x15, 0x22, 255);
        private static readonly Color GroundNight = new Color32(0x06, 0x07, 0x0D, 255);
        private static readonly Color GlassColor = new Color32(0x12, 0x15, 0x22, 190);
        private static readonly Color LightColor = new Color32(0xF2, 0xE3, 0xB0, 255);

        private IGameView view;

        private RectTransform skyRect;
        private RawImage duskSky, nightSky;
        private Image resultFlash;
        private CanvasGroup stars;
        private RectTransform sun;
        private RawImage sunGlow;
        private Image ground, horizonLine;
        private Image tableDimmer;
        private RawImage flashlight;
        private CanvasGroup sunFade;

        private Vector2 sunPosition;
        private bool sunPlaced;
        private float night;

        private RondoResult? flashResult;
        private float flashStartedAt = -10f;

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
            RestyleTable(canvas.transform);
        }

        private void Start()
        {
            view = GameView.Current;
            if (view == null) return;
            view.OnRondoSpun += HandleRondoSpun;
        }

        private void OnDestroy()
        {
            if (view == null) return;
            view.OnRondoSpun -= HandleRondoSpun;
        }

        // The sky flashes the result's colour as the dial comes to rest
        private void HandleRondoSpun(RondoResult result)
        {
            flashResult = result;
            flashStartedAt = Time.time + RondoDial.SpinSeconds;
        }

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

            // The sun, sinking a little each day
            sun = NewRect("Sun", root, new Vector2(0.5f, HorizonHeight), new Vector2(0.5f, HorizonHeight), Vector2.zero, Vector2.zero);
            sunFade = sun.gameObject.AddComponent<CanvasGroup>();
            sunFade.blocksRaycasts = false;
            sunGlow = Raw("Glow", sun, GlowTexture(), new Color(SunColor.r, SunColor.g, SunColor.b, 0.45f));
            Centre(sunGlow.rectTransform, SunRadius * 6f);
            Image disc = Fill("Disc", sun, SunColor);
            disc.sprite = CircleSprite();
            Centre(disc.rectTransform, SunRadius * 2f);

            // The table: covers the lower half of a sun sitting on the horizon
            var groundRect = NewRect("Ground", root, Vector2.zero, new Vector2(1f, HorizonHeight), Vector2.zero, Vector2.zero);
            ground = Fill("Surface", groundRect, GroundDay);
            horizonLine = Fill("Horizon", groundRect, new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.6f));
            var lineRect = horizonLine.rectTransform;
            lineRect.anchorMin = new Vector2(0f, 1f);
            lineRect.anchorMax = Vector2.one;
            lineRect.offsetMin = new Vector2(0f, -2f);
            lineRect.offsetMax = Vector2.zero;
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

            // A soft pool of flashlight on your own cards, behind them
            Transform hand = canvas.Find("HandArea");
            if (hand != null)
            {
                var poolRect = NewRect("Flashlight", hand, Vector2.zero, new Vector2(0f, 1f), new Vector2(-40f, 0f), new Vector2(1140f, 0f));
                poolRect.SetAsFirstSibling();
                flashlight = poolRect.gameObject.AddComponent<RawImage>();
                flashlight.texture = PoolTexture();
                flashlight.color = Color.clear;
                flashlight.raycastTarget = false;
            }
        }

        /// <summary>The draw pile becomes a stack of card backs; the log and divider match the sky.</summary>
        private static void RestyleTable(Transform canvas)
        {
            Transform cluster = canvas.Find("TableArea/CenterCluster");
            Transform pile = cluster != null ? cluster.Find("DrawPile") : null;
            if (pile != null)
            {
                Image hitArea = pile.GetComponent<Image>();
                if (hitArea != null) hitArea.color = Color.clear; // still catches the tap
                Vector2 size = ((RectTransform)pile).rect.size;
                for (int i = 2; i >= 0; i--)
                {
                    RectTransform back = CardBack(pile, size);
                    back.anchoredPosition = new Vector2(-4f * i, 4f * i);
                    back.SetSiblingIndex(2 - i);
                }

                TextMeshProUGUI count = pile.GetComponentInChildren<TextMeshProUGUI>();
                if (count != null)
                {
                    var band = NewRect("CountBand", pile, Vector2.zero, new Vector2(1f, 0.3f), new Vector2(8f, 8f), new Vector2(-8f, 0f));
                    Rounded(band.gameObject.AddComponent<Image>(), new Color(0f, 0f, 0f, 0.55f), 10).raycastTarget = false;
                    band.SetSiblingIndex(3);
                    RectTransform text = count.rectTransform;
                    text.anchorMin = Vector2.zero;
                    text.anchorMax = new Vector2(1f, 0.3f);
                    text.offsetMin = new Vector2(8f, 8f);
                    text.offsetMax = new Vector2(-8f, 0f);
                    count.color = TextColor;
                    count.enableAutoSizing = true;
                    count.fontSizeMin = 10f;
                    count.fontSizeMax = 24f;
                    count.transform.SetAsLastSibling();
                }
            }

            Transform log = cluster != null ? cluster.Find("PlaySpace") : null;
            Image logImage = log != null ? log.GetComponent<Image>() : null;
            if (logImage != null)
            {
                Rounded(logImage, GlassColor, 16);
                TextMeshProUGUI text = log.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.color = TextColor;
            }

            Transform divider = canvas.Find("HandArea/Divider");
            Image line = divider != null ? divider.GetComponent<Image>() : null;
            if (line != null) line.color = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.35f);
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
                image.color = GlassColor; // read as the normal colour in PlayerZoneController.Start
            }
        }

        // ---------- Animation ----------

        /// <summary>Where the sun sits on a given day: high and to the left on Day 1, on the horizon at Sundown.</summary>
        private Vector2 SunPositionOn(float day)
        {
            float t = Mathf.Clamp01((day - 1f) / Mathf.Max(1f, view.SundownDay - 1f));
            float eased = 1f - (1f - t) * (1f - t) * 0.25f - (1f - t) * 0.75f; // drops a little faster near the end
            return new Vector2(-SunDrift * (1f - t), SunRise * (1f - eased));
        }

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

            // Night fades in and out
            float wantNight = view.EyesClosed ? 1f : 0f;
            if (!Mathf.Approximately(night, wantNight))
            {
                night = Mathf.MoveTowards(night, wantNight, NightFadeSpeed * Time.deltaTime);
                ApplyNight();
            }

            float progress = Mathf.Clamp01((view.CurrentDay - 1f) / Mathf.Max(1f, view.SundownDay - 1f));
            duskSky.color = new Color(1f, 1f, 1f, progress * 0.85f);

            float since = Time.time - flashStartedAt;
            float flash = since >= 0f ? 1f - since / 1.8f : 0f;
            resultFlash.color = flash > 0f && flashResult.HasValue
                ? WithAlpha(RondoDial.ResultColor(flashResult.Value), 0.32f * flash * flash)
                : Color.clear;
        }

        private void ApplyNight()
        {
            nightSky.color = new Color(1f, 1f, 1f, night);
            stars.alpha = night;
            sunGlow.color = new Color(SunColor.r, SunColor.g, SunColor.b, 0.45f * (1f - night));
            ground.color = Color.Lerp(GroundDay, GroundNight, night);
            horizonLine.color = new Color(AccentColor.r, AccentColor.g, AccentColor.b, Mathf.Lerp(0.6f, 0.12f, night));
            if (tableDimmer != null) tableDimmer.color = new Color(0f, 0f, 0f, 0.5f * night);
            if (flashlight != null) flashlight.color = WithAlpha(LightColor, 0.16f * night);
            sunFade.alpha = 1f - night;
        }

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

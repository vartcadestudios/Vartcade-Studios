using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Managers;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// The countdown bar across the top of the table, built in code over the TopBar prefab:
    /// - Left: the day number and a row of day pips, one per day until Sundown (red), so you can
    ///   see time running out; a Doomsday Clock takes a pip away.
    /// - Middle: a Day / Night badge with a sun or moon, and whose turn it is.
    /// - Right: the bunker's seats, filled in at Sundown.
    /// Attach to the "TopBar" GameObject.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Old text elements (hidden; the bar is built in code)")]
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private TextMeshProUGUI bunkerText;

        private const float PipSize = 22f, PipGap = 10f;

        private static readonly Color BarColor = new Color32(0x12, 0x15, 0x22, 215);
        private static readonly Color Muted = new Color32(0xA9, 0xAE, 0xBD, 255);
        private static readonly Color PipAhead = new Color32(0x3A, 0x40, 0x55, 255);
        private static readonly Color SundownRed = new Color32(0xD9, 0x3B, 0x3B, 255);
        private static readonly Color MoonColor = new Color32(0xE8, 0xE4, 0xDA, 255);
        private static readonly Color SeatEmpty = new Color32(0x2A, 0x31, 0x45, 255);
        private static readonly Color SeatTaken = new Color32(0x5F, 0xB8, 0x7A, 255);

        private TextMeshProUGUI dayNumber, countdown, phaseName, turnText, bunkerLabel;
        private RectTransform pips, seats;
        private Image sunIcon, moonIcon, moonBite;
        private readonly List<Image> pipImages = new List<Image>();
        private readonly List<Image> seatImages = new List<Image>();
        private string shown = "";

        private void Start()
        {
            foreach (TextMeshProUGUI old in new[] { dayText, phaseText, bunkerText })
                if (old != null) old.gameObject.SetActive(false);
            Image oldBackground = GetComponent<Image>();
            if (oldBackground != null) oldBackground.enabled = false;

            Build();
        }

        // ---------- Building ----------

        private void Build()
        {
            var bar = NewRect("CountdownBar", transform, Vector2.zero, Vector2.one, new Vector2(24f, 8f), new Vector2(-24f, -10f));
            Rounded(bar.gameObject.AddComponent<Image>(), BarColor, 20).raycastTarget = false;

            // Left: DAY 6 /10, then the pips and how long is left
            dayNumber = SundownFonts.UseTitle(NewText("Day", bar, 52, FontStyles.Normal, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(28f, 0f), new Vector2(210f, 0f)));
            dayNumber.color = TextColor;

            pips = NewRect("Pips", bar, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(210f, 2f), new Vector2(560f, 26f));
            countdown = NewText("Countdown", bar, 17, FontStyles.Bold, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(210f, -30f), new Vector2(600f, -4f));
            countdown.characterSpacing = 6f;

            // Middle: sun or moon, DAY or NIGHT, and whose turn it is
            var middle = NewRect("Phase", bar, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-300f, 0f), new Vector2(300f, 0f));
            sunIcon = Disc("Sun", middle, new Vector2(-250f, 0f), 40f, SunColor);
            moonIcon = Disc("Moon", middle, new Vector2(-250f, 0f), 38f, MoonColor);
            moonBite = Disc("MoonShadow", middle, new Vector2(-240f, 6f), 34f, BarColor); // turns the disc into a crescent
            phaseName = SundownFonts.UseTitle(NewText("PhaseName", middle, 40, FontStyles.Normal, TextAlignmentOptions.MidlineLeft,
                Vector2.zero, new Vector2(0f, 1f), new Vector2(104f, 0f), new Vector2(260f, 0f)));
            phaseName.color = TextColor;
            turnText = NewText("Turn", middle, 26, FontStyles.Normal, TextAlignmentOptions.MidlineLeft,
                Vector2.zero, Vector2.one, new Vector2(262f, 0f), Vector2.zero);
            turnText.enableAutoSizing = true;
            turnText.fontSizeMin = 16f;
            turnText.fontSizeMax = 26f;
            turnText.color = TextColor;

            // Right: the bunker's seats
            var right = NewRect("Bunker", bar, new Vector2(1f, 0f), Vector2.one, new Vector2(-340f, 0f), new Vector2(-28f, 0f));
            bunkerLabel = NewText("Label", right, 17, FontStyles.Bold, TextAlignmentOptions.MidlineRight,
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-4f, 0f));
            bunkerLabel.characterSpacing = 6f;
            bunkerLabel.color = Muted;
            seats = NewRect("Seats", right, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, -20f), new Vector2(120f, 20f));
        }

        private static Image Disc(string name, Transform parent, Vector2 position, float size, Color color)
        {
            var rect = NewRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = position + new Vector2(size / 2f, 0f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = CircleSprite();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        // ---------- Live state ----------

        private void Update()
        {
            IGameView view = GameView.Current;
            if (view == null || dayNumber == null) return;

            string summary = Summary(view);
            if (summary == shown) return; // nothing changed since last frame
            shown = summary;

            int day = view.CurrentDay, sundown = view.SundownDay;
            bool isSundown = view.CurrentState == GameState.Sundown;

            dayNumber.text = isSundown ? "SUNDOWN"
                : $"<size=40%><color=#E07A2E>DAY </color></size>{day}<size=50%><color=#A9AEBD> /{sundown}</color></size>";

            int left = sundown - day;
            countdown.text = isSundown ? "" : left <= 0 ? "<color=#D93B3B>SUNDOWN TONIGHT</color>"
                : left == 1 ? "<color=#E07A2E>SUNDOWN TOMORROW</color>"
                : $"<color=#A9AEBD>SUNDOWN IN {left} DAYS</color>";
            LayOutPips(day, sundown, isSundown);

            bool night = view.EyesClosed;
            sunIcon.enabled = !night && !isSundown;
            moonIcon.enabled = moonBite.enabled = night;
            phaseName.text = isSundown ? "" : view.CurrentState == GameState.NightPhase ? "NIGHT"
                : view.CurrentDayRule == DayRule.Blackout ? "BLACKOUT" : "DAY";

            var active = view.ActivePlayer;
            turnText.text = isSundown ? "<color=#A9AEBD>The doors are closing...</color>"
                : active == null ? ""
                : view.IsLocalTurn ? $"<color=#E07A2E><b>Your turn</b></color>  <color=#A9AEBD>{ActionsLeft(view.ActionsLeft)}</color>"
                : $"<color=#A9AEBD>{active.playerName} is playing</color>";

            LayOutSeats(view.BunkerCapacity, isSundown ? view.BunkerSeated : 0, isSundown);
        }

        private static string ActionsLeft(int n) => n == 1 ? "1 action left" : $"{n} actions left";

        // Everything the bar shows, in one string, so it only redraws when something changes
        private static string Summary(IGameView v) =>
            $"{v.CurrentDay}|{v.SundownDay}|{v.CurrentState}|{v.EyesClosed}|{v.CurrentDayRule}|" +
                      $"{v.ActivePlayer?.playerName}|{v.IsLocalTurn}|{v.ActionsLeft}|{v.BunkerCapacity}|{v.BunkerSeated}";

        /// <summary>One pip per day: past days small and dim, today a bright sun, Sundown red.</summary>
        private void LayOutPips(int day, int sundown, bool isSundown)
        {
            while (pipImages.Count < sundown) pipImages.Add(Disc("Pip", pips, Vector2.zero, PipSize, PipAhead));
            for (int i = 0; i < pipImages.Count; i++)
            {
                Image pip = pipImages[i];
                int d = i + 1;
                pip.gameObject.SetActive(d <= sundown);
                if (d > sundown) continue;

                bool today = d == day && !isSundown;
                float size = today ? PipSize + 10f : d < day ? PipSize - 8f : PipSize - 4f;
                pip.rectTransform.anchorMin = pip.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                pip.rectTransform.sizeDelta = new Vector2(size, size);
                pip.rectTransform.anchoredPosition = new Vector2(i * (PipSize + PipGap) + PipSize / 2f, 0f);
                pip.color = today ? SunColor
                    : d == sundown ? SundownRed
                    : d < day || isSundown ? new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.55f)
                    : PipAhead;
            }
        }

        /// <summary>One seat per place in the bunker; at Sundown the taken ones turn green.</summary>
        private void LayOutSeats(int capacity, int taken, bool isSundown)
        {
            while (seatImages.Count < capacity)
            {
                var seat = NewRect("Seat", seats, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
                seatImages.Add(Rounded(seat.gameObject.AddComponent<Image>(), SeatEmpty, 8));
            }
            for (int i = 0; i < seatImages.Count; i++)
            {
                Image seat = seatImages[i];
                seat.gameObject.SetActive(i < capacity);
                seat.raycastTarget = false;
                seat.rectTransform.sizeDelta = new Vector2(28f, 38f);
                seat.rectTransform.anchoredPosition = new Vector2(14f + i * 38f, 0f);
                seat.color = i < taken ? SeatTaken : SeatEmpty;
            }

            string seatsWord = capacity == 1 ? "1 SEAT" : $"{capacity} SEATS";
            bunkerLabel.text = isSundown ? $"BUNKER  {taken}/{capacity} SEATED" : $"BUNKER  {seatsWord}";
            // The seats sit just left of the label
            bunkerLabel.ForceMeshUpdate();
            float labelWidth = bunkerLabel.preferredWidth;
            float seatsWidth = capacity * 38f;
            seats.anchorMin = seats.anchorMax = new Vector2(1f, 0.5f);
            seats.sizeDelta = new Vector2(seatsWidth, 40f);
            seats.anchoredPosition = new Vector2(-labelWidth - 24f - seatsWidth / 2f, 0f);
        }
    }
}

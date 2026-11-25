using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Data;
using TDTS.Managers;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// Wireframes 03 and 04: the game table as a room in the bunker, drawn in code.
    /// - 03-1 Back wall of wood planks, a shelf of supplies, a hanging lamp that sways, and a window
    ///   whose sky darkens a step each Day (orange afternoon on Day 1, almost black at Sundown).
    /// - 03-2 Opponents sit behind the far edge of a tilted table (03-13), their name plates and
    ///   face-up cards lying on the table in front of them.
    /// - 04-1 At Night the lamp goes out, moonlight comes through the window and the room turns teal.
    /// - 05-2 While someone else takes a Day turn, a soft light falls on their seat.
    /// It also lays out the scene's table pieces (zones, draw pile, Rondo, log, hand, action bar) to
    /// match board 03. BoardController adds it first thing in Awake, before anything else lays out.
    /// </summary>
    public class TableRoom : MonoBehaviour
    {
        public static TableRoom Current { get; private set; }

        // Reference layout (1920 x 1080), y measured from the top like the wireframe
        /// <summary>The v3 painted room (Resources/RoomV3) is in the project; otherwise the code-drawn room is used.</summary>
        public static bool ArtRoom => ArtKit.Has("RoomV3/room_day1");

        /// <summary>Where the table's far edge is: lower in the v3 room (smaller table, bigger cast).</summary>
        public static float TableEdgeY => ArtRoom ? 650f : 556f;
        private static readonly float[] SeatXCode = { 0f, 470f, 960f, 1440f }; // by seats after you (1 = left, 2 = across, 3 = right)
        private static readonly float[] SeatXArt = { 0f, 520f, 960f, 1400f };
        private static float[] SeatX => ArtRoom ? SeatXArt : SeatXCode;
        private static float PuppetHeight => ArtRoom ? 400f : 300f;

        // v3 room pieces (all in the 1920 x 1080 frame the art was painted in)
        private Image baseNow, baseNext, artTable, artTableNight, artLamp, artLampNight;
        private RectTransform fan, spider;
        private TMPro.TextMeshProUGUI calendarDays, clockDays;
        private string baseShown = "";
        private float baseFade = 1f;

        private IGameView view;
        private RectTransform room, seats, lamp, lampLight, lightLayer, spotlight;
        private RawImage windowSky;
        private RectTransform windowSun;
        private Image windowMoon, nightTint, dimmer;
        private int skyDay = -1;
        private bool skyNight;
        private float night;

        private static readonly Color Wall = new Color32(0x3A, 0x2A, 0x22, 255);
        private static readonly Color Plank = new Color32(0x30, 0x22, 0x1B, 255);
        private static readonly Color Wood = new Color32(0x24, 0x18, 0x12, 255);
        private static readonly Color TableTop = new Color32(0x4A, 0x34, 0x27, 255);
        private static readonly Color TableLip = new Color32(0x5E, 0x43, 0x33, 255);
        private static readonly Color LampLight = new Color32(0xFF, 0xD5, 0x8A, 255);

        private void Awake()
        {
            Current = this;
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) { enabled = false; return; }
            Transform root = canvas.rootCanvas.transform;
            BuildRoom(root);
            LayOutTable(root);
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void Start() => view = GameView.Current;

        // ---------- The room ----------

        private void BuildRoom(Transform canvas)
        {
            room = NewRect("TableRoom", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            room.SetAsFirstSibling();
            if (ArtRoom)
            {
                BuildArtRoom();
                return;
            }
            Fill(room, "Wall", Wall);
            for (int i = 0; i < 17; i++) // vertical planks
            {
                var plank = NewRect("Plank", room, new Vector2(i / 16f, 0f), new Vector2(i / 16f, 1f), new Vector2(-2f, 0f), new Vector2(2f, 0f));
                plank.gameObject.AddComponent<Image>().color = Plank;
                plank.GetComponent<Image>().raycastTarget = false;
            }
            var shade = NewRect("CeilingShadow", room, new Vector2(0f, 0.8f), Vector2.one, Vector2.zero, Vector2.zero);
            var shadeImage = shade.gameObject.AddComponent<RawImage>();
            shadeImage.texture = SkyTexture(new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 0.25f), new Color(0f, 0f, 0f, 0.55f));
            shadeImage.raycastTarget = false;

            BuildWindow();
            if (!RoomDetails.TryBuild(room)) BuildShelf(); // the team-style props; the old shape shelf if they're missing

            // Opponents sit here, behind the table
            // The lamp's pool of light sits behind the seats, so it lights the wall, not the faces
            lightLayer = NewRect("LampLight", room, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            seats = NewRect("Seats", room, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            spotlight = Glow(room, "Spotlight", new Vector2(960f, 420f), new Vector2(420f, 520f), new Color(1f, 0.85f, 0.55f, 0f));

            // The tilted table (a perspective trapezoid) with a lighter far edge
            var table = NewRect("Table", room, Vector2.zero, new Vector2(1f, 1f - TableEdgeY / 1080f), new Vector2(-160f, -40f), new Vector2(160f, 0f));
            // Drawn as a generated trapezoid image (a custom mesh didn't render on every setup).
            // The table is 2240 px wide; the far edge is pulled in 560 px each side.
            var top = table.gameObject.AddComponent<RawImage>();
            top.texture = TrapezoidTexture(560f / 2240f, TableLip, TableTop);
            top.raycastTarget = false;

            BuildLamp();

            dimmer = Fill(room, "SpotlightDim", new Color(0f, 0f, 0f, 0f));
            nightTint = Fill(room, "NightTint", new Color32(0x0A, 0x22, 0x2B, 0));
        }

        /// <summary>
        /// The v3 room: one painted backdrop per stage (Day 1-3 full shelf, Day 4-7 half, Day 8+ almost empty,
        /// Night, and one per Rondo result) that cross-fade, with the moving bits as their own sprites:
        /// the table, the lamp (swings), the fan blades (spin) and the spider (drops on its thread).
        /// The calendar and the red clock under the window show the real days left.
        /// </summary>
        private void BuildArtRoom()
        {
            baseNow = ArtKit.Full(room, "Backdrop", ArtKit.Get("RoomV3/room_day1"));
            baseNext = ArtKit.Full(room, "BackdropNext", null, new Color(1f, 1f, 1f, 0f));
            baseShown = "day1";

            calendarDays = SundownFonts.UseTitle(NewText("CalendarDays", room, 66, TMPro.FontStyles.Normal, TMPro.TextAlignmentOptions.Center,
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero));
            Place(calendarDays.rectTransform, 728f, 300f, 120f, 84f);
            calendarDays.color = new Color32(0x5A, 0x24, 0x20, 255);
            clockDays = SundownFonts.UseTitle(NewText("ClockDays", room, 34, TMPro.FontStyles.Normal, TMPro.TextAlignmentOptions.Center,
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero));
            Place(clockDays.rectTransform, 1232f, 408f, 60f, 40f);
            clockDays.color = new Color32(0xFF, 0x4A, 0x3A, 255);

            fan = ArtKit.Box(room, "FanBlades", ArtKit.Get("RoomV3/room_fan"), RoomArtLayout.Fan, new Vector2(1800f, 454f)).rectTransform;
            spider = ArtKit.Box(room, "Spider", ArtKit.Get("RoomV3/room_spider"), RoomArtLayout.Spider).rectTransform;

            // Opponents sit behind the table; the lamp's pool lights the wall behind them
            lightLayer = NewRect("LampLight", room, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            seats = NewRect("Seats", room, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            spotlight = Glow(room, "Spotlight", new Vector2(960f, 460f), new Vector2(460f, 560f), new Color(1f, 0.85f, 0.55f, 0f));

            artTable = ArtKit.Box(room, "Table", ArtKit.Get("RoomV3/room_table"), RoomArtLayout.Table);
            artTableNight = ArtKit.Box(room, "TableNight", ArtKit.Get("RoomV3/room_table_night"), RoomArtLayout.TableNight);
            artTableNight.color = new Color(1f, 1f, 1f, 0f);

            var hook = new Vector2(960f, 96f);
            artLamp = ArtKit.Box(room, "Lamp", ArtKit.Get("RoomV3/room_lamp"), RoomArtLayout.Lamp, hook);
            artLampNight = ArtKit.Box(room, "LampNight", ArtKit.Get("RoomV3/room_lamp_night"), RoomArtLayout.LampNight, hook);
            artLampNight.color = new Color(1f, 1f, 1f, 0f);
            lamp = artLamp.rectTransform;
            lampLight = NewRect("Light", lightLayer, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            lampLight.pivot = new Vector2(0.5f, 1f);
            lampLight.sizeDelta = new Vector2(900f, 620f);
            lampLight.anchoredPosition = new Vector2(0f, -250f);
            var light = lampLight.gameObject.AddComponent<RawImage>();
            light.texture = PoolTexture();
            light.color = UITheme.WithAlpha(LampLight, 0.12f);
            light.raycastTarget = false;

            ScreenOverlays.Create(room, true); // OV10 warm, OV7 paper, OV3 dusk, OV8 night (under the HUD)
            dimmer = Fill(room, "SpotlightDim", new Color(0f, 0f, 0f, 0f));
            nightTint = Fill(room, "NightTint", new Color32(0x0A, 0x22, 0x2B, 0));
        }

        /// <summary>Which painted backdrop fits now: Night, today's Rondo result, or the shelf stage for the days left.</summary>
        private static string BackdropFor(IGameView v)
        {
            if (v.EyesClosed) return "night";
            if (v.TodaysRondo.HasValue)
            {
                switch (v.TodaysRondo.Value)
                {
                    case RondoResult.Purge: return "purge";
                    case RondoResult.TradingOnly: return "trading_only";
                    case RondoResult.HungerDisaster: return "hunger";
                    case RondoResult.Tornado: return "tornado";
                    case RondoResult.DoomsdayClock: return "doomsday";
                    case RondoResult.Blackout: return "blackout";
                    case RondoResult.Outbreak: return "outbreak";
                    case RondoResult.PeacefulDay: return "peaceful";
                }
            }
            int left = v.SundownDay - v.CurrentDay + 1;
            return left > 7 ? "day1" : left > 3 ? "day5" : "day9";
        }

        private void UpdateArtRoom(bool still)
        {
            // Cross-fade to a new backdrop
            string want = BackdropFor(view);
            if (want != baseShown && baseFade >= 1f)
            {
                Sprite next = ArtKit.Get("RoomV3/room_" + want);
                if (next != null)
                {
                    baseNext.sprite = next;
                    baseFade = 0f;
                    baseShown = want;
                }
            }
            if (baseFade < 1f)
            {
                baseFade = still ? 1f : Mathf.MoveTowards(baseFade, 1f, Time.deltaTime / 1.2f);
                baseNext.color = new Color(1f, 1f, 1f, baseFade);
                if (baseFade >= 1f)
                {
                    baseNow.sprite = baseNext.sprite;
                    baseNext.color = new Color(1f, 1f, 1f, 0f);
                }
            }

            int left = Mathf.Max(0, view.SundownDay - view.CurrentDay + 1);
            calendarDays.text = left.ToString();
            clockDays.text = left.ToString("00");

            artLamp.color = new Color(1f, 1f, 1f, 1f - night);
            artLampNight.color = new Color(1f, 1f, 1f, night);
            artLampNight.rectTransform.localEulerAngles = lamp.localEulerAngles;
            artTable.color = new Color(1f, 1f, 1f, 1f - night);
            artTableNight.color = new Color(1f, 1f, 1f, night);
            if (!still) fan.localEulerAngles = new Vector3(0f, 0f, fan.localEulerAngles.z - 420f * Time.deltaTime);
            spider.anchoredPosition = new Vector2(0f, still ? 0f : -Mathf.Abs(Mathf.Sin(Time.time * 0.35f)) * 22f);
        }

        private void BuildWindow()
        {
            var frame = Place(NewRect("Window", room, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), 1420f, 290f, 390f, 260f);
            frame.gameObject.AddComponent<Image>().color = Wood;
            var glass = NewRect("Glass", frame, Vector2.zero, Vector2.one, new Vector2(14f, 14f), new Vector2(-14f, -14f));
            glass.gameObject.AddComponent<RectMask2D>();
            windowSky = glass.gameObject.AddComponent<RawImage>();
            windowSky.raycastTarget = false;
            windowSun = MenuBackground.Shape(glass, new Vector2(0.55f, 0.4f), new Vector2(90f, 90f), UITheme.Sun, circle: true);
            windowMoon = MenuBackground.Shape(glass, new Vector2(0.3f, 0.72f), new Vector2(50f, 50f), UITheme.Cream, circle: true).GetComponent<Image>();
            var hills = NewRect("Hills", glass, Vector2.zero, new Vector2(1f, 0.22f), Vector2.zero, Vector2.zero);
            hills.gameObject.AddComponent<Image>().color = new Color32(0x12, 0x0C, 0x0E, 255);
            MenuBackground.Shape(glass, new Vector2(0.25f, 0.22f), new Vector2(200f, 40f), new Color32(0x12, 0x0C, 0x0E, 255), rotation: 6f, cornerScale: 20f);
            MenuBackground.Shape(glass, new Vector2(0.78f, 0.22f), new Vector2(220f, 46f), new Color32(0x12, 0x0C, 0x0E, 255), rotation: -5f, cornerScale: 20f);
            var bar = NewRect("Bar", frame, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-6f, 0f), new Vector2(6f, 0f));
            bar.gameObject.AddComponent<Image>().color = Wood;
        }

        private void BuildShelf()
        {
            Color[] boxes = { UITheme.Orange, UITheme.Teal, UITheme.Brick, UITheme.Gold, new Color32(0x6B, 0x5A, 0x4A, 255) };
            for (int shelf = 0; shelf < 2; shelf++)
            {
                float y = 250f + shelf * 120f;
                Place(NewRect("Shelf", room, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), 330f, y, 300f, 14f)
                    .gameObject.AddComponent<Image>().color = Wood;
                for (int i = 0; i < 4; i++)
                {
                    float h = 50f + (i * 17 + shelf * 23) % 40;
                    var box = Place(NewRect("Supply", room, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), 215f + i * 72f, y - 7f - h / 2f, 54f, h);
                    Rounded(box.gameObject.AddComponent<Image>(), Color.Lerp(boxes[(i + shelf * 2) % boxes.Length], Wall, 0.45f), 4).raycastTarget = false;
                }
            }
        }

        private void BuildLamp()
        {
            lamp = NewRect("Lamp", room, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            lamp.pivot = new Vector2(0.5f, 1f);
            lamp.sizeDelta = new Vector2(120f, 200f);
            lamp.anchoredPosition = new Vector2(0f, -100f);
            var cord = NewRect("Cord", lamp, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-2f, -110f), new Vector2(2f, 0f));
            cord.gameObject.AddComponent<Image>().color = Wood;
            lampLight = NewRect("Light", lightLayer, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            lampLight.pivot = new Vector2(0.5f, 1f);
            lampLight.sizeDelta = new Vector2(900f, 620f);
            lampLight.anchoredPosition = new Vector2(0f, -170f);
            var light = lampLight.gameObject.AddComponent<RawImage>();
            light.texture = PoolTexture();
            light.color = UITheme.WithAlpha(LampLight, 0.16f);
            light.raycastTarget = false;
            var shadeRect = NewRect("Shade", lamp, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-55f, -150f), new Vector2(55f, -106f));
            Rounded(shadeRect.gameObject.AddComponent<Image>(), new Color32(0x2C, 0x37, 0x3C, 255), 30).raycastTarget = false;
            MenuBackground.Shape(lamp, new Vector2(0.5f, 0.24f), new Vector2(28f, 28f), LampLight, circle: true);
        }

        // ---------- Seats ----------

        /// <summary>Seats a puppet for the opponent this many seats after you, behind the table edge.</summary>
        public Puppet SeatPuppet(int seatsAfterYou, int seatNumber)
        {
            float x = SeatX[Mathf.Clamp(seatsAfterYou, 1, 3)] / 1920f;
            // The cast art faces left; whoever sits on the left of the table turns to look in toward the middle
            float feet = TableEdgeY + (ArtRoom ? 70f : 60f);
            Puppet puppet = Puppet.Create(seats, new Vector2(x, 1f - feet / 1080f), seatNumber, PuppetHeight, seatNumber % 2 == 0, seatsAfterYou == 1);
            if (ArtRoom && seatsAfterYou != 2) puppet.Turn(1f); // side seats are turned a little toward the middle
            return puppet;
        }

        // ---------- Live state ----------

        private void Update()
        {
            if (view == null) { view = GameView.Current; if (view == null) return; }
            bool still = GameSettings.ReduceMotion;

            // The lamp sways gently; at Night it's off
            night = Mathf.MoveTowards(night, view.EyesClosed ? 1f : 0f, Time.deltaTime / 1.2f);
            lamp.localEulerAngles = new Vector3(0f, 0f, still ? 0f : Mathf.Sin(Time.time * 0.8f) * 1.6f);
            lampLight.GetComponent<RawImage>().color = UITheme.WithAlpha(LampLight, (ArtRoom ? 0.12f : 0.16f) * (1f - night));
            // The painted Night backdrop is already dark, so the v3 room needs only a touch of tint
            nightTint.color = new Color(nightTint.color.r, nightTint.color.g, nightTint.color.b, (ArtRoom ? 0.12f : 0.5f) * night);
            if (ArtRoom) UpdateArtRoom(still);

            int day = view.CurrentDay;
            bool isNight = view.EyesClosed;
            if (windowSky != null && (day != skyDay || isNight != skyNight)) PaintWindow(day, view.SundownDay, isNight);

            // 05-2: someone else's Day turn puts a soft light on their seat
            PlayerData active = view.ActivePlayer;
            int seat = SeatOf(active);
            bool spot = seat > 0 && !view.IsLocalTurn && view.CurrentState == GameState.DayPhase && !isNight;
            if (spot) spotlight.anchorMin = spotlight.anchorMax = new Vector2(SeatX[seat] / 1920f, 1f - (ArtRoom ? 470f : 380f) / 1080f);
            var spotImage = spotlight.GetComponent<RawImage>();
            spotImage.color = new Color(1f, 0.85f, 0.55f, Mathf.MoveTowards(spotImage.color.a, spot ? 0.28f : 0f, Time.deltaTime * 2f));
            dimmer.color = new Color(0f, 0f, 0f, Mathf.MoveTowards(dimmer.color.a, spot ? 0.25f : 0f, Time.deltaTime * 2f));
        }

        private int SeatOf(PlayerData player)
        {
            if (player == null || view == null) return 0;
            for (int i = 1; i <= 3; i++) if (view.GetPlayerAfterLocal(i) == player) return i;
            return 0;
        }

        /// <summary>The window sky steps from orange afternoon (Day 1) to almost black (Sundown).</summary>
        private void PaintWindow(int day, int sundown, bool isNight)
        {
            skyDay = day;
            skyNight = isNight;
            float t = Mathf.Clamp01((day - 1f) / Mathf.Max(1f, sundown - 1f));
            Color bottom = isNight ? new Color32(0x10, 0x14, 0x28, 255) : Color.Lerp(new Color32(0xF2, 0x9A, 0x4A, 255), new Color32(0x3A, 0x14, 0x14, 255), t);
            Color middle = isNight ? new Color32(0x08, 0x0A, 0x16, 255) : Color.Lerp(new Color32(0xC8, 0x5A, 0x3A, 255), new Color32(0x16, 0x10, 0x1E, 255), t);
            Color top = isNight ? new Color32(0x03, 0x04, 0x09, 255) : Color.Lerp(new Color32(0x6B, 0x33, 0x4E, 255), new Color32(0x06, 0x07, 0x0E, 255), t);
            windowSky.texture = SkyTexture(bottom, middle, top);
            windowSun.gameObject.SetActive(!isNight);
            windowSun.anchorMin = windowSun.anchorMax = new Vector2(0.55f, Mathf.Lerp(0.55f, 0.16f, t));
            windowMoon.gameObject.SetActive(isNight);
        }

        // ---------- Laying out the scene's table pieces (board 03) ----------

        private static void LayOutTable(Transform canvas)
        {
            Transform tableArea = canvas.Find("TableArea");
            if (tableArea != null)
            {
                Stretch((RectTransform)tableArea);
                DisableLayout(tableArea);
                PlaceZone(tableArea.Find("PlayerLeft"), SeatX[1], 400f);
                PlaceZone(tableArea.Find("PlayerTop"), SeatX[2], 440f);
                PlaceZone(tableArea.Find("PlayerRight"), SeatX[3], 400f);

                Transform cluster = tableArea.Find("CenterCluster");
                if (cluster != null)
                {
                    Stretch((RectTransform)cluster);
                    DisableLayout(cluster);
                    // Low enough to clear the name plates above them
                    // Pile left of centre, Rondo right, the log off to the left: the middle stays clear for your hand
                    Keep(cluster.Find("DrawPile"), 720f, 790f);
                    Keep(cluster.Find("RondoWheel"), 1220f, 795f);
                    Keep(cluster.Find("PlaySpace"), 440f, 800f);
                }
            }

            // 03-9: your hand fans along the bottom
            Transform handArea = canvas.Find("HandArea");
            if (handArea != null)
            {
                var rect = (RectTransform)handArea;
                rect.anchorMin = new Vector2(300f / 1920f, 0f);
                rect.anchorMax = new Vector2(1580f / 1920f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.offsetMin = new Vector2(0f, 0f);
                rect.offsetMax = new Vector2(0f, 200f);
                DisableLayout(handArea);
                foreach (string hidden in new[] { "HandLabel", "Divider" })
                {
                    Transform t = handArea.Find(hidden);
                    if (t != null) t.gameObject.SetActive(false);
                }
                Transform row = handArea.Find("HandRow");
                if (row != null) Stretch((RectTransform)row);
            }

            // 03-12: the action buttons stand in a column on the right. Their rows live inside HandArea,
            // so they move up to the canvas; otherwise they'd drag the hand around with them.
            Transform trade = FindDeep(canvas, "TradeButton"), steal = FindDeep(canvas, "StealButton");
            foreach (Transform buttonRow in new[] { trade?.parent, steal?.parent })
            {
                if (buttonRow == null) continue;
                buttonRow.SetParent(canvas, false);
                var rowRect = (RectTransform)buttonRow;
                rowRect.anchorMin = rowRect.anchorMax = new Vector2(1f, 0f);
                rowRect.pivot = new Vector2(1f, 0f);
                rowRect.sizeDelta = new Vector2(290f, 400f);
                rowRect.anchoredPosition = new Vector2(-34f, 34f);
                var horizontal = buttonRow.GetComponent<HorizontalLayoutGroup>();
                if (horizontal != null) DestroyImmediate(horizontal);
                var column = buttonRow.GetComponent<VerticalLayoutGroup>() ?? buttonRow.gameObject.AddComponent<VerticalLayoutGroup>();
                column.spacing = 14f;
                column.childAlignment = TextAnchor.LowerCenter;
                column.childControlWidth = column.childControlHeight = true;
                column.childForceExpandWidth = true;
                column.childForceExpandHeight = false;
                foreach (Transform button in buttonRow)
                {
                    var size = button.GetComponent<LayoutElement>() ?? button.gameObject.AddComponent<LayoutElement>();
                    size.preferredHeight = 84f;
                    size.flexibleWidth = 1f;
                }
            }
        }

        private static void PlaceZone(Transform zone, float x, float width)
        {
            if (zone == null) return;
            Place((RectTransform)zone, x, TableEdgeY + 64f, width, 112f);
        }

        /// <summary>Moves a piece to a spot (centre, in reference pixels from the top-left), keeping its size.</summary>
        private static void Keep(Transform piece, float x, float y)
        {
            if (piece == null) return;
            var rect = (RectTransform)piece;
            Vector2 size = rect.rect.size;
            Place(rect, x, y, size.x, size.y);
        }

        public static RectTransform Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(x / 1920f, 1f - y / 1080f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = Vector2.zero;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void DisableLayout(Transform t)
        {
            var group = t.GetComponent<LayoutGroup>();
            if (group != null) group.enabled = false;
            var fitter = t.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.enabled = false;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name) return child;
                Transform found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>
        /// A trapezoid filling the texture: full width at the bottom, pulled in by farInset (a fraction
        /// of the width) on each side at the top, with a lighter lip along the far edge.
        /// </summary>
        private static Texture2D TrapezoidTexture(float farInset, Color lip, Color surface)
        {
            const int w = 1024, h = 256, lipRows = 5;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = (y + 0.5f) / h; // 0 = near edge (bottom), 1 = far edge (top)
                float inset = farInset * w * t;
                Color c = y >= h - lipRows ? lip : Color.Lerp(surface, Color.Lerp(surface, Color.black, 0.25f), 1f - t);
                for (int x = 0; x < w; x++)
                {
                    float edge = Mathf.Min(x + 0.5f - inset, w - inset - (x + 0.5f));
                    pixels[y * w + x] = new Color(c.r, c.g, c.b, Mathf.Clamp01(edge + 0.5f));
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Image Fill(Transform parent, string name, Color color)
        {
            var image = NewRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform Glow(Transform parent, string name, Vector2 at, Vector2 size, Color color)
        {
            var rect = Place(NewRect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero), at.x, at.y, size.x, size.y);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.texture = PoolTexture();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }
    }
}

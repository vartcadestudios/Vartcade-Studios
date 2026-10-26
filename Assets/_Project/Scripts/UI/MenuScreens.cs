using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Data;
using TDTS.Managers;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// Pop-up screens shared by the Main menu and the in-game menu: How to play, Settings and Profile.
    /// Each one dims what's behind it and closes itself with its own button.
    /// </summary>
    public static class MenuScreens
    {
        private static readonly Color FieldColor = new Color32(0x0C, 0x0F, 0x1A, 255);
        private static readonly Color MutedText = new Color32(0xA9, 0xAE, 0xBD, 255);

        // ---------- How to play ----------

        // Written from the rules as the game runs them (GameManager), not the printed rulebook
        private static readonly (string tab, string title, string body)[] Pages =
        {
            ("Goal", "Make it to the bunker",
                "The sun sets in <b>ten days</b>. When it does, the bunker has only <b>two seats</b>.\n\n" +
                "Whoever holds the <color=#FAC775><b>Bunker Key</b></color> gets a seat, no matter what. " +
                "It hides on the <b>back</b> of an ordinary Supply card, so you may not know who has it... even if it's you.\n\n" +
                "The other seats go to players holding at least <b>3 real Supply</b> cards, most Supply first. " +
                "Everyone else is left outside when the light goes."),

            ("Your cards", "Every card has two faces",
                "You can always see the <b>front</b> of your own cards: Food, Water, Medicine, Entertainment, or an action.\n\n" +
                "The <b>back</b> is what really counts, and you can't see it until you <b>peek</b>. " +
                "A Food front might hide <b>real Supply</b>, worthless <b>Trash</b>... or the <b>Bunker Key</b>.\n\n" +
                "Peeked cards get a <color=#FFD54A><b>gold dot</b></color>. At Night, tap one to see its back again; by day it stays hidden. " +
                "Everyone can always see the <b>fronts</b> of everyone's cards; only backs are secret. " +
                "There's <b>no limit</b> to how many cards you can hold."),

            ("Day", "Day: eyes open, one action",
                "Players take turns in order. On your turn, do <b>one</b> of these:\n\n" +
                "<b>Trade</b>  Pick one of your cards, tap a player, then choose which of their cards you want. They accept or refuse.\n" +
                "<b>Scavenge</b>  Tap the draw pile to draw its top card.\n" +
                "<b>Play card</b>  Play a <b>Morning Action</b> card, like Swap Deck or Reverse Turn.\n\n" +
                "There's no passing: your turn ends once you've made your move."),

            ("Night", "Night: eyes closed, two actions",
                "Everyone closes their eyes. You get <b>two</b> actions. You <b>must peek</b> at least once, " +
                "and you can <b>Steal or Swap only once</b>. Each peek uses an action, so two peeks end your turn.\n\n" +
                "<b>Peek</b>  Tap one of your cards, then press Peek to learn its back.\n" +
                "<b>Steal</b>  Tap a player, then <b>choose</b> which of their cards to take.\n" +
                "<b>Swap</b>  Give one of your cards for a random card of theirs.\n" +
                "<b>Play card</b>  Play a <b>Night Action</b> you've peeked at, like the Flashlight.\n\n" +
                "Everyone else only hears that <i>someone did something in the dark</i>."),

            ("Rondo", "The Rondo",
                "From <b>Day 6</b>, the Rondo dial in the middle of the table spins every morning:\n\n" +
                "<b>Purge</b>  5 cards burn off the draw pile.\n" +
                "<b>Trading Only</b>  No Scavenge and no action cards today.\n" +
                "<b>Hunger Disaster</b>  Everyone loses a real Food.\n" +
                "<b>Outbreak</b>  Everyone loses a real Medicine or Entertainment.\n" +
                "<b>Tornado</b>  Every hand is shuffled together and dealt back out.\n" +
                "<b>Doomsday Clock</b>  Sundown comes a day sooner.\n" +
                "<b>Blackout</b>  Eyes stay closed all Day.\n" +
                "<b>Peaceful Day</b>  Nothing happens. Enjoy it."),

            ("Controls", "Playing on screen",
                "<b>Tap a player</b> to see their cards close up and target them for Trade, Steal, Swap or an action card. " +
                "Pick one of their cards and Trade, Steal or Swap right there.\n" +
                "<b>Tap one of your cards</b> to pick it. At Night, press <b>Peek</b> to look at its back. At Night, tapping a peeked card shows its back again.\n" +
                "<b>Tap the draw pile</b> to Scavenge.\n" +
                "The <b>log</b> in the middle tells you what just happened and how many actions you have left.\n\n" +
                "Press <b>Esc</b> any time for the menu, these rules and the settings.\n\n" +
                "<color=#E07A2E><b>Trust no one.</b></color>"),
        };

        public static GameObject HowToPlay(Transform canvas, Action onClose = null)
        {
            RectTransform overlay = Overlay("HowToPlay", canvas);
            var panel = NewPanel("Panel", overlay, new Vector2(-620f, -390f), new Vector2(620f, 390f));

            var heading = NewText("Heading", panel, 26, FontStyles.Bold, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), Vector2.one, new Vector2(56f, -76f), new Vector2(-56f, -36f));
            heading.text = "HOW TO PLAY";
            heading.characterSpacing = 18f;
            heading.color = AccentColor;

            var title = SundownFonts.UseTitle(NewText("Title", panel, 56, FontStyles.Normal, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), Vector2.one, new Vector2(56f, -230f), new Vector2(-56f, -150f)));
            title.color = TextColor;

            var body = NewText("Body", panel, 28, FontStyles.Normal, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, new Vector2(56f, 120f), new Vector2(-56f, -250f));
            body.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.9f);
            body.lineSpacing = 12f;
            body.enableAutoSizing = true;
            body.fontSizeMin = 18f;
            body.fontSizeMax = 28f;

            // Tabs across the top
            var tabs = NewRect("Tabs", panel, new Vector2(0f, 1f), Vector2.one, new Vector2(56f, -140f), new Vector2(-56f, -84f));
            var row = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 10f;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = true;

            var tabButtons = new List<Button>();
            int page = 0;
            void Show(int index)
            {
                page = Mathf.Clamp(index, 0, Pages.Length - 1);
                title.text = Pages[page].title;
                body.text = Pages[page].body;
                for (int i = 0; i < tabButtons.Count; i++)
                    tabButtons[i].colors = Tints(i == page ? AccentColor : ButtonColor);
            }
            for (int i = 0; i < Pages.Length; i++)
            {
                int index = i;
                tabButtons.Add(MakeButton(tabs, Pages[i].tab, ButtonColor, () => Show(index), 20));
            }

            var footer = NewRect("Footer", panel, Vector2.zero, new Vector2(1f, 0f), new Vector2(56f, 36f), new Vector2(-56f, 100f));
            var buttons = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            buttons.spacing = 14f;
            buttons.childControlWidth = buttons.childControlHeight = true;
            buttons.childForceExpandWidth = buttons.childForceExpandHeight = true;
            MakeButton(footer, "‹  Back", ButtonColor, () => Show(page - 1), 24);
            MakeButton(footer, "Next  ›", ButtonColor, () => Show(page + 1), 24);
            MakeButton(footer, "Got it", AccentColor, () => Close(overlay, onClose), 24);

            Show(0);
            return overlay.gameObject;
        }

        // ---------- Settings ----------

        public static GameObject Settings(Transform canvas, Action onClose = null)
        {
            RectTransform overlay = Overlay("Settings", canvas);
            var panel = NewPanel("Panel", overlay, new Vector2(-420f, -300f), new Vector2(420f, 300f));

            var title = SundownFonts.UseTitle(NewText("Title", panel, 56, FontStyles.Normal, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), Vector2.one, new Vector2(56f, -120f), new Vector2(-56f, -36f)));
            title.text = "Settings";
            title.color = TextColor;

            float y = -150f;
            RectTransform Row(string label, string hint)
            {
                var text = NewText(label, panel, 28, FontStyles.Bold, TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), new Vector2(0.45f, 1f), new Vector2(56f, y - 70f), new Vector2(0f, y));
                text.text = $"{label}\n<size=70%><color=#A9AEBD>{hint}</color></size>";
                text.fontStyle = FontStyles.Normal;
                var control = NewRect(label + "Control", panel, new Vector2(0.45f, 1f), new Vector2(1f, 1f),
                    new Vector2(12f, y - 58f), new Vector2(-56f, y - 10f));
                y -= 104f;
                return control;
            }

            Slider volume = MakeSlider(Row("Volume", "Sound and music"), GameSettings.Volume);
            volume.onValueChanged.AddListener(v => GameSettings.Volume = v);

            Segmented(Row("Fullscreen", "Fill the whole screen"), new[] { "On", "Off" },
                GameSettings.Fullscreen ? 0 : 1, i => GameSettings.Fullscreen = i == 0);

            Segmented(Row("Bot speed", "How long bots think"), new[] { "Slow", "Normal", "Fast" },
                (int)GameSettings.Bots, i => GameSettings.Bots = (GameSettings.BotSpeed)i);

            var done = NewRect("Done", panel, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-296f, 36f), new Vector2(-56f, 100f));
            MakeButton(done, "Done", AccentColor, () => Close(overlay, onClose), 24);

            return overlay.gameObject;
        }

        // ---------- Profile ----------

        private static readonly Color TileColor = new Color32(0x1A, 0x1E, 0x2E, 255);
        private static readonly Color WinColor = new Color32(0x5F, 0xB8, 0x7A, 255);
        private static readonly Color LossColor = new Color32(0xD9, 0x5B, 0x5B, 255);
        private static readonly Color NoGameColor = new Color32(0x2A, 0x31, 0x45, 255);

        // Avatar colours, picked from your name so it stays the same game to game
        private static readonly Color[] AvatarColors =
        {
            new Color32(0xE0, 0x7A, 0x2E, 255), new Color32(0x4F, 0x8F, 0xD9, 255), new Color32(0x5F, 0xB8, 0x7A, 255),
            new Color32(0xB5, 0x6C, 0xD6, 255), new Color32(0xD9, 0x5B, 0x6A, 255), new Color32(0xD6, 0xB8, 0x4A, 255),
        };

        /// <summary>The name you play under (the one you type in the Lobby).</summary>
        public static string ProfileName
        {
            get
            {
                string name = Networking.SessionManager.SavedPlayerName;
                return string.IsNullOrWhiteSpace(name) ? "You" : name.Trim();
            }
        }

        public static Color AvatarColor(string name)
        {
            int hash = 0;
            foreach (char c in name) hash = hash * 31 + c;
            return AvatarColors[Mathf.Abs(hash) % AvatarColors.Length];
        }

        /// <summary>A round badge with your initial.</summary>
        public static RectTransform Avatar(Transform parent, string name, float size)
        {
            var avatar = NewRect("Avatar", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            avatar.sizeDelta = new Vector2(size, size);
            Image disc = avatar.gameObject.AddComponent<Image>();
            disc.sprite = CircleSprite();
            disc.color = AvatarColor(name);
            disc.raycastTarget = false;
            var initial = SundownFonts.UseTitle(NewText("Initial", avatar, size * 0.5f, FontStyles.Normal,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero));
            initial.text = name.Substring(0, 1).ToUpperInvariant();
            initial.color = PanelColor;
            return avatar;
        }

        /// <summary>Your record on this device: games, wins, streaks and your last ten results.</summary>
        public static GameObject Profile(Transform canvas, Action onClose = null)
        {
            PlayerProfile profile = PlayerProfile.Current;
            string name = ProfileName;

            RectTransform overlay = Overlay("Profile", canvas);
            var panel = NewPanel("Panel", overlay, new Vector2(-560f, -380f), new Vector2(560f, 380f));

            // Who you are
            RectTransform avatar = Avatar(panel, name, 120f);
            avatar.anchorMin = avatar.anchorMax = new Vector2(0f, 1f);
            avatar.anchoredPosition = new Vector2(56f + 60f, -56f - 60f);

            var title = SundownFonts.UseTitle(NewText("Name", panel, 60, FontStyles.Normal, TextAlignmentOptions.BottomLeft,
                new Vector2(0f, 1f), Vector2.one, new Vector2(200f, -120f), new Vector2(-56f, -40f)));
            title.text = name.ToUpperInvariant();
            title.color = TextColor;

            var since = NewText("Since", panel, 22, FontStyles.Normal, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), Vector2.one, new Vector2(202f, -170f), new Vector2(-56f, -124f));
            since.text = profile.gamesPlayed == 0
                ? "No games yet. Your record starts with your first Sundown."
                : $"Playing since {profile.firstPlayed}  ·  last game {profile.lastPlayed}";
            since.color = MutedText;

            // The numbers, two rows of four
            var grid = NewRect("Stats", panel, new Vector2(0f, 1f), Vector2.one, new Vector2(56f, -440f), new Vector2(-56f, -200f));
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(236f, 112f);
            layout.spacing = new Vector2(12f, 12f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 4;

            Tile(grid, profile.gamesPlayed.ToString(), "GAMES PLAYED", TextColor);
            Tile(grid, profile.wins.ToString(), "MADE IT INSIDE", WinColor);
            Tile(grid, profile.gamesPlayed == 0 ? "-" : $"{Mathf.RoundToInt(profile.WinRate * 100f)}%", "WIN RATE", TextColor);
            Tile(grid, profile.currentStreak.ToString(), "CURRENT STREAK", AccentColor);
            Tile(grid, profile.longestStreak.ToString(), "LONGEST STREAK", AccentColor);
            Tile(grid, profile.keyWins.ToString(), "WINS WITH THE KEY", new Color32(0xFA, 0xC7, 0x75, 255));
            Tile(grid, profile.bestSupply.ToString(), "MOST REAL SUPPLY", TextColor);
            Tile(grid, profile.onlineGames.ToString(), "ONLINE GAMES", TextColor);

            // Last ten games, oldest on the left
            var caption = NewText("RecentLabel", panel, 18, FontStyles.Bold, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), Vector2.one, new Vector2(56f, -490f), new Vector2(-56f, -460f));
            caption.text = $"LAST {PlayerProfile.HistoryLength} GAMES";
            caption.characterSpacing = 12f;
            caption.color = MutedText;

            var dots = NewRect("Recent", panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(56f, -540f), new Vector2(600f, -500f));
            for (int i = 0; i < PlayerProfile.HistoryLength; i++)
            {
                int index = i - (PlayerProfile.HistoryLength - profile.recent.Count);
                Color color = index < 0 ? NoGameColor : profile.recent[index] ? WinColor : LossColor;
                var dot = NewRect("Game", dots, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
                dot.sizeDelta = new Vector2(30f, 30f);
                dot.anchoredPosition = new Vector2(15f + i * 42f, 0f);
                Image image = dot.gameObject.AddComponent<Image>();
                image.sprite = CircleSprite();
                image.color = color;
                image.raycastTarget = false;
            }
            var legend = NewText("Legend", panel, 18, FontStyles.Normal, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 1f), Vector2.one, new Vector2(500f, -540f), new Vector2(-56f, -500f));
            legend.text = "<color=#5FB87A>Green</color> made it inside    <color=#D95B5B>Red</color> left outside";
            legend.color = MutedText;

            // Reset asks twice; Done closes
            var footer = NewRect("Footer", panel, Vector2.zero, new Vector2(1f, 0f), new Vector2(56f, 36f), new Vector2(-56f, 100f));
            var buttons = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            buttons.spacing = 14f;
            buttons.childControlWidth = buttons.childControlHeight = true;
            buttons.childForceExpandWidth = buttons.childForceExpandHeight = true;

            Button reset = null;
            bool armed = false;
            reset = MakeButton(footer, "Reset stats", ButtonColor, () =>
            {
                if (!armed)
                {
                    armed = true;
                    reset.GetComponentInChildren<TextMeshProUGUI>().text = "Tap again to erase";
                    reset.colors = Tints(LossColor);
                    return;
                }
                PlayerProfile.Reset();
                Close(overlay, null);
                Profile(canvas, onClose); // show the fresh, empty record
            }, 22);
            MakeButton(footer, "Done", AccentColor, () => Close(overlay, onClose), 24);

            return overlay.gameObject;
        }

        private static void Tile(Transform grid, string value, string label, Color valueColor)
        {
            var tile = NewRect(label, grid, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Rounded(tile.gameObject.AddComponent<Image>(), TileColor, 16).raycastTarget = false;

            var number = SundownFonts.UseTitle(NewText("Value", tile, 52, FontStyles.Normal, TextAlignmentOptions.Center,
                new Vector2(0f, 0.36f), Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, -6f)));
            number.text = value;
            number.color = valueColor;

            var caption = NewText("Label", tile, 15, FontStyles.Bold, TextAlignmentOptions.Center,
                Vector2.zero, new Vector2(1f, 0.36f), new Vector2(8f, 8f), new Vector2(-8f, 0f));
            caption.text = label;
            caption.characterSpacing = 8f;
            caption.color = MutedText;
        }

        // ---------- Pieces ----------

        /// <summary>A full-screen dimmer that blocks taps, holding a pop-up.</summary>
        public static RectTransform Overlay(string name, Transform canvas)
        {
            var overlay = NewRect(name, canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.SetAsLastSibling();
            overlay.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
            return overlay;
        }

        private static void Close(RectTransform overlay, Action onClose)
        {
            UnityEngine.Object.Destroy(overlay.gameObject);
            PlayerPrefs.Save();
            onClose?.Invoke();
        }

        /// <summary>A row of buttons where exactly one is lit, like On / Off.</summary>
        private static void Segmented(RectTransform parent, string[] options, int selected, Action<int> onPick)
        {
            var row = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8f;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = true;

            var buttons = new Button[options.Length];
            void Light(int pick)
            {
                for (int i = 0; i < buttons.Length; i++) buttons[i].colors = Tints(i == pick ? AccentColor : ButtonColor);
            }
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                buttons[i] = MakeButton(parent, options[i], ButtonColor, () =>
                {
                    Light(index);
                    onPick(index);
                }, 22);
            }
            Light(selected);
        }

        private static Slider MakeSlider(RectTransform parent, float value)
        {
            var slider = parent.gameObject.AddComponent<Slider>();

            var track = NewRect("Track", parent, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -6f), new Vector2(0f, 6f));
            Rounded(track.gameObject.AddComponent<Image>(), FieldColor, 6);

            var fillArea = NewRect("FillArea", parent, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -6f), new Vector2(0f, 6f));
            var fill = NewRect("Fill", fillArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Rounded(fill.gameObject.AddComponent<Image>(), AccentColor, 6).raycastTarget = false;

            var handleArea = NewRect("HandleArea", parent, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(16f, -16f), new Vector2(-16f, 16f));
            var handle = NewRect("Handle", handleArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            handle.sizeDelta = new Vector2(32f, 0f);
            Image knob = handle.gameObject.AddComponent<Image>();
            knob.sprite = CircleSprite();
            knob.color = TextColor;

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = knob;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(value);
            return slider;
        }
    }
}

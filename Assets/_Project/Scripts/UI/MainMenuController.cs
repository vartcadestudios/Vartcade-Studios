using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TDTS.Data;
using TDTS.Managers;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// The title screen, built from wireframe 01 (v0.1): the animated environment with the bunker
    /// hatch, the logo and tagline, Play online / Play vs bots / Tutorial, the Cards, Profile and
    /// Settings row, Quit on PC, an idle puppet by the hatch and the profile chip.
    /// Builds its own UI at runtime and hides the scene's older canvas.
    /// Attach to an empty "MainMenuController" GameObject in the MainMenu scene.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string lobbySceneName = "Lobby";
        [SerializeField] private string gameSceneName = "Game";
        [SerializeField] private string cardGallerySceneName = "CardGallery";

        [Header("Old scene panels (hidden; kept so their buttons still compile)")]
        [SerializeField] private GameObject howToPlayPanel;
        [SerializeField] private GameObject settingsPanel;

        private const string TutorialSeenKey = "menu.tutorialSeen";
        private static bool logoDropped; // the logo bounces in on first load only

        private Transform canvas;
        private RectTransform logo;
        private RectTransform profileChip;
        private GameObject newTag;
        private float logoStart;
        private bool art;              // the v3 street, paper buttons, mirror and shop (Resources/StreetV3, UI3)
        private RectTransform mirror;

        private void Start()
        {
            HideSceneCanvas();
            EnsureCameraAndEventSystem();
            canvas = PlainCanvas(transform);
            art = StreetBackdrop.Available && ArtKit.Has("UI3/btn_play");
            if (art)
                StreetBackdrop.Create(canvas);
            else
            {
                MenuBackground background = MenuBackground.Create(canvas);
                Puppet.Create(background.Foreground, new Vector2(0.945f, 0.105f), 1, 300f);
            }
            BuildLogo();
            BuildButtons();
            RefreshProfile();
            BuildFooter();
            if (art) ArtKit.Full(canvas, "ScreenFrame", ArtKit.Get("UI3/screen_frame")); // wooden frame + slight vignette, on top
            logoStart = Time.unscaledTime;
        }

        // Logo drops in with a small bounce, first load only
        private void Update()
        {
            if (logo == null || logoDropped) return;
            float t = (Time.unscaledTime - logoStart) / 0.7f;
            if (t >= 1f)
            {
                logo.anchoredPosition = Vector2.zero;
                logoDropped = true;
                return;
            }
            float bounce = 1f - Mathf.Pow(1f - t, 3f) * Mathf.Cos(t * Mathf.PI * 2.5f);
            logo.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(160f, 0f, bounce));
        }

        public void OnPlayOnlinePressed() => SceneManager.LoadScene(lobbySceneName);

        public void OnPlayBotsPressed() => SceneManager.LoadScene(gameSceneName);

        // Kept for the old scene buttons
        public void OnPlayPressed() => OnPlayOnlinePressed();

        public void OnTutorialPressed()
        {
            // The guided game (W15) is still to come; until then the tutorial opens How to play
            PlayerPrefs.SetInt(TutorialSeenKey, 1);
            PlayerPrefs.Save();
            if (newTag != null) newTag.SetActive(false);
            MenuScreens.HowToPlay(canvas);
        }

        public void OnHowToPlayPressed() => MenuScreens.HowToPlay(canvas);

        public void OnCardsPressed() => SceneManager.LoadScene(cardGallerySceneName);

        public void OnSettingsPressed() => MenuScreens.Settings(canvas);

        public void OnProfilePressed() => MenuScreens.Profile(canvas, RefreshProfile); // a reset shows straight away

        private void RefreshProfile()
        {
            if (art) BuildMirror();
            else BuildProfileChip();
        }

        public void OnQuitPressed()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void ClosePanel(GameObject panel)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        // ---------- Building ----------

        private void HideSceneCanvas()
        {
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
                if (root != gameObject && root.GetComponent<Canvas>() != null) root.SetActive(false);
        }

        private static Transform PlainCanvas(Transform parent)
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return go.transform;
        }

        // 01-3: TEN DAYS in cream, TO SUNDOWN in gold, hard charcoal shadow, then the tagline
        private void BuildLogo()
        {
            var holder = NewRect("LogoArea", canvas, new Vector2(0f, 1f), new Vector2(0.6f, 1f), new Vector2(120f, -440f), new Vector2(0f, -120f));
            logo = NewRect("Logo", holder, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            if (logoDropped) logo.anchoredPosition = Vector2.zero;

            var title = SundownFonts.Poster(NewText("Title", logo, 150, FontStyles.Normal, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero));
            title.text = $"TEN DAYS\n<color={UITheme.GoldHex}><size=82%>TO SUNDOWN</size></color>";
            title.lineSpacing = -48f;
            var shadow = title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = UITheme.Charcoal;
            shadow.effectDistance = new Vector2(6f, -6f);

            var tagline = NewText("Tagline", logo, 32, FontStyles.Italic, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, -60f), new Vector2(0f, 0f));
            tagline.text = "The bunker has room for two. Make sure one of them is you.";
            tagline.color = UITheme.Cream;
            tagline.characterSpacing = 2f;
        }

        // 01-4 to 01-7: the buttons sit in the left half so thumbs reach them on phones
        private void BuildButtons()
        {
            float left = art ? 106f : 120f, width = art ? 520f : 492f;
            float y = art ? -470f : -510f;
            float gap = art ? 12f : 24f;

            RectTransform Slot(float height)
            {
                var slot = NewRect("Slot", canvas, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(left, y - height), new Vector2(left + width, y));
                y -= height + gap;
                return slot;
            }

            // v3: paper buttons with a bevel, brass rivets and a cut edge (UI3/btn_*); the art is taller than the flat buttons
            Chunky(Slot(art ? 116f : 92f), "PLAY ONLINE", UITheme.Brick, UITheme.Cream, 46, OnPlayOnlinePressed, sprite: Skin("btn_play"));
            Chunky(Slot(art ? 104f : 80f), "PLAY VS BOTS", UITheme.Cream, UITheme.Charcoal, 38, OnPlayBotsPressed, sprite: Skin("btn_bots"));
            Button tutorial = Chunky(Slot(art ? 104f : 80f), "TUTORIAL", UITheme.Gold, UITheme.Charcoal, 38, OnTutorialPressed, sprite: Skin("btn_tutorial"));

            // NEW tag until the tutorial has been opened once
            var tag = NewRect("New", tutorial.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-122f, -20f), new Vector2(-28f, 20f));
            Rounded(tag.gameObject.AddComponent<Image>(), UITheme.Charcoal, 6).raycastTarget = false;
            var tagText = SundownFonts.UseTitle(NewText("Label", tag, 22, FontStyles.Normal, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero));
            tagText.text = "NEW";
            tagText.color = UITheme.Gold;
            tagText.characterSpacing = 8f;
            newTag = tag.gameObject;
            newTag.SetActive(PlayerPrefs.GetInt(TutorialSeenKey, 0) == 0);

            // Secondary row: Cards, Profile, Settings
            RectTransform row = Slot(art ? 92f : 68f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            foreach ((string text, UnityEngine.Events.UnityAction action) in new (string, UnityEngine.Events.UnityAction)[]
                     {
                         ("CARDS", OnCardsPressed), ("PROFILE", OnProfilePressed), ("SETTINGS", OnSettingsPressed),
                     })
            {
                var cell = NewRect(text, row, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                Chunky(cell, text, UITheme.Charcoal, UITheme.Cream, 28, action, sprite: Skin("btn_small"));
            }

            // Quit only on PC builds
            if (!Application.isMobilePlatform)
            {
                RectTransform quitSlot = Slot(art ? 86f : 58f);
                quitSlot.offsetMax = new Vector2(left + (art ? 212f : 180f), quitSlot.offsetMax.y);
                Button quit = Chunky(quitSlot, "QUIT", new Color(0f, 0f, 0f, 0.25f), UITheme.Cream, 26, OnQuitPressed, shadowed: false, sprite: Skin("btn_quit"));
                if (!art)
                {
                    var outline = quit.gameObject.AddComponent<Outline>();
                    outline.effectColor = UITheme.WithAlpha(UITheme.Cream, 0.5f);
                    outline.effectDistance = new Vector2(2f, -2f);
                }
            }
        }

        /// <summary>A flat poster button: Anton label, a hard charcoal shadow under it, hover tilt and press-down.</summary>
        private Sprite Skin(string name) => art ? ArtKit.Get("UI3/" + name) : null;

        private static Button Chunky(RectTransform slot, string text, Color fill, Color textColor, float size,
            UnityEngine.Events.UnityAction onClick, bool shadowed = true, Sprite sprite = null)
        {
            if (shadowed && sprite == null)
            {
                var shadow = NewRect("Shadow", slot, Vector2.zero, Vector2.one, new Vector2(0f, -6f), new Vector2(0f, -6f));
                Rounded(shadow.gameObject.AddComponent<Image>(), UITheme.WithAlpha(UITheme.Charcoal, 0.9f), 8).raycastTarget = false;
            }

            var face = NewRect(text, slot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image background = face.gameObject.AddComponent<Image>();
            if (sprite != null) ArtKit.Skin(background, sprite); // the drawn button keeps its own colours
            else Plate(background, Color.white, 8);
            var button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = Tints(sprite != null ? Color.white : fill);
            colors.highlightedColor = sprite != null ? Color.white : fill; // the tilt and scale show hover; keep the colour true
            button.colors = colors;
            button.onClick.AddListener(onClick);
            face.gameObject.AddComponent<MenuButtonFx>();

            // The drawn buttons have a lip at the bottom and rivets at the ends, so the label sits a little higher and further in
            var label = SundownFonts.UseTitle(NewText("Label", face, size, FontStyles.Normal, TextAlignmentOptions.Left,
                Vector2.zero, Vector2.one, sprite != null ? new Vector2(46f, 10f) : new Vector2(30f, 0f), sprite != null ? new Vector2(-36f, -4f) : new Vector2(-20f, 0f)));
            if (slot.rect.width < 200f || text.Length <= 8 && size <= 30) label.alignment = TextAlignmentOptions.Center;
            label.text = text;
            label.color = textColor;
            label.characterSpacing = 4f;
            label.textWrappingMode = TextWrappingModes.NoWrap; // "SETTINGS" stays on one line
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = size;
            return button;
        }

        // 01-9: name, wins and streak; tap to open the profile
        private void BuildProfileChip()
        {
            if (profileChip != null) Destroy(profileChip.gameObject);
            PlayerProfile profile = PlayerProfile.Current;
            string name = MenuScreens.ProfileName;

            profileChip = NewRect("ProfileChip", canvas, Vector2.one, Vector2.one, new Vector2(-470f, -150f), new Vector2(-60f, -60f));
            Image background = Rounded(profileChip.gameObject.AddComponent<Image>(), Color.white, 45);
            var button = profileChip.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.colors = Tints(UITheme.WithAlpha(UITheme.Charcoal, 0.85f));
            button.onClick.AddListener(OnProfilePressed);
            var edge = profileChip.gameObject.AddComponent<Outline>();
            edge.effectColor = UITheme.WithAlpha(UITheme.Cream, 0.18f);

            RectTransform avatar = MenuScreens.Avatar(profileChip, name, 62f);
            avatar.anchorMin = avatar.anchorMax = new Vector2(0f, 0.5f);
            avatar.anchoredPosition = new Vector2(16f + 31f, 0f);

            var title = NewText("Name", profileChip, 28, FontStyles.Bold, TextAlignmentOptions.BottomLeft,
                new Vector2(0f, 0.5f), Vector2.one, new Vector2(94f, 0f), new Vector2(-20f, -6f));
            title.text = name;
            title.color = UITheme.Cream;

            var stats = NewText("Stats", profileChip, 20, FontStyles.Normal, TextAlignmentOptions.TopLeft,
                Vector2.zero, new Vector2(1f, 0.5f), new Vector2(94f, 6f), new Vector2(-20f, -2f));
            stats.text = profile.gamesPlayed == 0 ? "View profile" : $"{profile.wins} wins  ·  streak {profile.currentStreak}";
            stats.color = UITheme.WithAlpha(UITheme.Cream, 0.7f);
        }

        // ---------- v3: the avatar in a vanity mirror, the name plaque and the locked shop ----------

        /// <summary>
        /// Top right: your puppet looks back out of a carved vanity mirror (UI3/mirror_*); the name plaque under it
        /// opens the profile; below that the TINDAHAN shop, locked until there's clothing to buy.
        /// </summary>
        private void BuildMirror()
        {
            if (mirror != null) Destroy(mirror.gameObject);
            PlayerProfile profile = PlayerProfile.Current;
            mirror = NewRect("MirrorCorner", canvas, Vector2.one, Vector2.one, new Vector2(-610f, -1048f), new Vector2(-90f, -22f));

            // The mirror is 520 x 720; its glass is an ellipse 400 x 510 centred 410 px from the top
            var frame = NewRect("Mirror", mirror, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -720f), Vector2.zero);
            var back = ArtKit.Full(frame, "Glass", ArtKit.Get("UI3/mirror_back"));
            back.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            Puppet you = Puppet.Create(back.transform, new Vector2(0.5f, (720f - 665f) / 720f), 1, 470f);
            if (you != null)
            {
                foreach (Graphic g in you.GetComponentsInChildren<Graphic>()) g.color = Color.Lerp(g.color, new Color(0.75f, 0.85f, 0.88f, g.color.a), 0.12f);
            }
            ArtKit.Full(frame, "Reflection", ArtKit.Get("UI3/mirror_glass"));
            ArtKit.Full(frame, "Frame", ArtKit.Get("UI3/mirror_frame"));

            // Name plaque, hanging on the bottom of the frame; tap for the profile
            var plaque = NewRect("Plaque", mirror, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-180f, -730f), new Vector2(180f, -604f));
            Image plate = plaque.gameObject.AddComponent<Image>();
            plate.sprite = ArtKit.Get("UI3/plaque");
            var button = plaque.gameObject.AddComponent<Button>();
            button.targetGraphic = plate;
            button.colors = Tints(Color.white);
            button.onClick.AddListener(OnProfilePressed);
            plaque.gameObject.AddComponent<MenuButtonFx>();
            var plaqueName = SundownFonts.UseTitle(NewText("Name", plaque, 32, FontStyles.Normal, TextAlignmentOptions.Center,
                new Vector2(0f, 0.5f), Vector2.one, new Vector2(40f, -8f), new Vector2(-40f, -14f)));
            plaqueName.text = MenuScreens.ProfileName.ToUpperInvariant();
            plaqueName.color = UITheme.Charcoal;
            plaqueName.enableAutoSizing = true;
            plaqueName.fontSizeMin = 18f;
            plaqueName.fontSizeMax = 32f;
            var stats = NewText("Stats", plaque, 16, FontStyles.Normal, TextAlignmentOptions.Center,
                new Vector2(0f, 0.2f), new Vector2(1f, 0.5f), new Vector2(40f, 0f), new Vector2(-40f, -6f));
            stats.text = profile.gamesPlayed == 0 ? "View profile" : $"{profile.wins} wins  ·  streak {profile.currentStreak}";
            stats.color = new Color32(0x5A, 0x4A, 0x3A, 255);

            // The shop: a little sari-sari stall with eight locked slots
            var shop = NewRect("Shop", mirror, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-210f, 0f), new Vector2(210f, 258f));
            Image stall = shop.gameObject.AddComponent<Image>();
            stall.sprite = ArtKit.Get("UI3/shop");
            stall.color = new Color(0.88f, 0.86f, 0.84f, 1f); // a touch faded: it's locked
            stall.raycastTarget = false;
            Tag(shop, "Sign", "TINDAHAN", new Vector2(0.5f, 1f), new Vector2(-90f, -2f), new Vector2(90f, 40f), UITheme.Cream, UITheme.Charcoal, 28, -2f);
            Tag(shop, "Soon", "SOON", new Vector2(1f, 1f), new Vector2(-70f, -96f), new Vector2(16f, -56f), UITheme.Brick, UITheme.Gold, 26, 12f);
            // Points: wins count most, every game a little (the store itself is checklist S01-S05)
            int points = profile.wins * 10 + profile.gamesPlayed * 2;
            Tag(shop, "Points", $"{points}<size=50%> PTS</size>", new Vector2(0f, 1f), new Vector2(-36f, -112f), new Vector2(64f, -64f), UITheme.Gold, UITheme.Charcoal, 26, -6f);
            var note = NewText("Note", shop, 15, FontStyles.Normal, TextAlignmentOptions.Center,
                Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, -6f), new Vector2(0f, 18f));
            note.text = "Win games to earn points  ·  clothes coming soon";
            note.color = UITheme.WithAlpha(UITheme.Cream, 0.9f);
        }

        private static void Tag(Transform parent, string name, string text, Vector2 anchor, Vector2 min, Vector2 max,
            Color fill, Color ink, float size, float tilt)
        {
            var rect = NewRect(name, parent, anchor, anchor, min, max);
            rect.localEulerAngles = new Vector3(0f, 0f, tilt);
            var shadow = NewRect("Shadow", rect, Vector2.zero, Vector2.one, new Vector2(4f, -6f), new Vector2(4f, -6f));
            Rounded(shadow.gameObject.AddComponent<Image>(), new Color(0f, 0f, 0f, 0.35f), 6).raycastTarget = false;
            var face = NewRect("Face", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Rounded(face.gameObject.AddComponent<Image>(), fill, 6).raycastTarget = false;
            var edge = face.gameObject.AddComponent<Outline>();
            edge.effectColor = UITheme.Charcoal;
            edge.effectDistance = new Vector2(2f, -2f);
            var label = SundownFonts.UseTitle(NewText("Label", face, size, FontStyles.Normal, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero));
            label.text = text;
            label.color = ink;
        }

        private void BuildFooter()
        {
            var footer = NewText("Footer", canvas, 20, FontStyles.Normal, art ? TextAlignmentOptions.BottomLeft : TextAlignmentOptions.BottomRight,
                art ? new Vector2(0.36f, 0f) : new Vector2(1f, 0f), art ? new Vector2(0.36f, 0f) : new Vector2(1f, 0f),
                art ? new Vector2(0f, 40f) : new Vector2(-500f, 24f), art ? new Vector2(460f, 76f) : new Vector2(-36f, 60f));
            footer.text = $"v{Application.version}  ·  Vartcade Studio";
            footer.color = UITheme.WithAlpha(UITheme.Cream, 0.45f);
        }
    }
}

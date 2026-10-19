using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// The title screen: the sundown sky, the game's name, a fan of cards, and Play, How to play,
    /// Settings and Quit. Builds its own UI at runtime and hides the scene's older canvas.
    /// Attach to an empty "MainMenuController" GameObject in the MainMenu scene.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string lobbySceneName = "Lobby";

        [Header("Old scene panels (hidden; kept so their buttons still compile)")]
        [SerializeField] private GameObject howToPlayPanel;
        [SerializeField] private GameObject settingsPanel;

        private Transform canvas;
        private RectTransform fan;

        private void Start()
        {
            HideSceneCanvas();
            EnsureCameraAndEventSystem();
            canvas = SkyCanvas(transform);
            BuildColumn();
            BuildCardFan();
        }

        // A slow sway, like cards held in a tired hand
        private void Update()
        {
            if (fan != null) fan.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(Time.unscaledTime * 0.6f) * 1.5f);
        }

        public void OnPlayPressed() => SceneManager.LoadScene(lobbySceneName);

        public void OnHowToPlayPressed() => MenuScreens.HowToPlay(canvas);

        public void OnSettingsPressed() => MenuScreens.Settings(canvas);

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

        /// <summary>
        /// Everything on the left stacks in one column, top to bottom: title, tagline, a flexible gap,
        /// the buttons, then the footer. On a short window the gap shrinks first, so nothing overlaps.
        /// </summary>
        private void BuildColumn()
        {
            var column = NewRect("Column", canvas, Vector2.zero, new Vector2(0.56f, 1f), new Vector2(160f, 36f), new Vector2(0f, -140f));
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var eyebrow = Stacked(NewText("Eyebrow", column, 24, FontStyles.Bold, TextAlignmentOptions.Left,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero));
            eyebrow.text = "VARTCADE STUDIOS PRESENTS";
            eyebrow.characterSpacing = 20f;
            eyebrow.color = AccentColor;

            var title = Stacked(SundownFonts.UseTitle(NewText("Title", column, 128, FontStyles.Normal, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero)));
            title.text = "TEN DAYS\nTO <color=#F28C3A>SUNDOWN</color>";
            title.lineSpacing = -50f; // Anton has a tall line gap; pull the two lines together
            title.color = TextColor;

            var tagline = Stacked(NewText("Tagline", column, 30, FontStyles.Italic, TextAlignmentOptions.Left,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero));
            tagline.text = "Survive the night. Trust no one.";
            tagline.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.75f);

            var gap = NewRect("Gap", column, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<LayoutElement>();
            gap.minHeight = 20f;
            gap.flexibleHeight = 1f;

            var buttons = NewRect("Buttons", column, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var size = buttons.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = 440f;
            var stack = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.spacing = 14f;
            stack.childControlWidth = stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            Button play = MakeButton(buttons, "Play", AccentColor, OnPlayPressed, 34);
            play.GetComponent<LayoutElement>().preferredHeight = 88f;
            foreach ((string text, UnityEngine.Events.UnityAction action) in new (string, UnityEngine.Events.UnityAction)[]
                     {
                         ("How to play", OnHowToPlayPressed),
                         ("Settings", OnSettingsPressed),
                         ("Quit", OnQuitPressed),
                     })
            {
                MakeButton(buttons, text, ButtonColor, action, 26).GetComponent<LayoutElement>().preferredHeight = 64f;
            }

            var footer = Stacked(NewText("Footer", column, 18, FontStyles.Normal, TextAlignmentOptions.BottomLeft,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero));
            footer.text = "A card game of supplies, secrets and the last two seats.";
            footer.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.45f);
            footer.margin = new Vector4(0f, 14f, 0f, 0f);
        }

        // Text in the column takes its full width and exactly the height it needs
        private static TextMeshProUGUI Stacked(TextMeshProUGUI label)
        {
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            return label;
        }

        /// <summary>Five face-down cards fanned out over the setting sun.</summary>
        private void BuildCardFan()
        {
            fan = NewRect("CardFan", canvas, new Vector2(0.73f, 0.42f), new Vector2(0.73f, 0.42f), Vector2.zero, Vector2.zero);
            fan.pivot = new Vector2(0.5f, 0f);
            const int count = 5;
            for (int i = 0; i < count; i++)
            {
                float fromMiddle = i - (count - 1) / 2f;
                RectTransform card = CardBack(fan, new Vector2(220f, 308f));
                card.pivot = new Vector2(0.5f, 0f);
                card.anchoredPosition = new Vector2(fromMiddle * 96f, -Mathf.Abs(fromMiddle) * 22f);
                card.localEulerAngles = new Vector3(0f, 0f, -fromMiddle * 11f);
            }
        }
    }
}

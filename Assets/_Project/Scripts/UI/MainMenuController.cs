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
            BuildTitle();
            BuildButtons();
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

        private void BuildTitle()
        {
            var eyebrow = NewText("Eyebrow", canvas, 24, FontStyles.Bold, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(0.6f, 1f), new Vector2(160f, -230f), new Vector2(0f, -190f));
            eyebrow.text = "VARTCADE STUDIOS PRESENTS";
            eyebrow.characterSpacing = 20f;
            eyebrow.color = AccentColor;

            var title = SundownFonts.UseTitle(NewText("Title", canvas, 132, FontStyles.Normal, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), new Vector2(0.62f, 1f), new Vector2(156f, -520f), new Vector2(0f, -230f)));
            title.text = "TEN DAYS\nTO <color=#F28C3A>SUNDOWN</color>";
            title.lineSpacing = -18f;
            title.color = TextColor;

            var tagline = NewText("Tagline", canvas, 30, FontStyles.Italic, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(0.6f, 1f), new Vector2(162f, -580f), new Vector2(0f, -530f));
            tagline.text = "Survive the night. Trust no one.";
            tagline.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.75f);
        }

        private void BuildButtons()
        {
            var column = NewRect("Buttons", canvas, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(160f, -980f), new Vector2(600f, -620f));
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Button play = MakeButton(column, "Play", AccentColor, OnPlayPressed, 34);
            play.GetComponent<LayoutElement>().preferredHeight = 96f;
            foreach ((string text, UnityEngine.Events.UnityAction action) in new (string, UnityEngine.Events.UnityAction)[]
                     {
                         ("How to play", OnHowToPlayPressed),
                         ("Settings", OnSettingsPressed),
                         ("Quit", OnQuitPressed),
                     })
            {
                MakeButton(column, text, ButtonColor, action, 26).GetComponent<LayoutElement>().preferredHeight = 70f;
            }

            var footer = NewText("Footer", canvas, 18, FontStyles.Normal, TextAlignmentOptions.BottomLeft,
                Vector2.zero, new Vector2(0.5f, 0f), new Vector2(160f, 30f), new Vector2(0f, 70f));
            footer.text = "A card game of supplies, secrets and the last two seats.";
            footer.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.45f);
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

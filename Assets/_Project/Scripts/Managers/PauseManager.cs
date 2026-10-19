using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TDTS.Networking;
using TDTS.UI;
using static TDTS.UI.SundownArt;

namespace TDTS.Managers
{
    /// <summary>
    /// The Esc menu: Resume, How to play, Settings and Quit to menu. Offline, Time.timeScale is the
    /// actual "pause" and the menu is its visual reflection. Online the table can't stop for one
    /// player, so the menu opens over a game that keeps going.
    /// Builds its own menu at runtime; the scene's old pause panel stays hidden.
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        public static PauseManager Instance { get; private set; }

        [Header("Old scene panel (hidden)")]
        [SerializeField] private GameObject pausePanel;

        [Header("Scene Names")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        public bool IsPaused { get; private set; }

        private GameObject menu;
        private GameObject subScreen; // How to play or Settings, opened from the menu

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (subScreen != null) CloseSubScreen(); // Esc backs out one step at a time
            else TogglePause();
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
            if (!NetworkGameBridge.IsOnline) Time.timeScale = IsPaused ? 0f : 1f;

            if (IsPaused) menu = BuildMenu();
            else
            {
                CloseSubScreen();
                if (menu != null) Destroy(menu);
                menu = null;
            }
        }

        public void OnResumePressed() => TogglePause();

        public void OnQuitToMenuPressed()
        {
            Time.timeScale = 1f; // always restore before leaving the scene
            if (NetworkGameBridge.IsOnline && SessionManager.Instance != null) _ = SessionManager.Instance.LeaveAsync();
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void CloseSubScreen()
        {
            if (subScreen != null) Destroy(subScreen);
            subScreen = null;
        }

        // ---------- Building ----------

        private GameObject BuildMenu()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return null;
            Transform root = canvas.rootCanvas.transform;
            bool online = NetworkGameBridge.IsOnline;

            RectTransform overlay = MenuScreens.Overlay("PauseMenu", root);
            var panel = NewPanel("Panel", overlay, new Vector2(-300f, -330f), new Vector2(300f, 330f));

            // A small setting sun on the horizon, like the card backs
            var sun = NewRect("Sun", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-34f, -84f), new Vector2(34f, -16f));
            Image disc = sun.gameObject.AddComponent<Image>();
            disc.sprite = CircleSprite();
            disc.color = SunColor;
            var below = NewRect("Ground", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-40f, -86f), new Vector2(40f, -50f));
            below.gameObject.AddComponent<Image>().color = PanelColor;
            var horizon = NewRect("Horizon", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-70f, -52f), new Vector2(70f, -49f));
            horizon.gameObject.AddComponent<Image>().color = AccentColor;

            var title = SundownFonts.UseTitle(NewText("Title", panel, 64, FontStyles.Normal, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), Vector2.one, new Vector2(24f, -170f), new Vector2(-24f, -76f)));
            title.text = online ? "Menu" : "Paused";
            title.color = TextColor;

            var note = NewText("Note", panel, 22, FontStyles.Normal, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), Vector2.one, new Vector2(24f, -214f), new Vector2(-24f, -170f));
            IGameView view = GameView.Current;
            note.text = online ? "The table keeps playing while this is open."
                : view != null ? $"Day {view.CurrentDay} of {view.SundownDay}" : "";
            note.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.6f);

            var column = NewRect("Buttons", panel, Vector2.zero, Vector2.one, new Vector2(56f, 44f), new Vector2(-56f, -236f));
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;

            MakeButton(column, "Resume", AccentColor, OnResumePressed, 28);
            MakeButton(column, "How to play", ButtonColor, () => subScreen = MenuScreens.HowToPlay(root, () => subScreen = null), 24);
            MakeButton(column, "Settings", ButtonColor, () => subScreen = MenuScreens.Settings(root, () => subScreen = null), 24);
            MakeButton(column, online ? "Leave table" : "Quit to menu", ButtonColor, OnQuitToMenuPressed, 24);

            return overlay.gameObject;
        }
    }
}

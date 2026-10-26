using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TDTS.Data;
using TDTS.Managers;
using TDTS.Networking;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// The Sundown results screen. When the state machine reaches Sundown, the table dims and a
    /// card rises with your own fate up top, then everyone's result one row at a time (spec R1/R2):
    /// who got inside the bunker and why, who was left outside and why.
    /// Built in code; the scene's old panel stays hidden. Never triggered manually.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        [Header("Old scene UI (hidden; the screen is built in code)")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI resultText;

        [Header("Scene Names")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [SerializeField] private float delaySeconds = 1.2f; // let the last move land first

        private static readonly Color Inside = new Color32(0x5F, 0xB8, 0x7A, 255);
        private static readonly Color Outside = new Color32(0xD9, 0x5B, 0x5B, 255);
        private static readonly Color RowColor = new Color32(0x1A, 0x1E, 0x2E, 255);

        private void Start()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (resultText != null) resultText.gameObject.SetActive(false);
            if (GameView.Current != null)
                GameView.Current.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (GameView.Current != null)
                GameView.Current.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState previous, GameState next)
        {
            if (next != GameState.Sundown || recorded) return;
            RecordResult();
            StartCoroutine(Show());
        }

        // ---------- Your profile ----------

        private bool recorded;
        private bool newBestStreak;

        /// <summary>
        /// Adds this game to your profile straight away, before the screen appears, so leaving early
        /// still counts it. Uses the host's verdict and reason, since a client never sees unpeeked backs.
        /// </summary>
        private void RecordResult()
        {
            PlayerData me = GameView.Current?.LocalPlayer;
            if (me == null) return;
            recorded = true;

            bool heldKey = me.resultReason.Contains("Bunker Key");
            Match supply = Regex.Match(me.resultReason, @"\d+");
            newBestStreak = PlayerProfile.Current.Record(me.isSeated, heldKey,
                supply.Success ? int.Parse(supply.Value) : 0, NetworkGameBridge.IsOnline);
        }

        /// <summary>A line under your verdict about your streak, when there's one worth mentioning.</summary>
        private string StreakLine()
        {
            PlayerProfile profile = PlayerProfile.Current;
            if (!recorded) return "";
            if (newBestStreak && profile.longestStreak > 1) return $"<color=#E07A2E><b>{profile.longestStreak} wins in a row. New best streak!</b></color>";
            if (profile.currentStreak > 1) return $"<color=#E07A2E><b>{profile.currentStreak} wins in a row</b></color>";
            return "";
        }

        public void OnBackToMenuPressed()
        {
            Time.timeScale = 1f;
            if (NetworkGameBridge.IsOnline && SessionManager.Instance != null) _ = SessionManager.Instance.LeaveAsync();
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void OnPlayAgainPressed() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // ---------- The screen ----------

        private IEnumerator Show()
        {
            yield return new WaitForSeconds(delaySeconds);

            IGameView view = GameView.Current;
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (view == null || canvas == null) yield break;

            var players = view.Players.OrderByDescending(p => p.isSeated).ToList();
            PlayerData me = view.LocalPlayer;
            bool survived = me != null && me.isSeated;
            int seated = players.Count(p => p.isSeated);

            RectTransform overlay = MenuScreens.Overlay("Sundown", canvas.rootCanvas.transform);
            overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);
            var fade = overlay.gameObject.AddComponent<CanvasGroup>();

            float rowsHeight = players.Count * 84f;
            float half = Mathf.Min(470f, 230f + rowsHeight / 2f);
            var panel = NewPanel("Panel", overlay, new Vector2(-560f, -half), new Vector2(560f, half));

            // Your fate first, in the colour of the outcome
            var eyebrow = NewText("Eyebrow", panel, 24, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), Vector2.one, new Vector2(40f, -80f), new Vector2(-40f, -40f));
            eyebrow.text = $"SUNDOWN  ·  {seated} OF {players.Count} MADE IT INSIDE";
            eyebrow.characterSpacing = 16f;
            eyebrow.color = AccentColor;

            var verdict = SundownFonts.UseTitle(NewText("Verdict", panel, 84, FontStyles.Normal, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), Vector2.one, new Vector2(40f, -190f), new Vector2(-40f, -84f)));
            verdict.text = me == null ? "THE DOORS ARE CLOSED"
                : survived ? "YOU MADE IT INSIDE" : "YOU WERE LEFT OUTSIDE";
            verdict.color = me == null ? TextColor : survived ? Inside : Outside;
            verdict.enableAutoSizing = true;
            verdict.fontSizeMin = 40f;
            verdict.fontSizeMax = 84f;

            var why = NewText("Why", panel, 26, FontStyles.Normal, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), Vector2.one, new Vector2(40f, -236f), new Vector2(-40f, -190f));
            string streak = StreakLine();
            why.text = (me != null ? Reason(me) : "") + (streak.Length > 0 ? $"   ·   {streak}" : "");
            why.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.7f);

            // Everyone, survivors first
            var list = NewRect("Players", panel, Vector2.zero, Vector2.one, new Vector2(48f, 130f), new Vector2(-48f, -262f));
            var stack = list.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.spacing = 10f;
            stack.childControlWidth = stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            var rows = players.Select(p => Row(list, p, p == me)).ToList();

            var buttons = NewRect("Buttons", panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-300f, 36f), new Vector2(300f, 104f));
            var line = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            line.spacing = 14f;
            line.childControlWidth = line.childControlHeight = true;
            line.childForceExpandWidth = line.childForceExpandHeight = true;
            if (!NetworkGameBridge.IsOnline) MakeButton(buttons, "Play again", AccentColor, OnPlayAgainPressed, 26);
            MakeButton(buttons, NetworkGameBridge.IsOnline ? "Leave table" : "Main menu",
                NetworkGameBridge.IsOnline ? AccentColor : ButtonColor, OnBackToMenuPressed, 26);

            // Fade in, then reveal the rows one by one
            foreach (CanvasGroup row in rows) row.alpha = 0f;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.4f)
            {
                fade.alpha = t;
                panel.anchoredPosition = new Vector2(0f, Mathf.Lerp(-30f, 0f, t));
                yield return null;
            }
            fade.alpha = 1f;
            panel.anchoredPosition = Vector2.zero;
            foreach (CanvasGroup row in rows)
            {
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.25f)
                {
                    row.alpha = t;
                    yield return null;
                }
                row.alpha = 1f;
            }
        }

        /// <summary>One player's line: a coloured marker, their name and why, and INSIDE or OUTSIDE.</summary>
        private static CanvasGroup Row(Transform parent, PlayerData player, bool isYou)
        {
            var row = NewRect(player.playerName, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 74f;
            Rounded(row.gameObject.AddComponent<Image>(), RowColor, 16).raycastTarget = false;
            Color fate = player.isSeated ? Inside : Outside;

            var marker = NewRect("Marker", row, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 10f), new Vector2(8f, -10f));
            Rounded(marker.gameObject.AddComponent<Image>(), fate, 4).raycastTarget = false;

            var name = NewText("Name", row, 30, FontStyles.Bold, TextAlignmentOptions.MidlineLeft,
                Vector2.zero, new Vector2(0.72f, 1f), new Vector2(30f, 0f), Vector2.zero);
            name.text = $"{player.playerName}{(isYou ? "  <size=65%><color=#E07A2E>YOU</color></size>" : "")}" +
                        $"   <size=70%><color=#A9AEBD>{Reason(player)}</color></size>";
            name.color = TextColor;
            name.enableAutoSizing = true;
            name.fontSizeMin = 16f;
            name.fontSizeMax = 30f;

            var pill = NewRect("Fate", row, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-190f, -22f), new Vector2(-20f, 22f));
            Rounded(pill.gameObject.AddComponent<Image>(), new Color(fate.r, fate.g, fate.b, 0.2f), 22).raycastTarget = false;
            var label = NewText("Label", pill, 20, FontStyles.Bold, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.text = player.isSeated ? "INSIDE" : "OUTSIDE";
            label.characterSpacing = 12f;
            label.color = fate;

            return row.gameObject.AddComponent<CanvasGroup>();
        }

        private static string Reason(PlayerData player) =>
            string.IsNullOrEmpty(player.resultReason) ? "" : char.ToUpper(player.resultReason[0]) + player.resultReason.Substring(1);
    }
}

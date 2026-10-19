using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using TDTS.Managers;

namespace TDTS.UI
{
    /// <summary>
    /// Listens for GameManager's Sundown state and shows each player's individual
    /// result (Survived / Lost) based on bunker seating. Never triggered manually —
    /// reacts to the state machine only.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        [Header("UI (assign in Inspector)")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI resultText;

        [Header("Scene Names")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState previous, GameState next)
        {
            if (next != GameState.Sundown) return;

            // Spec R1/R2: survivors first, each with the reason
            var lines = GameManager.Instance.Players
                .OrderByDescending(p => p.isSeated)
                .Select(p => p.isSeated
                    ? $"<color=#7BD88F><b>{p.playerName}: SURVIVED</b></color>  <size=70%>({p.resultReason})</size>"
                    : $"<color=#E06C6C>{p.playerName}: did not survive</color>  <size=70%>({p.resultReason})</size>");

            resultText.text = "<b>SUNDOWN</b>\n<size=70%>The bunker has 2 seats.</size>\n\n" + string.Join("\n", lines);
            gameOverPanel.SetActive(true);
        }

        public void OnBackToMenuPressed()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
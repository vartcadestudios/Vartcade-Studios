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

            var lines = GameManager.Instance.Players
                .Select(p => $"{p.playerName}: {(p.isSeated ? "Survived" : "Lost")}");

            resultText.text = string.Join("\n", lines);
            gameOverPanel.SetActive(true);
        }

        public void OnBackToMenuPressed()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
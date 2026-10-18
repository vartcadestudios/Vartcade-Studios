using UnityEngine;
using UnityEngine.SceneManagement;

namespace TDTS.Managers
{
    /// <summary>
    /// Handles pausing/resuming gameplay. Time.timeScale is the actual "pause" —
    /// the panel is just its visual reflection, not the source of truth.
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        public static PauseManager Instance { get; private set; }

        [Header("UI (assign in Inspector)")]
        [SerializeField] private GameObject pausePanel;

        [Header("Scene Names")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
            Time.timeScale = IsPaused ? 0f : 1f;
            if (pausePanel != null)
                pausePanel.SetActive(IsPaused);
        }

        public void OnResumePressed()
        {
            TogglePause();
        }

        public void OnQuitToMenuPressed()
        {
            Time.timeScale = 1f; // always restore before leaving the scene
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
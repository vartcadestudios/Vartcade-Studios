using UnityEngine;
using UnityEngine.SceneManagement;

namespace TDTS.UI
{
    /// <summary>
    /// Handles navigation actions from the Main Menu screen.
    /// Attach to an empty "MainMenuController" GameObject in the MainMenu scene.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string lobbySceneName = "Lobby";

        [Header("Panels (assign in Inspector)")]
        [SerializeField] private GameObject howToPlayPanel;
        [SerializeField] private GameObject settingsPanel;

        public void OnPlayPressed()
        {
            SceneManager.LoadScene(lobbySceneName);
        }

        public void OnHowToPlayPressed()
        {
            if (howToPlayPanel != null)
                howToPlayPanel.SetActive(true);
        }

        public void OnSettingsPressed()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(true);
        }

        public void ClosePanel(GameObject panel)
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }
}
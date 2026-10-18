using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Data;
using TDTS.Managers;

namespace TDTS.UI
{
    /// <summary>An opponent's zone on the board: shows name and card count, tap to target.</summary>
    public class PlayerZoneController : MonoBehaviour
    {
        [Tooltip("Position in GameManager's player list. 1 = Player 2, 2 = Player 3, 3 = Player 4.")]
        [SerializeField] private int playerIndex = 1;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Color selectedColor = new Color32(0xE8, 0x82, 0x3A, 255);

        private Image background;
        private Color normalColor;

        private void Start()
        {
            background = GetComponent<Image>();
            if (background != null) normalColor = background.color;

            Button button = GetComponent<Button>();
            if (button == null) button = gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OnClicked);

            if (GameManager.Instance == null) return;
            GameManager.Instance.OnHandsChanged += Refresh;
            GameManager.Instance.OnTargetChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnHandsChanged -= Refresh;
            GameManager.Instance.OnTargetChanged -= Refresh;
        }

        private PlayerData GetPlayer()
        {
            var players = GameManager.Instance.Players;
            return playerIndex >= 0 && playerIndex < players.Count ? players[playerIndex] : null;
        }

        private void OnClicked()
        {
            GameManager.Instance.SelectTarget(GetPlayer());
        }

        private void Refresh()
        {
            PlayerData p = GetPlayer();
            if (p == null)
            {
                label.text = "";
                return;
            }

            label.text = $"{p.playerName}\nCards: {p.hand.Count}";
            if (background != null)
                background.color = (GameManager.Instance.SelectedTarget == p) ? selectedColor : normalColor;
        }
    }
}
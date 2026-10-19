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
        [Tooltip("Seats after you in turn order: 1 = left, 2 = top, 3 = right.")]
        [SerializeField] private int seatsAfterYou = 1;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Color selectedColor = new Color32(0xE8, 0x82, 0x3A, 255);
        [SerializeField] private Color activeColor = new Color32(0x3C, 0x7A, 0x4E, 255);

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
            GameManager.Instance.OnTurnChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnTurnChanged -= Refresh;
            GameManager.Instance.OnHandsChanged -= Refresh;
            GameManager.Instance.OnTargetChanged -= Refresh;
        }

        private PlayerData GetPlayer() => GameManager.Instance.GetPlayerAfterLocal(seatsAfterYou);

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

            var players = GameManager.Instance.Players;
            int turn = 0;
            for (int i = 0; i < players.Count; i++)
                if (players[i] == p) turn = i + 1;

            bool active = GameManager.Instance.ActivePlayer == p;
            string status = active ? "<b>Taking their turn...</b>" : $"Turn {turn} of {players.Count}";
            label.text = $"{p.playerName}\nCards: {p.hand.Count}\n<size=70%>{status}</size>";

            // Your pick for Trade / Steal wins over the "their turn" colour
            if (background != null)
                background.color = GameManager.Instance.SelectedTarget == p ? selectedColor
                    : active ? activeColor
                    : normalColor;
        }
    }
}
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Managers;

namespace TDTS.UI
{
    /// <summary>Tap the draw pile to draw a card. Shows how many cards are left.</summary>
    public class DrawPileController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;

        private void Start()
        {
            Button button = GetComponent<Button>();
            if (button == null) button = gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OnClicked);

            if (GameView.Current == null) return;
            GameView.Current.OnDeckChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (GameView.Current != null)
                GameView.Current.OnDeckChanged -= Refresh;
        }

        private void OnClicked()
        {
            GameView.Current.DrawForLocalPlayer();
        }

        private void Refresh()
        {
            label.text = $"Draw\nPile\n{GameView.Current.DeckCount}";
        }
    }
}
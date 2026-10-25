using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Managers;

namespace TDTS.UI
{
    /// <summary>
    /// Tap the draw pile to draw a card (Scavenge; there's no button for it). Shows how many cards are
    /// left, and "Tap to draw" while drawing is your move.
    /// </summary>
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
            GameView.Current.OnTurnChanged += Refresh;
            GameView.Current.OnHandsChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (GameView.Current == null) return;
            GameView.Current.OnDeckChanged -= Refresh;
            GameView.Current.OnTurnChanged -= Refresh;
            GameView.Current.OnHandsChanged -= Refresh;
        }

        private void OnClicked()
        {
            GameView.Current.DrawForLocalPlayer();
        }

        private void Refresh()
        {
            IGameView view = GameView.Current;
            bool canDraw = view.IsLocalTurn && !view.HasPendingTrade && view.CurrentState == GameState.DayPhase
                && view.CurrentDayRule != DayRule.TradingOnly && view.DeckCount > 0;
            label.text = $"<b>{view.DeckCount}</b> <size=70%>left</size>" +
                (canDraw ? "\n<size=55%><color=#E07A2E><b>TAP TO DRAW</b></color></size>" : "");
        }
    }
}
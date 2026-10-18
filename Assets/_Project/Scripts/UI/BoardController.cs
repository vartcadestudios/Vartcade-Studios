using UnityEngine;
using TDTS.Managers;

namespace TDTS.UI
{
    /// <summary>
    /// Shows/hides board elements based on live GameManager state:
    /// Day buttons during DayPhase, Night buttons during NightPhase,
    /// and the Rondo wheel from its start day onward.
    /// </summary>
    public class BoardController : MonoBehaviour
    {
        [Header("Board Elements (assign in Inspector)")]
        [SerializeField] private GameObject nightButtons;  // Steal / Swap / Skip
        [SerializeField] private GameObject dayButtons;    // Trade / Scavenge / Play card
        [SerializeField] private GameObject rondoWheel;

        [Header("Rules")]
        [SerializeField] private int rondoStartDay = 6;

        private void Start()
        {
            if (GameManager.Instance == null) return;

            GameManager.Instance.OnStateChanged += HandleStateChanged;
            Refresh(); // GameManager may already have changed state before we subscribed
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState previous, GameState next)
        {
            Refresh();
        }

        private void Refresh()
        {
            GameState state = GameManager.Instance.CurrentState;

            dayButtons.SetActive(state == GameState.DayPhase);
            nightButtons.SetActive(state == GameState.NightPhase);
            rondoWheel.SetActive(GameManager.Instance.CurrentDay >= rondoStartDay);
        }
    }
}
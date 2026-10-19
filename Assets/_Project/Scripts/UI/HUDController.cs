using TMPro;
using UnityEngine;
using TDTS.Managers;

namespace TDTS.UI
{
    /// <summary>
    /// Reads live state from GameManager and reflects it in the HUD text elements.
    /// Attach to the "TopBar" GameObject (or a dedicated empty HUDController object).
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Bound Text Elements (assign in Inspector)")]
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private TextMeshProUGUI bunkerText;

        private void Update()
        {
            if (GameManager.Instance == null) return;

            dayText.text = $"Day {GameManager.Instance.CurrentDay} of {GameManager.Instance.SundownDay}";

            string phase = GameManager.Instance.CurrentState switch
            {
                GameState.DayPhase => "Day phase",
                GameState.NightPhase => "Night phase",
                GameState.Sundown => "Sundown",
                _ => "..."
            };

            var active = GameManager.Instance.ActivePlayer;
            if (active == null)
                phaseText.text = phase;
            else if (GameManager.Instance.IsLocalTurn)
                phaseText.text = $"{phase} - <b>Your turn</b> ({GameManager.Instance.ActionsLeft} AP)";
            else
                phaseText.text = $"{phase} - {active.playerName}'s turn";

            // Seats are only filled at Sundown (spec R1)
            bunkerText.text = GameManager.Instance.CurrentState == GameState.Sundown
                ? $"Bunker {GameManager.Instance.BunkerSeated}/{GameManager.Instance.BunkerCapacity} seated"
                : $"Bunker: {GameManager.Instance.BunkerCapacity} seats";
        }
    }
}
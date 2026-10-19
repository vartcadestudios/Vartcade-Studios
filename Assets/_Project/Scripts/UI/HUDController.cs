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
            if (GameView.Current == null) return;

            dayText.text = $"Day {GameView.Current.CurrentDay} of {GameView.Current.SundownDay}";

            string phase = GameView.Current.CurrentState switch
            {
                GameState.DayPhase => "Day phase",
                GameState.NightPhase => "Night phase",
                GameState.Sundown => "Sundown",
                _ => "..."
            };

            var active = GameView.Current.ActivePlayer;
            if (active == null)
                phaseText.text = phase;
            else if (GameView.Current.IsLocalTurn)
                phaseText.text = $"{phase} - <b>Your turn</b> ({GameView.Current.ActionsLeft} AP)";
            else
                phaseText.text = $"{phase} - {active.playerName}'s turn";

            // Seats are only filled at Sundown (spec R1)
            bunkerText.text = GameView.Current.CurrentState == GameState.Sundown
                ? $"Bunker {GameView.Current.BunkerSeated}/{GameView.Current.BunkerCapacity} seated"
                : $"Bunker: {GameView.Current.BunkerCapacity} seats";
        }
    }
}
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

            dayText.text = $"Day {GameManager.Instance.CurrentDay}";

            phaseText.text = GameManager.Instance.CurrentState switch
            {
                GameState.DayPhase => "Day phase",
                GameState.NightPhase => "Night phase",
                GameState.Sundown => "Sundown",
                _ => "..."
            };

            bunkerText.text = $"Bunker {GameManager.Instance.BunkerSeated}/{GameManager.Instance.BunkerCapacity} seated";
        }
    }
}
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Managers;

namespace TDTS.UI
{
    /// <summary>
    /// Wires the Day and Night action buttons to GameManager for the local player and
    /// greys them out when it isn't your turn. Adds a Pass button next to the Day buttons.
    /// </summary>
    public class ActionBarController : MonoBehaviour
    {
        [Header("Day buttons (assign in Inspector)")]
        [SerializeField] private Button tradeButton;
        [SerializeField] private Button scavengeButton;
        [SerializeField] private Button playCardButton;

        [Header("Night buttons (assign in Inspector)")]
        [SerializeField] private Button stealButton;
        [SerializeField] private Button swapButton;
        [SerializeField] private Button skipButton;

        private Button passButton;

        private void Start()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            tradeButton.onClick.AddListener(gm.TradeSelectedForLocalPlayer);
            scavengeButton.onClick.AddListener(gm.DrawForLocalPlayer);
            playCardButton.onClick.AddListener(gm.PlaySelectedForLocalPlayer);
            stealButton.onClick.AddListener(gm.StealForLocalPlayer);
            skipButton.onClick.AddListener(gm.SkipForLocalPlayer);

            // P5 Pass: a copy of the Scavenge button so it matches the row's style
            passButton = Instantiate(scavengeButton, scavengeButton.transform.parent);
            passButton.name = "PassButton";
            passButton.onClick = new Button.ButtonClickedEvent();
            passButton.onClick.AddListener(gm.PassForLocalPlayer);
            TextMeshProUGUI label = passButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = "Pass";
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            bool myTurn = gm.IsLocalTurn && gm.PendingTrade == null;
            bool day = myTurn && gm.CurrentState == GameState.DayPhase;
            bool night = myTurn && gm.CurrentState == GameState.NightPhase;

            tradeButton.interactable = day;
            scavengeButton.interactable = day;
            playCardButton.interactable = day;
            if (passButton != null) passButton.interactable = day;

            stealButton.interactable = night;
            skipButton.interactable = night;
            swapButton.interactable = false; // Swap arrives with the Night phase in Phase 4
        }
    }
}

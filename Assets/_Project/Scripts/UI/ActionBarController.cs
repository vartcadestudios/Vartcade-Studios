using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Managers;

namespace TDTS.UI
{
    /// <summary>
    /// Wires the Day and Night action buttons to GameManager for the local player and
    /// greys them out when it isn't your turn. Adds the buttons the scene doesn't have yet
    /// (Pass, Accept/Decline trade, Night Play card) as copies of existing ones.
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
        private Button acceptButton;
        private Button declineButton;
        private Button nightPlayButton;
        private TextMeshProUGUI declineLabel;

        private void Start()
        {
            // Style first, so the copies made below come out matching
            foreach (Button button in new[] { tradeButton, scavengeButton, playCardButton, stealButton, swapButton })
                Style(button, SundownArt.ButtonColor);
            Style(skipButton, SundownArt.AccentColor); // ending your Night turn is the main move

            IGameView gm = GameView.Current;
            if (gm == null) return;

            tradeButton.onClick.AddListener(gm.TradeSelectedForLocalPlayer);
            scavengeButton.onClick.AddListener(gm.DrawForLocalPlayer);
            playCardButton.onClick.AddListener(gm.PlaySelectedForLocalPlayer);
            stealButton.onClick.AddListener(gm.StealForLocalPlayer);
            swapButton.onClick.AddListener(gm.SwapSelectedForLocalPlayer);

            // P11: at Night the old Skip button ends your turn
            skipButton.onClick.AddListener(gm.PassForLocalPlayer);
            SetLabel(skipButton, "End turn");

            passButton = CopyOf(scavengeButton, "Pass", gm.PassForLocalPlayer);
            nightPlayButton = CopyOf(stealButton, "Play card", gm.PlaySelectedForLocalPlayer);
            acceptButton = CopyOf(scavengeButton, "Accept trade", () => gm.AnswerTradeForLocalPlayer(true));
            Style(acceptButton, SundownArt.AccentColor);
            declineButton = CopyOf(scavengeButton, "Decline", () => gm.AnswerTradeForLocalPlayer(false));
            declineLabel = declineButton.GetComponentInChildren<TextMeshProUGUI>();

            // The added buttons don't fit at their scene width, so let each row share its width out
            FitToRow(scavengeButton);
            FitToRow(stealButton);
        }

        private void Update()
        {
            IGameView gm = GameView.Current;
            if (gm == null) return;

            bool offeredToMe = gm.HasPendingTrade && gm.TradeTo == gm.LocalPlayer;
            bool myTurn = gm.IsLocalTurn && !gm.HasPendingTrade;
            bool day = myTurn && gm.CurrentState == GameState.DayPhase;
            bool night = myTurn && gm.CurrentState == GameState.NightPhase;
            bool tradingOnly = gm.CurrentDayRule == DayRule.TradingOnly;

            // A trade offered to you replaces the Day buttons until you answer
            tradeButton.gameObject.SetActive(!offeredToMe);
            scavengeButton.gameObject.SetActive(!offeredToMe);
            playCardButton.gameObject.SetActive(!offeredToMe);
            if (passButton != null) passButton.gameObject.SetActive(!offeredToMe);
            if (acceptButton != null) acceptButton.gameObject.SetActive(offeredToMe);
            if (declineButton != null)
            {
                declineButton.gameObject.SetActive(offeredToMe);
                if (offeredToMe) declineLabel.text = $"Decline ({Mathf.CeilToInt(gm.TradeSecondsLeft)}s)";
            }

            tradeButton.interactable = day;
            scavengeButton.interactable = day && !tradingOnly;
            playCardButton.interactable = day && !tradingOnly;
            if (passButton != null) passButton.interactable = day;

            stealButton.interactable = night;
            swapButton.interactable = night;
            skipButton.interactable = night && gm.PeeksThisTurn > 0;
            if (nightPlayButton != null) nightPlayButton.interactable = night;
        }

        /// <summary>A copy of an existing button, so new buttons match the row's style.</summary>
        private static Button CopyOf(Button original, string text, UnityEngine.Events.UnityAction onClick)
        {
            Button copy = Instantiate(original, original.transform.parent);
            copy.name = text.Replace(" ", "") + "Button";
            copy.onClick = new Button.ButtonClickedEvent();
            copy.onClick.AddListener(onClick);
            SetLabel(copy, text);
            return copy;
        }

        /// <summary>Rounded, tinted button with light text, greyed out when it isn't available.</summary>
        private static void Style(Button button, Color color)
        {
            Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image == null) return;
            SundownArt.Rounded(image, Color.white, 14);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = SundownArt.Tints(color);

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null) return;
            label.color = SundownArt.TextColor;
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 26f;
        }

        private static void FitToRow(Button anyButtonInRow)
        {
            var row = anyButtonInRow.transform.parent.GetComponent<HorizontalLayoutGroup>();
            if (row == null) return;
            row.childControlWidth = true;
            row.childForceExpandWidth = true;
        }

        private static void SetLabel(Button button, string text)
        {
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = text;
        }
    }
}

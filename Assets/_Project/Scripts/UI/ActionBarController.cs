using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Managers;

namespace TDTS.UI
{
    /// <summary>
    /// Wires the Day and Night action buttons to GameManager for the local player and
    /// greys them out when it isn't your turn. Adds the buttons the scene doesn't have yet
    /// (Accept/Decline trade, Peek, Night Play card) as copies of existing ones.
    /// There is no Pass. End turn stays hidden while actions are limited: every turn uses all its actions.
    /// Scavenge is hidden too: you draw by tapping the draw pile.
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

        private Button acceptButton;
        private Button declineButton;
        private Button nightPlayButton;
        private Button peekButton;
        private TextMeshProUGUI declineLabel, stealLabel, swapLabel;

        private void Start()
        {
            // Style first, so the copies made below come out matching
            // Wireframe 03-12 / 04-5: Day buttons brick with Play card in gold, Night buttons dark teal
            foreach (Button button in new[] { tradeButton, scavengeButton })
                Style(button, UITheme.Brick);
            Style(playCardButton, UITheme.Gold);
            foreach (Button button in new[] { stealButton, swapButton })
                Style(button, UITheme.TealDark);
            Style(skipButton, UITheme.Orange);

            IGameView gm = GameView.Current;
            if (gm == null) return;

            Transform canvas = stealButton.GetComponentInParent<Canvas>().rootCanvas.transform;
            tradeButton.onClick.AddListener(() => CardPicker.OpenTrade(canvas, gm));
            scavengeButton.onClick.AddListener(gm.DrawForLocalPlayer);
            playCardButton.onClick.AddListener(gm.PlaySelectedForLocalPlayer);
            stealButton.onClick.AddListener(() => CardPicker.OpenSteal(canvas, gm));
            swapButton.onClick.AddListener(gm.SwapSelectedForLocalPlayer);

            // P11: at Night the old Skip button ends your turn
            skipButton.onClick.AddListener(gm.PassForLocalPlayer);
            SetLabel(skipButton, "End turn");

            nightPlayButton = CopyOf(stealButton, "Play card", gm.PlaySelectedForLocalPlayer);
            // Tapping a card at Night only picks it; this looks at the back of the picked card (1 action)
            peekButton = CopyOf(stealButton, "Peek", () => gm.PeekForLocalPlayer(gm.SelectedCardIndex));
            Style(peekButton, UITheme.Gold);
            peekButton.transform.SetSiblingIndex(stealButton.transform.GetSiblingIndex()); // first in the row
            acceptButton = CopyOf(scavengeButton, "Accept trade", () => gm.AnswerTradeForLocalPlayer(true));
            Style(acceptButton, UITheme.Gold);
            declineButton = CopyOf(scavengeButton, "Decline", () => gm.AnswerTradeForLocalPlayer(false));
            Style(declineButton, UITheme.Brick);
            declineLabel = declineButton.GetComponentInChildren<TextMeshProUGUI>();
            stealLabel = stealButton.GetComponentInChildren<TextMeshProUGUI>();
            swapLabel = swapButton.GetComponentInChildren<TextMeshProUGUI>();

            // The added buttons don't fit at their scene width, so let each row share its width out
            FitToRow(scavengeButton);
            FitToRow(stealButton);

            // Flashlight results pop up on this device (offline, or the host's own seat).
            // Online players get the same result as a line in their log.
            flashlightCanvas = canvas;
            if (GameManager.Instance != null) GameManager.Instance.OnLocalFlashlight += ShowFlashlight;
        }

        private Transform flashlightCanvas;

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnLocalFlashlight -= ShowFlashlight;
        }

        private void ShowFlashlight(TDTS.Data.PlayerData target, System.Collections.Generic.List<TDTS.Data.CardData> lit) =>
            CardPicker.OpenFlashlight(flashlightCanvas, target, lit, target == GameManager.Instance.LocalPlayer);

        private void Update()
        {
            IGameView gm = GameView.Current;
            if (gm == null) return;

            bool offeredToMe = gm.HasPendingTrade && gm.TradeTo == gm.LocalPlayer;
            bool myTurn = gm.IsLocalTurn && !gm.HasPendingTrade;
            bool day = myTurn && gm.CurrentState == GameState.DayPhase;
            bool night = myTurn && gm.CurrentState == GameState.NightPhase;
            bool tradingOnly = gm.CurrentDayRule == DayRule.TradingOnly;
            // No End turn: you use all your actions. It only shows when actions
            // aren't limited (a testing switch), since then a turn would never end on its own.
            bool canEndTurn = !gm.LimitActions;

            // A trade offered to you replaces the Day buttons until you answer.
            // Wireframe 03-12: Scavenge is a button again (tapping the draw pile still works too).
            tradeButton.gameObject.SetActive(!offeredToMe);
            scavengeButton.gameObject.SetActive(!offeredToMe);
            playCardButton.gameObject.SetActive(!offeredToMe);
            skipButton.gameObject.SetActive(canEndTurn);
            if (acceptButton != null) acceptButton.gameObject.SetActive(offeredToMe);
            if (declineButton != null)
            {
                declineButton.gameObject.SetActive(offeredToMe);
                if (offeredToMe) declineLabel.text = $"Decline ({Mathf.CeilToInt(gm.TradeSecondsLeft)}s)";
            }

            tradeButton.interactable = day;
            scavengeButton.interactable = day && !tradingOnly && gm.DeckCount > 0;
            playCardButton.interactable = day && !tradingOnly && !PickedCardIsUnplayable(gm, true);

            // Each button says what it does underneath its name (wireframe 03-12)
            Hint(tradeButton, "Trade", "Pick a card + player");
            Hint(scavengeButton, "Scavenge", gm.DeckCount > 0 ? "Draw 1 card" : "The pile is empty");
            Hint(playCardButton, "Play card", PickedMorningName(gm) ?? "Pick an unused Morning card");
            if (nightPlayButton != null) Hint(nightPlayButton, "Play card", "A peeked Night card");
            if (peekButton != null) Hint(peekButton, "Peek", gm.PeeksThisTurn == 0 ? "Required: look at a back" : "Look at another back");

            // One Steal or Swap a night
            stealButton.interactable = night && !gm.StoleOrSwappedThisTurn;
            swapButton.interactable = night && !gm.StoleOrSwappedThisTurn;
            // Disabled buttons say why (wireframe 04-5)
            bool usedUp = night && gm.StoleOrSwappedThisTurn;
            Hint(stealButton, "Steal", usedUp ? "One per Night" : "Pick one of their cards");
            Hint(swapButton, "Swap", usedUp ? "One per Night" : "Yours for a random one of theirs");
            skipButton.interactable = night && gm.PeeksThisTurn > 0;
            if (nightPlayButton != null) nightPlayButton.interactable = night && !PickedCardIsUnplayable(gm, false);
            if (peekButton != null) peekButton.interactable = night && !PickedCardIsPeeked(gm);
        }

        private static void Hint(Button button, string name, string hint)
        {
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null) return;
            string text = $"{name}\n<size=46%>{hint}</size>";
            if (label.text != text) label.text = text;
        }

        /// <summary>The name of the picked card when it's a Morning card you can still play.</summary>
        private static string PickedMorningName(IGameView gm)
        {
            var hand = gm.LocalPlayer?.hand;
            int index = gm.SelectedCardIndex;
            if (hand == null || index < 0 || index >= hand.Count) return null;
            var card = hand[index];
            return card.HasMorningFront && !card.frontUsed ? card.FrontName : null;
        }

        /// <summary>You already know the back of the card you picked (tap it at Night to see it again).</summary>
        private static bool PickedCardIsPeeked(IGameView gm)
        {
            var hand = gm.LocalPlayer?.hand;
            int index = gm.SelectedCardIndex;
            return hand != null && index >= 0 && index < hand.Count && hand[index].peeked;
        }

        /// <summary>
        /// The card you picked can't be played now (a Supply by Day, an unpeeked or non-Night card at Night).
        /// With nothing picked the button stays live, so pressing it tells you to pick a card.
        /// </summary>
        private static bool PickedCardIsUnplayable(IGameView gm, bool day)
        {
            var hand = gm.LocalPlayer?.hand;
            int index = gm.SelectedCardIndex;
            if (hand == null || index < 0 || index >= hand.Count) return false;
            var card = hand[index];
            return day ? !card.HasMorningFront || card.frontUsed : !(card.HasNightBack && card.peeked);
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
            SundownArt.Plate(image, Color.white, 14); // bevelled plate like the card frames
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = SundownArt.Tints(color);

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null) return;
            // Charcoal text on the light gold and orange buttons, cream on the dark ones
            label.color = color.grayscale > 0.5f ? UITheme.Charcoal : UITheme.Cream;
            SundownFonts.UseTitle(label); // Anton, like the wireframe's buttons
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 34f;
            label.lineSpacing = -20f;

            // The scene's labels were placed by hand; centre every one in its button
            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 0f);
            rect.offsetMax = new Vector2(-8f, 0f);
            label.alignment = TextAlignmentOptions.Center;
            label.margin = Vector4.zero;
            label.text = label.text.Trim(); // some scene labels end in blank lines, which push the text up
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

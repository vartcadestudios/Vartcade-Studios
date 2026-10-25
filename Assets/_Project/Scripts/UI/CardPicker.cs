using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Data;
using TDTS.Managers;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// Pops up the cards of another player, face up (fronts are public):
    /// - Steal (P7): tap the card you take.
    /// - Trade (P1): tap the card you want in return for the one you picked. They can still refuse.
    /// - Close-up: tap their cards on the table to look at them big. Pick one, then Trade (Day)
    ///   or Steal / Swap (Night) right from here, or just Close.
    /// Closes itself if the turn, phase or hands move on underneath it.
    /// </summary>
    public class CardPicker : MonoBehaviour
    {
        private const float CardWidth = 110f, CardHeight = 154f, BigWidth = 150f, BigHeight = 210f;
        private const float Gap = 20f, MaxRowWidth = 1560f;
        private static readonly Color Chosen = new Color32(0xFF, 0xD5, 0x4A, 255);

        private IGameView view;
        private PlayerData target;
        private string openedOn;

        // Close-up only
        private readonly List<RectTransform> cards = new List<RectTransform>();
        private int chosen = -1;
        private Button tradeButton, stealButton, swapButton;
        private TextMeshProUGUI hint;

        /// <summary>
        /// Opens the picker for the player you've tapped. Without a usable target it goes straight
        /// to the rules, which explain what's missing (no target, no cards).
        /// </summary>
        public static void OpenSteal(Transform canvas, IGameView view)
        {
            PlayerData target = view.SelectedTarget;
            if (target == null || view.HandCount(target) == 0)
            {
                view.StealForLocalPlayer(-1);
                return;
            }
            Overlay(canvas).BuildPick(view, target, $"Steal from {target.playerName}",
                "Pick the card you want to take. Peek at it to learn its back.", view.StealForLocalPlayer);
        }

        /// <summary>
        /// Opens the picker for a trade: your picked card for one of theirs. Without a picked card or
        /// a target with cards it goes straight to the rules, which explain what's missing.
        /// </summary>
        public static void OpenTrade(Transform canvas, IGameView view)
        {
            PlayerData target = view.SelectedTarget;
            var hand = view.LocalPlayer?.hand;
            int mine = view.SelectedCardIndex;
            if (target == null || view.HandCount(target) == 0 || hand == null || mine < 0 || mine >= hand.Count)
            {
                view.TradeSelectedForLocalPlayer(-1);
                return;
            }
            Overlay(canvas).BuildPick(view, target, $"Trade your {hand[mine].FrontName}",
                $"Pick the card you want from {target.playerName}. They can accept or refuse.", view.TradeSelectedForLocalPlayer);
        }

        /// <summary>A big look at another player's cards, with the moves you can make on them right now.</summary>
        public static void OpenCloseUp(Transform canvas, IGameView view, PlayerData target)
        {
            if (target == null || view.HandCount(target) == 0) return;
            if (view.SelectedTarget != target) view.SelectTarget(target); // your moves from here aim at them
            Overlay(canvas).BuildCloseUp(view, target);
        }

        private static CardPicker Overlay(Transform canvas)
        {
            var overlay = NewRect("CardPicker", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.SetAsLastSibling();
            overlay.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f); // blocks taps behind it
            return overlay.gameObject.AddComponent<CardPicker>();
        }

        // ---------- Pick a card (Steal, Trade) ----------

        private void BuildPick(IGameView gameView, PlayerData who, string titleText, string hintText, System.Action<int> onPick)
        {
            Listen(gameView, who);
            Vector2 size = new Vector2(CardWidth, CardHeight);
            RectTransform panel = Panel(who.hand.Count, size, 460f, titleText, hintText);
            RectTransform row = Row(panel, who.hand, size);
            for (int i = 0; i < row.childCount; i++)
            {
                int cardIndex = i; // capture a copy for the lambda
                Tappable(row.GetChild(i)).onClick.AddListener(() =>
                {
                    Close();
                    onPick(cardIndex);
                });
            }
            Bottom(panel, new[] { MakeButtonIn(panel, "Cancel", ButtonColor, Close) });
        }

        // ---------- Close-up ----------

        private void BuildCloseUp(IGameView gameView, PlayerData who)
        {
            Listen(gameView, who);
            Vector2 size = new Vector2(BigWidth, BigHeight);
            RectTransform panel = Panel(who.hand.Count, size, 540f, $"{who.playerName}'s cards", "");
            hint = panel.Find("Hint").GetComponent<TextMeshProUGUI>();

            RectTransform row = Row(panel, who.hand, size);
            for (int i = 0; i < row.childCount; i++)
            {
                int cardIndex = i;
                cards.Add((RectTransform)row.GetChild(i));
                Tappable(row.GetChild(i)).onClick.AddListener(() => Choose(cardIndex));
            }

            var buttons = new List<Button>();
            if (view.IsLocalTurn && !view.HasPendingTrade && view.CurrentState == GameState.DayPhase)
            {
                tradeButton = MakeButtonIn(panel, "Trade for it", AccentColor, () => Act((v, card) => v.TradeSelectedForLocalPlayer(card)));
                buttons.Add(tradeButton);
            }
            if (view.IsLocalTurn && !view.HasPendingTrade && view.CurrentState == GameState.NightPhase)
            {
                stealButton = MakeButtonIn(panel, "Steal it", AccentColor, () => Act((v, card) => v.StealForLocalPlayer(card)));
                swapButton = MakeButtonIn(panel, "Swap", ButtonColor, () => Act((v, card) => v.SwapSelectedForLocalPlayer()));
                buttons.Add(stealButton);
                buttons.Add(swapButton);
            }
            buttons.Add(MakeButtonIn(panel, "Close", ButtonColor, Close));
            Bottom(panel, buttons.ToArray());
            Refresh();
        }

        private void Choose(int cardIndex)
        {
            chosen = chosen == cardIndex ? -1 : cardIndex;
            Refresh();
        }

        /// <summary>Closes, then makes the move with the chosen card. The rules explain anything that's missing.</summary>
        private void Act(System.Action<IGameView, int> move)
        {
            IGameView v = view;
            int card = chosen;
            Close();
            move(v, card);
        }

        /// <summary>Lifts the chosen card, and says what you can do with it.</summary>
        private void Refresh()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                bool picked = i == chosen;
                cards[i].localScale = picked ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
                Outline edge = cards[i].GetComponent<Outline>();
                edge.effectColor = picked ? Chosen : new Color(0f, 0f, 0f, 0.45f);
                edge.effectDistance = picked ? new Vector2(4f, -4f) : new Vector2(1.5f, -1.5f);
                if (picked) cards[i].SetAsLastSibling();
            }

            var hand = view.LocalPlayer?.hand;
            int mine = view.SelectedCardIndex;
            CardData myCard = hand != null && mine >= 0 && mine < hand.Count ? hand[mine] : null;
            string theirs = chosen >= 0 ? target.hand[chosen].FrontName : null;
            bool myTurn = view.IsLocalTurn && !view.HasPendingTrade;

            if (tradeButton != null) tradeButton.interactable = theirs != null && myCard != null;
            if (stealButton != null) stealButton.interactable = theirs != null && !view.StoleOrSwappedThisTurn;
            if (swapButton != null) swapButton.interactable = myCard != null && !view.StoleOrSwappedThisTurn;

            hint.text = !myTurn ? "Only the fronts are on show. Backs stay secret."
                : view.CurrentState == GameState.DayPhase
                    ? (myCard == null ? "Pick one of your own cards first, then come back to trade it."
                        : theirs == null ? $"Tap the card you want for your {myCard.FrontName}."
                        : $"Offer your {myCard.FrontName} for their {theirs}? They can refuse.")
                : view.StoleOrSwappedThisTurn ? "You've already stolen or swapped tonight."
                : theirs == null ? "Tap a card to steal it. Swap gives one of yours for a random card of theirs."
                : $"Steal their {theirs}?" + (myCard != null ? $" Or swap your {myCard.FrontName} for a random card." : "");
        }

        // ---------- Building ----------

        private void Listen(IGameView gameView, PlayerData who)
        {
            view = gameView;
            target = who;
            openedOn = Situation();
            view.OnTurnChanged += CloseIfMovedOn;
            view.OnStateChanged += HandleStateChanged;
            view.OnHandsChanged += CloseIfMovedOn;
        }

        /// <summary>
        /// Whose turn it is and the cards involved. Online, every update from the host fires these
        /// events, so only close when something here actually changed.
        /// </summary>
        private string Situation()
        {
            var sb = new System.Text.StringBuilder($"{view.TurnNumber}|{view.CurrentState}|{view.HasPendingTrade}|");
            foreach (CardData c in target.hand) sb.Append(c.FrontName).Append(',');
            sb.Append('|').Append(view.LocalPlayer?.hand.Count ?? 0);
            return sb.ToString();
        }

        private void CloseIfMovedOn()
        {
            if (view != null && Situation() != openedOn) Close();
        }

        /// <summary>The panel, wide enough for the row of cards, with a title and a hint line.</summary>
        private RectTransform Panel(int count, Vector2 size, float height, string titleText, string hintText)
        {
            float width = Mathf.Max(720f, RowWidth(count, size) + 120f);
            var panel = NewPanel("Panel", transform, new Vector2(-width / 2f, -height / 2f), new Vector2(width / 2f, height / 2f));

            var title = NewText("Title", panel, 40, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), Vector2.one, new Vector2(24f, -92f), new Vector2(-24f, -30f));
            title.text = titleText;
            title.color = TextColor;

            var line = NewText("Hint", panel, 22, FontStyles.Normal, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), Vector2.one, new Vector2(24f, -132f), new Vector2(-24f, -92f));
            line.text = hintText;
            line.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.7f);
            return panel;
        }

        /// <summary>Their cards face up in a row. There's no hand limit, so a big hand overlaps to stay on screen.</summary>
        private static RectTransform Row(RectTransform panel, List<CardData> hand, Vector2 size)
        {
            int count = hand.Count;
            float step = Step(count, size);
            float rowWidth = RowWidth(count, size);
            var row = NewRect("Cards", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            row.anchoredPosition = new Vector2(0f, 6f);
            for (int i = 0; i < count; i++)
            {
                RectTransform card = CardView.Front(row, size, hand[i]);
                card.anchoredPosition = new Vector2(-rowWidth / 2f + size.x / 2f + i * step, 0f);
            }
            return row;
        }

        private static float Step(int count, Vector2 size) =>
            count > 1 ? Mathf.Min(size.x + Gap, (MaxRowWidth - size.x) / (count - 1)) : 0f;

        private static float RowWidth(int count, Vector2 size) => size.x + Mathf.Max(0, count - 1) * Step(count, size);

        private static Button Tappable(Transform card)
        {
            Image face = card.GetComponent<Image>();
            face.raycastTarget = true;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.colors = Tints(Color.white); // brightens on hover
            return button;
        }

        private static Button MakeButtonIn(RectTransform panel, string text, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var slot = NewRect(text, panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            return MakeButton(slot, text, color, onClick, 24);
        }

        /// <summary>Lines the buttons up, centred along the bottom of the panel.</summary>
        private static void Bottom(RectTransform panel, Button[] buttons)
        {
            const float width = 220f, gap = 20f;
            float total = buttons.Length * width + (buttons.Length - 1) * gap;
            for (int i = 0; i < buttons.Length; i++)
            {
                var slot = (RectTransform)buttons[i].transform.parent;
                float left = -total / 2f + i * (width + gap);
                slot.offsetMin = new Vector2(left, 30f);
                slot.offsetMax = new Vector2(left + width, 90f);
            }
        }

        private void HandleStateChanged(GameState previous, GameState next) => Close();

        private void Close()
        {
            if (view != null)
            {
                view.OnTurnChanged -= CloseIfMovedOn;
                view.OnStateChanged -= HandleStateChanged;
                view.OnHandsChanged -= CloseIfMovedOn;
                view = null;
            }
            if (this != null) Destroy(gameObject);
        }

        private void OnDestroy() => Close();
    }
}

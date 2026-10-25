using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Data;
using TDTS.Managers;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// Pops up the cards of the player you've tapped, face up (fronts are public), so you can pick one:
    /// - Steal (P7): the card you take.
    /// - Trade (P1): the card you want in return for the one you picked. They can still refuse.
    /// Closes itself if the turn, phase or hands move on underneath it.
    /// </summary>
    public class CardPicker : MonoBehaviour
    {
        private const float CardWidth = 110f, CardHeight = 154f, Gap = 20f, MaxRowWidth = 1560f;

        private IGameView view;

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
            Open(canvas, view, target, $"Steal from {target.playerName}",
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
            Open(canvas, view, target, $"Trade your {hand[mine].FrontName}",
                $"Pick the card you want from {target.playerName}. They can accept or refuse.", view.TradeSelectedForLocalPlayer);
        }

        private static void Open(Transform canvas, IGameView view, PlayerData target, string title, string hint, System.Action<int> onPick)
        {
            var overlay = NewRect("CardPicker", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.SetAsLastSibling();
            overlay.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f); // blocks taps behind it
            overlay.gameObject.AddComponent<CardPicker>().Build(view, target, title, hint, onPick);
        }

        private void Build(IGameView gameView, PlayerData target, string titleText, string hintText, System.Action<int> onPick)
        {
            view = gameView;
            view.OnTurnChanged += Close;
            view.OnStateChanged += HandleStateChanged;
            view.OnHandsChanged += Close;

            int count = target.hand.Count;
            // There's no hand limit, so a big hand overlaps its cards to stay on screen
            float step = count > 1 ? Mathf.Min(CardWidth + Gap, (MaxRowWidth - CardWidth) / (count - 1)) : 0f;
            float rowWidth = CardWidth + (count - 1) * step;
            float width = Mathf.Max(620f, rowWidth + 120f);
            var panel = NewPanel("Panel", transform, new Vector2(-width / 2f, -230f), new Vector2(width / 2f, 230f));

            var title = NewText("Title", panel, 40, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), Vector2.one, new Vector2(24f, -92f), new Vector2(-24f, -30f));
            title.text = titleText;
            title.color = TextColor;

            var hint = NewText("Hint", panel, 22, FontStyles.Normal, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), Vector2.one, new Vector2(24f, -132f), new Vector2(-24f, -92f));
            hint.text = hintText;
            hint.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.7f);

            var row = NewRect("Cards", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            row.anchoredPosition = new Vector2(0f, 6f);
            for (int i = 0; i < count; i++)
            {
                int cardIndex = i; // capture a copy for the lambda
                RectTransform card = CardView.Front(row, new Vector2(CardWidth, CardHeight), target.hand[i]);
                card.anchoredPosition = new Vector2(-rowWidth / 2f + CardWidth / 2f + i * step, 0f);

                Image face = card.GetComponent<Image>();
                face.raycastTarget = true;
                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = face;
                button.colors = Tints(Color.white); // brightens on hover
                button.onClick.AddListener(() => Pick(cardIndex, onPick));
            }

            var cancel = NewRect("Cancel", panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-120f, 30f), new Vector2(120f, 90f));
            MakeButton(cancel, "Cancel", ButtonColor, Close, 24);
        }

        private void Pick(int cardIndex, System.Action<int> onPick)
        {
            Close();
            onPick(cardIndex);
        }

        private void HandleStateChanged(GameState previous, GameState next) => Close();

        private void Close()
        {
            if (view != null)
            {
                view.OnTurnChanged -= Close;
                view.OnStateChanged -= HandleStateChanged;
                view.OnHandsChanged -= Close;
                view = null;
            }
            if (this != null) Destroy(gameObject);
        }

        private void OnDestroy() => Close();
    }
}

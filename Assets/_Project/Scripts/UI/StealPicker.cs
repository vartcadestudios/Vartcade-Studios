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
    /// Steal (P7): you choose which of the target's face-down cards to take. Pops up their cards
    /// in a shuffled row, so where a card sits in their hand gives nothing away.
    /// Closes itself if the turn or phase moves on underneath it.
    /// </summary>
    public class StealPicker : MonoBehaviour
    {
        private const float CardWidth = 110f, CardHeight = 154f, Gap = 20f;

        private IGameView view;

        /// <summary>
        /// Opens the picker for the player you've tapped. Without a usable target it goes straight
        /// to the rules, which explain what's missing (no target, no cards, hand full).
        /// </summary>
        public static void Open(Transform canvas, IGameView view)
        {
            PlayerData target = view.SelectedTarget;
            if (target == null || view.HandCount(target) == 0 || view.LocalPlayer.hand.Count >= GameManager.MaxHandSize)
            {
                view.StealForLocalPlayer(-1);
                return;
            }

            var overlay = NewRect("StealPicker", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.SetAsLastSibling();
            overlay.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f); // blocks taps behind it
            overlay.gameObject.AddComponent<StealPicker>().Build(view, target);
        }

        private void Build(IGameView gameView, PlayerData target)
        {
            view = gameView;
            view.OnTurnChanged += Close;
            view.OnStateChanged += HandleStateChanged;
            view.OnHandsChanged += Close;

            int count = view.HandCount(target);
            float rowWidth = count * CardWidth + (count - 1) * Gap;
            float width = Mathf.Max(620f, rowWidth + 120f);
            var panel = NewPanel("Panel", transform, new Vector2(-width / 2f, -230f), new Vector2(width / 2f, 230f));

            var title = NewText("Title", panel, 40, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), Vector2.one, new Vector2(24f, -92f), new Vector2(-24f, -30f));
            title.text = $"Steal from {target.playerName}";
            title.color = TextColor;

            var hint = NewText("Hint", panel, 22, FontStyles.Normal, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), Vector2.one, new Vector2(24f, -132f), new Vector2(-24f, -92f));
            hint.text = "Pick a card. You'll see what it is once it's yours.";
            hint.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.7f);

            // Shuffle the display order so a card's spot in their hand gives nothing away
            var order = new List<int>();
            for (int i = 0; i < count; i++) order.Add(i);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            var row = NewRect("Cards", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            row.anchoredPosition = new Vector2(0f, 6f);
            for (int i = 0; i < count; i++)
            {
                int cardIndex = order[i];
                RectTransform card = CardBack(row, new Vector2(CardWidth, CardHeight));
                card.anchoredPosition = new Vector2(-rowWidth / 2f + CardWidth / 2f + i * (CardWidth + Gap), 0f);

                Image frame = card.GetComponent<Image>();
                frame.raycastTarget = true;
                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = frame;
                button.colors = Tints(Color.white); // the rust frame brightens on hover
                button.onClick.AddListener(() => Pick(cardIndex));
            }

            var cancel = NewRect("Cancel", panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-120f, 30f), new Vector2(120f, 90f));
            MakeButton(cancel, "Cancel", ButtonColor, Close, 24);
        }

        private void Pick(int victimCardIndex)
        {
            IGameView v = view;
            Close();
            v.StealForLocalPlayer(victimCardIndex);
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

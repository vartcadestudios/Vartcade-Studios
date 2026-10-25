using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Data;
using TDTS.Managers;

namespace TDTS.UI
{
    /// <summary>
    /// An opponent's zone on the board: name, card count and the fronts of their cards, which
    /// everyone can always see (only backs are secret). Tap anywhere on the panel to target them
    /// and open a close-up of their cards you can Trade, Steal or Swap from.
    /// </summary>
    public class PlayerZoneController : MonoBehaviour
    {
        [Tooltip("Seats after you in turn order: 1 = left, 2 = top, 3 = right.")]
        [SerializeField] private int seatsAfterYou = 1;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Color selectedColor = new Color32(0xE8, 0x82, 0x3A, 255);
        [SerializeField] private Color activeColor = new Color32(0x3C, 0x7A, 0x4E, 255);

        private const float MaxCardHeight = 96f, CardGap = 6f;
        private const int MaxRows = 4;

        private Image background;
        private Color normalColor;
        private RectTransform fanArea, fan;
        private bool sideways;
        private string shownHand = "";

        private void Start()
        {
            background = GetComponent<Image>();
            if (background != null) normalColor = background.color;
            BuildFan();

            Button button = GetComponent<Button>();
            if (button == null) button = gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OnClicked);

            if (GameView.Current == null) return;
            GameView.Current.OnHandsChanged += Refresh;
            GameView.Current.OnTargetChanged += Refresh;
            GameView.Current.OnTurnChanged += Refresh;
            GameView.Current.OnStateChanged += HandleStateChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (GameView.Current == null) return;
            GameView.Current.OnTurnChanged -= Refresh;
            GameView.Current.OnHandsChanged -= Refresh;
            GameView.Current.OnTargetChanged -= Refresh;
            GameView.Current.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState previous, GameState next) => Refresh();

        private PlayerData GetPlayer() => GameView.Current.GetPlayerAfterLocal(seatsAfterYou);

        /// <summary>
        /// Tapping anywhere on their panel picks them as your target and opens a close-up of their
        /// cards. With no cards there's nothing to show, so the tap just picks (or un-picks) them.
        /// </summary>
        private void OnClicked()
        {
            PlayerData p = GetPlayer();
            if (p == null) return;
            if (GameView.Current.HandCount(p) == 0)
            {
                GameView.Current.SelectTarget(p);
                return;
            }
            Transform canvas = GetComponentInParent<Canvas>().rootCanvas.transform;
            CardPicker.OpenCloseUp(canvas, GameView.Current, p);
        }

        private void Refresh()
        {
            PlayerData p = GetPlayer();
            if (p == null)
            {
                label.text = "";
                LayOutCards(null);
                return;
            }

            var players = GameView.Current.Players;
            int turn = 0;
            for (int i = 0; i < players.Count; i++)
                if (players[i] == p) turn = i + 1;

            bool active = GameView.Current.ActivePlayer == p;
            string status = active ? "<b>Taking their turn...</b>" : $"Turn {turn} of {players.Count}";
            int count = GameView.Current.HandCount(p);
            string cards = count == 1 ? "1 card" : $"{count} cards";
            label.text = $"<b>{p.playerName}</b>\n<size=65%>{cards}  ·  {status}</size>";
            LayOutCards(p.hand);

            // Your pick for Trade / Steal wins over the "their turn" colour
            if (background != null)
                background.color = GameView.Current.SelectedTarget == p ? Glass(selectedColor)
                    : active ? Glass(activeColor)
                    : normalColor;
        }

        // Highlights stay a little see-through, like the panel itself
        private Color Glass(Color c) => new Color(c.r, c.g, c.b, Mathf.Max(normalColor.a, 0.8f));

        // ---------- Their cards, face up ----------

        /// <summary>
        /// The label moves to the top of the panel; their cards sit face up below it, upright so
        /// you can read them. The narrow side panels stack them in rows.
        /// </summary>
        private void BuildFan()
        {
            var zone = (RectTransform)transform;
            bool left = zone.anchorMax.x <= 0.01f, right = zone.anchorMin.x >= 0.99f;
            sideways = left || right;
            float split = sideways ? 0.62f : 0.45f;

            RectTransform text = label.rectTransform;
            text.anchorMin = new Vector2(0f, split);
            text.anchorMax = Vector2.one;
            text.offsetMin = new Vector2(10f, 0f);
            text.offsetMax = new Vector2(-10f, -8f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 32f;

            fanArea = SundownArt.NewRect("HandArea", transform, Vector2.zero, new Vector2(1f, split), new Vector2(12f, 10f), new Vector2(-12f, 0f));
            fan = SundownArt.NewRect("Hand", fanArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var group = fan.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false; // taps go to the panel, which opens the close-up
        }

        /// <summary>
        /// Lays out their cards face up. Tries 1 to 4 rows and keeps whichever leaves the most of
        /// each card showing; when they still don't fit, cards in a row overlap from the left,
        /// so each card's name (top-left) stays readable.
        /// </summary>
        private void LayOutCards(IReadOnlyList<CardData> hand)
        {
            int count = hand?.Count ?? 0;
            Rect area = fanArea.rect;

            // Only rebuild when the cards or the room for them changed
            var key = new System.Text.StringBuilder($"{area.width:0}x{area.height:0}");
            for (int i = 0; i < count; i++) key.Append('|').Append(hand[i].FrontName);
            if (key.ToString() == shownHand) return;
            shownHand = key.ToString();

            for (int i = fan.childCount - 1; i >= 0; i--) Destroy(fan.GetChild(i).gameObject);
            if (count == 0) return;

            int bestRows = 1;
            float bestScore = float.MinValue;
            for (int r = 1; r <= Mathf.Min(MaxRows, count); r++)
            {
                Layout(r, count, area, out Vector2 size, out float step, out _);
                float visible = Mathf.Min(size.x, step > 0f ? step : size.x);
                float score = visible + size.y * 0.25f;
                if (score > bestScore) { bestScore = score; bestRows = r; }
            }

            Layout(bestRows, count, area, out Vector2 card, out float across, out int perRow);
            int rows = Mathf.CeilToInt(count / (float)perRow);
            float totalHeight = rows * card.y + (rows - 1) * CardGap;
            for (int i = 0; i < count; i++)
            {
                int row = i / perRow, col = i % perRow;
                int inRow = Mathf.Min(perRow, count - row * perRow);
                float rowWidth = card.x + (inRow - 1) * across;
                RectTransform front = CardView.Front(fan, card, hand[i]);
                front.anchoredPosition = new Vector2(-rowWidth / 2f + card.x / 2f + col * across,
                    totalHeight / 2f - card.y / 2f - row * (card.y + CardGap));
            }
        }

        /// <summary>Card size, step between cards and cards per row when using this many rows.</summary>
        private static void Layout(int rows, int count, Rect area, out Vector2 size, out float step, out int perRow)
        {
            perRow = Mathf.CeilToInt(count / (float)rows);
            float height = Mathf.Min(MaxCardHeight, (area.height - (rows - 1) * CardGap) / rows);
            size = new Vector2(height * 0.7f, height);
            step = perRow > 1 ? Mathf.Min(size.x + CardGap, (area.width - size.x) / (perRow - 1)) : 0f;
        }
    }
}

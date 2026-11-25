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
        private RectTransform puppetWindow;
        private Puppet puppet;
        private int puppetSeat;

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
            // Bot seats show their strategy, e.g. "(Aggressive bot)"
            string botTag = BotController.Current != null ? BotController.Current.TagFor(p) : "";
            label.text = $"<b>{p.playerName}</b><size=60%>{botTag}</size>\n<size=65%>{cards}  ·  {status}</size>";
            ShowPuppet(turn);
            LayOutCards(p.hand);

            // Wireframe 03-2/03-3: no box in the room, just a small name tag with their cards on the
            // table below it; the spot only glows when it's their turn or you've picked them
            if (TableRoom.Current != null)
            {
                string tag = active ? "#D9822BEE" : "#13191DE6";
                label.text = $"<mark={tag} padding=\"18,18,4,4\"><b>{p.playerName}</b>  <size=70%>{cards}</size></mark>";
                if (background != null)
                    background.color = GameView.Current.SelectedTarget == p ? new Color(UITheme.Gold.r, UITheme.Gold.g, UITheme.Gold.b, 0.28f)
                        : active ? new Color(UITheme.Orange.r, UITheme.Orange.g, UITheme.Orange.b, 0.18f)
                        : Color.clear;
                return;
            }

            // Your pick for Trade / Steal wins over the "their turn" colour
            if (background != null)
                background.color = GameView.Current.SelectedTarget == p ? Glass(selectedColor)
                    : active ? Glass(UITheme.Orange) // the active player's name plate turns orange (wireframe 05-2)
                    : normalColor;
        }

        /// <summary>Their puppet, numbered by their seat in the turn order.</summary>
        private void ShowPuppet(int seat)
        {
            if (seat == puppetSeat) return;
            if (puppet != null) Destroy(puppet.gameObject);
            puppetSeat = seat;
            // Seated behind the table in the room (wireframe 03-2); a small bust if there's no room
            if (TableRoom.Current != null)
            {
                puppet = TableRoom.Current.SeatPuppet(seatsAfterYou, seat);
                return;
            }
            if (puppetWindow == null) return;
            puppet = Puppet.Create(puppetWindow, new Vector2(0.5f, 0f), seat, 130f);
            ((RectTransform)puppet.transform).anchoredPosition = new Vector2(0f, -64f);
        }

        // Everyone's eyes are closed at Night (wireframe 04-2)
        private void Update()
        {
            if (puppet != null && GameView.Current != null) puppet.EyesClosed = GameView.Current.EyesClosed;
            TurnCards();
        }

        /// <summary>
        /// In the room their cards lie on the table facing them (upside down to you, angled toward
        /// their seat). On your Day turn, or once you've picked them, the cards turn round to face you
        /// and grow a little, so you can read them and pick one.
        /// </summary>
        private void TurnCards()
        {
            if (fan == null || TableRoom.Current == null || GameView.Current == null) return;
            IGameView view = GameView.Current;
            PlayerData p = GetPlayer();
            bool forYou = p != null && !view.EyesClosed
                && ((view.IsLocalTurn && view.CurrentState == GameState.DayPhase) || view.SelectedTarget == p);
            float facing = seatsAfterYou == 1 ? 200f : seatsAfterYou == 3 ? 160f : 180f;
            float target = forYou ? 0f : facing;
            float k = GameSettings.ReduceMotion ? 1f : 1f - Mathf.Exp(-7f * Time.unscaledDeltaTime);
            float z = Mathf.LerpAngle(fan.localEulerAngles.z, target, k);
            fan.localEulerAngles = new Vector3(0f, 0f, z);
            float scale = Mathf.Lerp(fan.localScale.x, forYou ? 1.08f : 0.94f, k);
            fan.localScale = new Vector3(scale, scale, 1f);
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
            text.offsetMin = new Vector2(TableRoom.Current != null ? 10f : 84f, 0f); // room for a bust when there's no room
            text.offsetMax = new Vector2(-10f, -8f);

            // Wireframe 03-2: without the room, a small puppet bust in a round window
            if (TableRoom.Current == null) puppetWindow = SundownArt.NewRect("PuppetWindow", transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -76f), new Vector2(74f, -12f));
            if (puppetWindow != null)
            {
                Image window = puppetWindow.gameObject.AddComponent<Image>();
                window.sprite = SundownArt.CircleSprite();
                window.color = UITheme.SkyMid;
                window.raycastTarget = false;
                puppetWindow.gameObject.AddComponent<Mask>();
            }
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

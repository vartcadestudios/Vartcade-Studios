using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Data;
using TDTS.Managers;

namespace TDTS.UI
{
    /// <summary>An opponent's zone on the board: shows name and card count, tap to target.</summary>
    public class PlayerZoneController : MonoBehaviour
    {
        [Tooltip("Seats after you in turn order: 1 = left, 2 = top, 3 = right.")]
        [SerializeField] private int seatsAfterYou = 1;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Color selectedColor = new Color32(0xE8, 0x82, 0x3A, 255);
        [SerializeField] private Color activeColor = new Color32(0x3C, 0x7A, 0x4E, 255);

        private const int MaxCardsShown = 10;
        private const int CardsShownInTheDark = 3;

        private Image background;
        private Color normalColor;
        private RectTransform fan;
        private CanvasGroup fanFade;
        private readonly List<RectTransform> fanCards = new List<RectTransform>();

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

        private void OnClicked()
        {
            GameView.Current.SelectTarget(GetPlayer());
        }

        private void Refresh()
        {
            PlayerData p = GetPlayer();
            if (p == null)
            {
                label.text = "";
                LayOutFan(0, false);
                return;
            }

            var players = GameView.Current.Players;
            int turn = 0;
            for (int i = 0; i < players.Count; i++)
                if (players[i] == p) turn = i + 1;

            bool active = GameView.Current.ActivePlayer == p;
            string status = active ? "<b>Taking their turn...</b>" : $"Turn {turn} of {players.Count}";
            // Eyes closed: you can't see how many cards anyone holds
            bool dark = GameView.Current.EyesClosed;
            int count = GameView.Current.HandCount(p);
            string cards = dark ? "? cards" : count == 1 ? "1 card" : $"{count} cards";
            label.text = $"<b>{p.playerName}</b>\n<size=65%>{cards}  ·  {status}</size>";
            LayOutFan(count, dark);

            // Your pick for Trade / Steal wins over the "their turn" colour
            if (background != null)
                background.color = GameView.Current.SelectedTarget == p ? Glass(selectedColor)
                    : active ? Glass(activeColor)
                    : normalColor;
        }

        // Highlights stay a little see-through, like the panel itself
        private Color Glass(Color c) => new Color(c.r, c.g, c.b, Mathf.Max(normalColor.a, 0.8f));

        // ---------- Their cards, face down ----------

        /// <summary>The label moves up top; their cards fan out face down below it.</summary>
        private void BuildFan()
        {
            RectTransform text = label.rectTransform;
            text.anchorMin = new Vector2(0f, 0.45f);
            text.anchorMax = Vector2.one;
            text.offsetMin = new Vector2(10f, 0f);
            text.offsetMax = new Vector2(-10f, -8f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 32f;

            fan = SundownArt.NewRect("Hand", transform, Vector2.zero, new Vector2(1f, 0.45f), new Vector2(12f, 10f), new Vector2(-12f, 0f));
            fanFade = fan.gameObject.AddComponent<CanvasGroup>();
            fanFade.blocksRaycasts = false;
        }

        /// <summary>Lays out their face-down cards. With eyes closed you only see a vague pile.</summary>
        private void LayOutFan(int count, bool dark)
        {
            int shown = dark ? (count > 0 ? CardsShownInTheDark : 0) : Mathf.Min(count, MaxCardsShown);
            Rect area = fan.rect;
            float height = Mathf.Clamp(area.height * 0.9f, 30f, 96f);
            Vector2 size = new Vector2(height * 0.7f, height);

            while (fanCards.Count < shown) fanCards.Add(SundownArt.CardBack(fan, size));
            float step = shown > 1 ? Mathf.Min(size.x * 0.62f, (area.width - size.x) / (shown - 1)) : 0f;
            if (dark) step = size.x * 0.18f; // a tight, unreadable stack

            for (int i = 0; i < fanCards.Count; i++)
            {
                RectTransform card = fanCards[i];
                card.gameObject.SetActive(i < shown);
                if (i >= shown) continue;

                float fromMiddle = i - (shown - 1) / 2f;
                card.sizeDelta = size;
                card.anchoredPosition = new Vector2(fromMiddle * step, -Mathf.Abs(fromMiddle) * 2.5f);
                card.localEulerAngles = new Vector3(0f, 0f, dark ? 0f : -fromMiddle * 4f);
            }
            fanFade.alpha = dark ? 0.55f : 1f;
        }
    }
}
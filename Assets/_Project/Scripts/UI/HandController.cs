using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Data;
using TDTS.Managers;

namespace TDTS.UI
{
    /// <summary>
    /// Displays the local player's hand. Tap a card to pick it for Trade, Play card, Swap or Peek;
    /// the Peek button then shows its back for a moment (Night, free).
    /// Tapping a card you've already peeked at picks it and shows its back again, any time.
    /// When someone offers you a trade, the card they asked for is lifted and edged in gold.
    /// There's no hand limit: slots are added as the hand grows, and cards overlap to fit the row.
    /// </summary>
    public class HandController : MonoBehaviour
    {
        [System.Serializable]
        public class CardSlotView
        {
            public GameObject root;
            public Image background;
            public TextMeshProUGUI label;
        }

        [Header("Card Slots (assign in Inspector)")]
        [SerializeField] private CardSlotView[] slots;

        private static readonly Color SelectedBorder = new Color32(0xFF, 0xD5, 0x4A, 255);

        [Header("Peek")]
        [SerializeField] private float peekSeconds = 1.5f;

        private int peekIndex = -1;
        private Coroutine peekRoutine;

        // Built at runtime on top of the scene's slots
        private readonly List<CardSlotView> cards = new List<CardSlotView>();
        private readonly List<TextMeshProUGUI> typeLabels = new List<TextMeshProUGUI>();
        private readonly List<GameObject> peekedMarks = new List<GameObject>();
        private GameObject slotTemplate; // an unstyled copy of the first scene slot
        private List<int> backgroundPath, labelPath;
        private float gap;

        private void Start()
        {
            // Copy a slot before styling, so new slots go through the same styling as the scene's.
            // Styling adds children, so note where the name and background sit first.
            slotTemplate = Instantiate(slots[0].root, transform);
            slotTemplate.name = "CardSlotTemplate";
            slotTemplate.SetActive(false);
            backgroundPath = PathTo(slots[0].background.transform, slots[0].root.transform);
            labelPath = PathTo(slots[0].label.transform, slots[0].root.transform);

            // The hand row places its own cards from here on, so they can overlap when the hand is big
            var row = GetComponent<HorizontalLayoutGroup>();
            gap = row != null ? row.spacing : 20f;
            if (row != null) row.enabled = false;

            foreach (CardSlotView slot in slots) AddSlot(slot);

            if (GameView.Current == null) return;

            GameView.Current.OnHandsChanged += HandleHandsChanged;
            GameView.Current.OnTargetChanged += Refresh; // selected card changed
            GameView.Current.OnLocalPeek += Peek;
            Refresh(); // hands may already have been dealt before we subscribed
        }

        private void OnDestroy()
        {
            if (GameView.Current == null) return;
            GameView.Current.OnHandsChanged -= HandleHandsChanged;
            GameView.Current.OnTargetChanged -= Refresh;
            GameView.Current.OnLocalPeek -= Peek;
        }

        private void HandleHandsChanged()
        {
            // Hand contents changed, so any peek in progress points at the wrong card.
            CancelPeek();
            Refresh();
        }

        private void OnSlotClicked(int index)
        {
            if (GameView.Current == null) return;

            PlayerData me = GameView.Current.LocalPlayer;
            if (me == null || index >= me.hand.Count) return;

            CardData card = me.hand[index];

            // A tap only picks the card, for Peek, Trade, Swap or Play card. Peeking is its own button.
            GameView.Current.SelectCard(index);

            // You already know this card's back, so you can look at it again as often as you like
            if (card.peeked) Peek(index);
        }

        /// <summary>The card of yours someone asked for in a trade waiting on your answer, or -1.</summary>
        private static int AskedForIndex()
        {
            IGameView view = GameView.Current;
            return view.HasPendingTrade && view.TradeTo == view.LocalPlayer ? view.TradeWantedIndex : -1;
        }

        /// <summary>Hooks up and styles one card slot.</summary>
        private void AddSlot(CardSlotView slot)
        {
            int index = cards.Count; // capture a copy for the lambda
            cards.Add(slot);

            Button button = slot.root.GetComponent<Button>();
            if (button == null) button = slot.root.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => OnSlotClicked(index));

            // Centre pivot, so a picked card grows evenly from its middle
            var rect = (RectTransform)slot.root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            typeLabels.Add(null);
            peekedMarks.Add(null);
            StyleSlot(index);
        }

        /// <summary>More cards than slots: add copies of the scene's slot.</summary>
        private void EnsureSlots(int count)
        {
            while (cards.Count < count)
            {
                GameObject root = Instantiate(slotTemplate, transform);
                root.name = $"CardSlot{cards.Count + 1}";
                root.SetActive(true);
                AddSlot(new CardSlotView
                {
                    root = root,
                    background = Follow(root.transform, backgroundPath).GetComponent<Image>(),
                    label = Follow(root.transform, labelPath).GetComponent<TextMeshProUGUI>(),
                });
            }
        }

        /// <summary>Child indexes leading from root down to target.</summary>
        private static List<int> PathTo(Transform target, Transform root)
        {
            var path = new List<int>();
            for (Transform t = target; t != root; t = t.parent) path.Insert(0, t.GetSiblingIndex());
            return path;
        }

        private static Transform Follow(Transform root, List<int> path)
        {
            foreach (int i in path) root = root.GetChild(i);
            return root;
        }

        /// <summary>
        /// Lines the cards up from the left. When they don't fit, they overlap more and more,
        /// and the picked or peeked card comes to the front so you can read it.
        /// </summary>
        private void LayOutCards(int count)
        {
            if (count == 0) return;
            float width = ((RectTransform)cards[0].root.transform).rect.width;
            float room = ((RectTransform)transform).rect.width;
            float step = count > 1 ? Mathf.Min(width + gap, (room - width) / (count - 1)) : 0f;

            for (int i = 0; i < count; i++)
            {
                var rect = (RectTransform)cards[i].root.transform;
                rect.anchoredPosition = new Vector2(width / 2f + i * step, 0f);
                rect.SetSiblingIndex(i);
            }

            int front = peekIndex >= 0 && peekIndex < count ? peekIndex
                : AskedForIndex() >= 0 ? AskedForIndex() : GameView.Current.SelectedCardIndex;
            if (front >= 0 && front < count) cards[front].root.transform.SetAsLastSibling();
        }

        private void Peek(int index)
        {
            CancelPeek();
            peekIndex = index;
            Refresh();
            peekRoutine = StartCoroutine(PeekRoutine());
        }

        private IEnumerator PeekRoutine()
        {
            yield return new WaitForSeconds(peekSeconds);
            peekIndex = -1;
            peekRoutine = null;
            Refresh();
        }

        private void CancelPeek()
        {
            if (peekRoutine != null) StopCoroutine(peekRoutine);
            peekRoutine = null;
            peekIndex = -1;
        }

        /// <summary>
        /// Turns a plain scene slot into a card: rounded corners, the name up top, and a darker band
        /// along the bottom for the card type, with a gold dot once you've peeked at it.
        /// </summary>
        private void StyleSlot(int i)
        {
            CardSlotView slot = cards[i];
            SundownArt.Rounded(slot.background, slot.background.color, 12);

            var band = SundownArt.NewRect("TypeBand", slot.root.transform, Vector2.zero, new Vector2(1f, 0.3f),
                new Vector2(4f, 4f), new Vector2(-4f, 0f));
            Image bandImage = SundownArt.Rounded(band.gameObject.AddComponent<Image>(), new Color(0f, 0f, 0f, 0.28f), 8);
            bandImage.raycastTarget = false;
            band.SetSiblingIndex(0); // behind the name

            typeLabels[i] = NewLabel("Type", band, 13f, FontStyles.Bold);
            typeLabels[i].fontSizeMin = 8f;
            typeLabels[i].characterSpacing = 4f;

            // The scene's label becomes the card's name, sized to fit on one or two lines
            RectTransform name = slot.label.rectTransform;
            name.anchorMin = new Vector2(0f, 0.3f);
            name.anchorMax = Vector2.one;
            name.offsetMin = new Vector2(6f, 0f);
            name.offsetMax = new Vector2(-6f, -8f);
            slot.label.alignment = TextAlignmentOptions.Center;
            slot.label.fontStyle = FontStyles.Bold;
            slot.label.enableAutoSizing = true;
            slot.label.fontSizeMin = 12f;
            slot.label.fontSizeMax = 26f;

            var mark = SundownArt.NewRect("Peeked", slot.root.transform, Vector2.one, Vector2.one,
                new Vector2(-20f, -20f), new Vector2(-8f, -8f));
            Image dot = mark.gameObject.AddComponent<Image>();
            dot.sprite = SundownArt.CircleSprite();
            dot.color = SelectedBorder;
            dot.raycastTarget = false;
            peekedMarks[i] = mark.gameObject;
        }

        private static TextMeshProUGUI NewLabel(string name, Transform parent, float size, FontStyles style)
        {
            var label = SundownArt.NewRect(name, parent, Vector2.zero, Vector2.one, new Vector2(4f, 0f), new Vector2(-4f, 0f))
                .gameObject.AddComponent<TextMeshProUGUI>();
            SundownFonts.Use(label);
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMax = size;
            label.raycastTarget = false;
            return label;
        }

        private void Refresh()
        {
            PlayerData me = GameView.Current.LocalPlayer;
            int count = me?.hand.Count ?? 0;
            EnsureSlots(count);

            for (int i = 0; i < cards.Count; i++)
            {
                bool hasCard = me != null && i < me.hand.Count;
                cards[i].root.SetActive(hasCard);
                if (!hasCard) continue;

                CardData card = me.hand[i];
                bool revealed = i == peekIndex;

                // The owner always sees the front; the back only while peeking.
                // Colours match the Card Gallery (CardView).
                Color color = CardView.ColorFor(card, revealed);
                cards[i].background.color = color;
                cards[i].label.color = typeLabels[i].color = CardView.TextColorOn(color);
                cards[i].label.text = revealed ? card.BackName : card.FrontName;
                typeLabels[i].text = revealed ? $"BACK Â· {CardView.TypeName(card, true)}" : CardView.TypeName(card, false);
                // Spec C6: a peeked card only shows a marker, not its back
                peekedMarks[i].SetActive(card.peeked && !revealed);

                // The card picked for Trade / Play card, or asked for in a trade offered to you,
                // lifts a little and gets a gold edge
                bool picked = i == GameView.Current.SelectedCardIndex || i == AskedForIndex();
                cards[i].root.transform.localScale = picked ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
                Outline outline = cards[i].root.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.enabled = picked;
                    outline.effectColor = SelectedBorder;
                    outline.effectDistance = new Vector2(4f, -4f);
                }
            }

            LayOutCards(count);
            MatchNameSizes();
        }

        /// <summary>
        /// All names in your hand share one size, so "Food" doesn't shout beside "Entertainment".
        /// Each card works out the biggest size that fits; everyone uses the smallest of those,
        /// but never below a readable floor. A single word too long for the floor shrinks on its own.
        /// </summary>
        private void MatchNameSizes()
        {
            const float floor = 16f, ceiling = 22f;
            var fits = new float[cards.Count];
            float shared = ceiling;

            for (int i = 0; i < cards.Count; i++)
            {
                if (!cards[i].root.activeInHierarchy) continue;
                TextMeshProUGUI name = cards[i].label;
                name.enableAutoSizing = true;
                name.fontSizeMin = 10f;
                name.fontSizeMax = ceiling;
                name.ForceMeshUpdate();
                fits[i] = name.fontSize;
                shared = Mathf.Min(shared, fits[i]);
            }

            shared = Mathf.Max(shared, floor);
            for (int i = 0; i < cards.Count; i++)
            {
                if (!cards[i].root.activeInHierarchy) continue;
                cards[i].label.enableAutoSizing = false;
                cards[i].label.fontSize = Mathf.Min(shared, fits[i]);
            }
        }
    }
}
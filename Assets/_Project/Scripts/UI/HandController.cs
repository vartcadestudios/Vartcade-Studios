using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Data;
using TDTS.Managers;

namespace TDTS.UI
{
    /// <summary>
    /// Displays the local player's hand. Day: tap a card to pick it for Trade or Play card.
    /// Night: tap a card you haven't seen to Peek (its back shows for a moment), or a peeked one to pick it.
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
        private TextMeshProUGUI[] typeLabels;
        private GameObject[] peekedMarks;

        private void Start()
        {
            typeLabels = new TextMeshProUGUI[slots.Length];
            peekedMarks = new GameObject[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i; // capture a copy for the lambda
                Button button = slots[i].root.GetComponent<Button>();
                if (button == null) button = slots[i].root.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => OnSlotClicked(index));
                StyleSlot(i);
            }

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
            GameState state = GameView.Current.CurrentState;

            // Night: tapping a card you haven't seen is a Peek (costs 1 action).
            // Any other tap picks the card for Trade, Swap or Play card.
            if (state == GameState.NightPhase && !card.peeked)
            {
                GameView.Current.PeekForLocalPlayer(index); // OnLocalPeek flashes the back once it goes through
                return;
            }

            GameView.Current.SelectCard(index);
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
            CardSlotView slot = slots[i];
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

            for (int i = 0; i < slots.Length; i++)
            {
                bool hasCard = me != null && i < me.hand.Count;
                slots[i].root.SetActive(hasCard);
                if (!hasCard) continue;

                CardData card = me.hand[i];
                bool revealed = i == peekIndex;

                // The owner always sees the front; the back only while peeking.
                // Colours match the Card Gallery (CardView).
                Color color = CardView.ColorFor(card, revealed);
                slots[i].background.color = color;
                slots[i].label.color = typeLabels[i].color = CardView.TextColorOn(color);
                slots[i].label.text = revealed ? card.BackName : card.FrontName;
                typeLabels[i].text = revealed ? $"BACK · {CardView.TypeName(card, true)}" : CardView.TypeName(card, false);
                // Spec C6: a peeked card only shows a marker, not its back
                peekedMarks[i].SetActive(card.peeked && !revealed);

                // The card picked for Trade / Play card lifts a little and gets a gold edge
                bool picked = i == GameView.Current.SelectedCardIndex;
                slots[i].root.transform.localScale = picked ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
                Outline outline = slots[i].root.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.enabled = picked;
                    outline.effectColor = SelectedBorder;
                    outline.effectDistance = new Vector2(4f, -4f);
                }
            }
        }
    }
}
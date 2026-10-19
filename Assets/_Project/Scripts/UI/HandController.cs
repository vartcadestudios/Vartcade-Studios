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

        private void Start()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i; // capture a copy for the lambda
                Button button = slots[i].root.GetComponent<Button>();
                if (button == null) button = slots[i].root.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => OnSlotClicked(index));
            }

            if (GameManager.Instance == null) return;

            GameManager.Instance.OnHandsChanged += HandleHandsChanged;
            GameManager.Instance.OnTargetChanged += Refresh; // selected card changed
            Refresh(); // hands may already have been dealt before we subscribed
        }

        private void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnHandsChanged -= HandleHandsChanged;
            GameManager.Instance.OnTargetChanged -= Refresh;
        }

        private void HandleHandsChanged()
        {
            // Hand contents changed, so any peek in progress points at the wrong card.
            CancelPeek();
            Refresh();
        }

        private void OnSlotClicked(int index)
        {
            if (GameManager.Instance == null) return;

            PlayerData me = GameManager.Instance.LocalPlayer;
            if (me == null || index >= me.hand.Count) return;

            CardData card = me.hand[index];
            GameState state = GameManager.Instance.CurrentState;

            // Night: tapping a card you haven't seen is a Peek (costs 1 action).
            // Any other tap picks the card for Trade, Swap or Play card.
            if (state == GameState.NightPhase && !card.peeked)
            {
                if (GameManager.Instance.PeekCard(me, index)) Peek(index);
                return;
            }

            GameManager.Instance.SelectCard(index);
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

        private void Refresh()
        {
            PlayerData me = GameManager.Instance.LocalPlayer;

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
                slots[i].label.color = CardView.TextColorOn(color);
                // Spec C6: a peeked card only shows a marker, not its back
                string marker = card.peeked ? " <color=#FFD54A>(peeked)</color>" : "";
                slots[i].label.text = revealed
                    ? $"<size=60%>BACK</size>\n{card.BackName}"
                    : $"{card.FrontName}\n<size=60%>{CardView.TypeName(card, false)}{marker}</size>";

                // Thick gold border on the card picked for Trade / Play card
                Outline outline = slots[i].root.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.enabled = i == GameManager.Instance.SelectedCardIndex;
                    outline.effectColor = SelectedBorder;
                    outline.effectDistance = new Vector2(5f, -5f);
                }
            }
        }
    }
}
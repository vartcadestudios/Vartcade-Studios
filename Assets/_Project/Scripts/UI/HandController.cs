using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Data;
using TDTS.Managers;

namespace TDTS.UI
{
    /// <summary>
    /// Displays the local player's hand. Tapping a Supply/Trash/Key card peeks at it
    /// for a moment; tapping an action card plays it into the play space.
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

        [Header("Colors")]
        [SerializeField] private Color hiddenColor = new Color32(0x4A, 0x4A, 0x4A, 255);
        [SerializeField] private Color revealedColor = new Color32(0x7A, 0x7A, 0x7A, 255);
        [SerializeField] private Color actionColor = new Color32(0x3A, 0x2F, 0x52, 255);

        [Header("Peek")]
        [SerializeField] private float peekSeconds = 1.5f;
        [Tooltip("Rulebook: peeking is a Night action. Tick this to enforce it.")]
        [SerializeField] private bool peekOnlyAtNight = false;

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
            Refresh(); // hands may already have been dealt before we subscribed
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnHandsChanged -= HandleHandsChanged;
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

            if (card.IsAction)
            {
                GameManager.Instance.PlayCard(me, index);
                return;
            }

            if (peekOnlyAtNight && GameManager.Instance.CurrentState != GameState.NightPhase)
            {
                Debug.Log("[HandController] Peeking is a Night action.");
                return;
            }

            Peek(index);
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

                slots[i].background.color =
                    card.IsAction ? actionColor : (revealed ? revealedColor : hiddenColor);
                slots[i].label.text = (card.IsAction || revealed) ? card.DisplayName : "?";

                Outline outline = slots[i].root.GetComponent<Outline>();
                if (outline != null) outline.enabled = card.IsAction;
            }
        }
    }
}
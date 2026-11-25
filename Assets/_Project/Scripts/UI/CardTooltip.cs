using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TDTS.Data;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// Wireframes 03-10 and 06: hover a card on PC (or long-press it for 0.35 s on a phone) to see its
    /// name, what it does, and what its back means at Sundown if you know it. One tooltip at a time;
    /// it closes when the pointer leaves or the finger lifts. Add it to any card with Attach.
    /// </summary>
    public class CardTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const float LongPressSeconds = 0.35f;

        private Func<CardData> getCard;
        private Func<bool> backKnown;
        private float pressedAt = -1f;
        private bool touch;

        private static RectTransform tip;
        private static TextMeshProUGUI tipText;
        private static Image zoom; // the card's illustration, shown big beside the text (UNO-style hover)
        private static CardTooltip owner;

        /// <summary>Shows a tooltip for whatever card getCard returns when the pointer rests on this object.</summary>
        public static CardTooltip Attach(GameObject target, Func<CardData> getCard, Func<bool> backKnown)
        {
            var tooltip = target.GetComponent<CardTooltip>() ?? target.AddComponent<CardTooltip>();
            tooltip.getCard = getCard;
            tooltip.backKnown = backKnown;
            return tooltip;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            touch = e is UnityEngine.InputSystem.UI.ExtendedPointerEventData x && x.pointerType == UnityEngine.InputSystem.UI.UIPointerType.Touch;
            if (!touch) Show();
        }

        public void OnPointerExit(PointerEventData e) => Hide();

        public void OnPointerDown(PointerEventData e)
        {
            pressedAt = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData e)
        {
            pressedAt = -1f;
            if (touch) Hide();
        }

        private void Update()
        {
            if (touch && pressedAt >= 0f && Time.unscaledTime - pressedAt >= LongPressSeconds && owner != this)
                Show();
            if (owner == this && tip != null) Place();
        }

        private void OnDisable() => Hide();

        private void Show()
        {
            CardData card = getCard?.Invoke();
            if (card == null) return;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            EnsureTip(canvas.rootCanvas.transform);
            tipText.text = Describe(card, backKnown != null && backKnown());
            tip.gameObject.SetActive(true);
            tip.SetAsLastSibling();
            Sprite art = CardArt.Front(card);
            if (zoom != null)
            {
                zoom.sprite = art;
                zoom.gameObject.SetActive(art != null);
                if (art != null) zoom.rectTransform.SetAsLastSibling();
            }
            owner = this;
            LayoutRebuilder.ForceRebuildLayoutImmediate(tip);
            Place();
        }

        private void Hide()
        {
            if (owner != this) return;
            owner = null;
            if (tip != null) tip.gameObject.SetActive(false);
            if (zoom != null) zoom.gameObject.SetActive(false);
        }

        // Above the card, kept on screen
        private void Place()
        {
            var me = (RectTransform)transform;
            var parent = (RectTransform)tip.parent;
            Vector3[] corners = new Vector3[4];
            me.GetWorldCorners(corners);
            Vector3 topMiddle = (corners[1] + corners[2]) / 2f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, RectTransformUtility.WorldToScreenPoint(null, topMiddle), null, out Vector2 local);
            Vector2 size = tip.rect.size;
            Rect area = parent.rect;
            float x = Mathf.Clamp(local.x, area.xMin + size.x / 2f + 12f, area.xMax - size.x / 2f - 12f);
            float y = Mathf.Min(local.y + 16f, area.yMax - size.y - 12f);
            tip.anchoredPosition = new Vector2(x, y);

            // The big card sits beside the text: left of it, or right if there's no room on the left
            if (zoom == null || !zoom.gameObject.activeSelf) return;
            Vector2 zoomSize = zoom.rectTransform.sizeDelta;
            float gap = 12f;
            float left = x - size.x / 2f - gap - zoomSize.x / 2f;
            float right = x + size.x / 2f + gap + zoomSize.x / 2f;
            float zx = left - zoomSize.x / 2f >= area.xMin + 12f ? left : Mathf.Min(right, area.xMax - zoomSize.x / 2f - 12f);
            float zy = Mathf.Clamp(y, area.yMin + 12f, area.yMax - zoomSize.y - 12f);
            zoom.rectTransform.anchoredPosition = new Vector2(zx, zy);
        }

        private static void EnsureTip(Transform canvas)
        {
            if (tip != null && tip.parent == canvas) return;
            if (tip != null) Destroy(tip.gameObject);

            tip = NewRect("CardTooltip", canvas, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            tip.pivot = new Vector2(0.5f, 0f);
            Rounded(tip.gameObject.AddComponent<Image>(), UITheme.Cream, 10).raycastTarget = false;
            var shadow = tip.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(0f, -6f);
            var layout = tip.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 16, 18);
            layout.childControlWidth = layout.childControlHeight = true;
            var fit = tip.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            tip.sizeDelta = new Vector2(440f, 0f);

            tipText = NewText("Text", tip, 22, FontStyles.Normal, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            tipText.color = UITheme.Charcoal;
            tip.gameObject.SetActive(false);

            if (zoom != null) Destroy(zoom.gameObject);
            var zoomRect = NewRect("CardZoom", canvas, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            zoomRect.pivot = new Vector2(0.5f, 0f);
            zoomRect.sizeDelta = new Vector2(240f, 240f / CardArt.Aspect);
            zoom = zoomRect.gameObject.AddComponent<Image>();
            zoom.raycastTarget = false;
            var zoomShadow = zoomRect.gameObject.AddComponent<Shadow>();
            zoomShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            zoomShadow.effectDistance = new Vector2(0f, -8f);
            zoomRect.gameObject.SetActive(false);
        }

        /// <summary>Kind or type, name, a one-line effect, then the back if you know it.</summary>
        public static string Describe(CardData card, bool knowBack)
        {
            string eyebrow, name, effect;
            if (card.HasMorningFront)
            {
                eyebrow = card.frontUsed ? "MORNING CARD  ·  USED" : "MORNING CARD  ·  FRONT";
                name = card.FrontName;
                effect = MorningEffect(card.morningAction) + " Works once; the card stays in your hand.";
            }
            else
            {
                eyebrow = "SUPPLY FRONT";
                name = card.FrontName;
                effect = "Only the back counts at Sundown. A Supply front can hide Trash, a Night card, or even the Bunker Key.";
            }

            string back = knowBack ? "Back: " + BackEffect(card) : "Back: hidden. Peek at Night to find out what it really is.";
            return $"<size=70%><color=#B53B32><b>{eyebrow}</b></color></size>\n" +
                   $"<size=130%><b>{name}</b></size>\n{effect}\n<size=85%><color=#13191DAA>{back}</color></size>";
        }

        private static string MorningEffect(MorningActionType action) => action switch
        {
            MorningActionType.SwapDeck => "Swap your whole hand with another player's.",
            MorningActionType.ReverseTurn => "Turn order flips direction.",
            MorningActionType.SkipNextPlayer => "The next player loses their Day action.",
            MorningActionType.EveryonePlusOne => "Everyone draws a card, starting with you.",
            _ => "Every hand passes to the next player in turn order.",
        };

        private static string BackEffect(CardData card) => card.back switch
        {
            BackType.Supply => $"real {TableHud.KindNames[card.backSupply]}. Counts toward your 3 kinds.",
            BackType.BunkerKey => "the Bunker Key. Hold it at Sundown and a seat is yours.",
            BackType.NightAction => card.nightAction switch
            {
                NightActionType.Flashlight => "Flashlight. At Night, see every back in one hand (yours too). One use.",
                NightActionType.TargetedNightSkip => "Night Skip. At Night, the player you pick loses their next Night turn.",
                _ => "Day Skip. At Night, the player you pick loses their next Day turn.",
            },
            _ => "Trash. Worth nothing at Sundown.",
        };
    }
}

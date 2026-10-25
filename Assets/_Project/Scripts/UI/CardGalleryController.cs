using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TDTS.Data;

namespace TDTS.UI
{
    /// <summary>
    /// Lays out every card in the deck (72 + the Bunker Key) so the whole set can be checked
    /// at a glance. Tap a card to flip it, or use the buttons to flip them all.
    /// Builds its own UI at runtime: drop it on an empty GameObject and press Play.
    /// </summary>
    public class CardGalleryController : MonoBehaviour
    {
        [SerializeField] private DeckRecipe deckRecipe = new DeckRecipe();
        [SerializeField] private Vector2 cardSize = new Vector2(112f, 150f);
        [SerializeField] private int columns = 15;

        private static readonly Color PageColor = new Color32(0x15, 0x17, 0x1C, 255);
        private static readonly Color ButtonColor = new Color32(0x2F, 0x3B, 0x4C, 255);
        private static readonly Color AccentColor = new Color32(0xC9, 0x77, 0x2E, 255);

        private readonly List<CardView> views = new List<CardView>();
        private Transform grid;
        private TextMeshProUGUI summary;

        private void Start()
        {
            EnsureCameraAndEventSystem();

            Transform canvas = BuildCanvas();
            BuildHeader(canvas);
            BuildGrid(canvas);
            Deal();
        }

        /// <summary>Builds a fresh deck (new random front/back pairing) and shows every card front-up.</summary>
        private void Deal()
        {
            foreach (CardView view in views) Destroy(view.gameObject);
            views.Clear();

            var removed = new List<CardData>();
            List<CardData> cards = DeckBuilder.BuildDrawPile(deckRecipe, new System.Random(), removed);

            // Morning fronts first, then Supply (the Key among them); matching backs grouped inside each.
            IEnumerable<CardData> ordered = cards
                .OrderBy(c => c.front == FrontType.MorningAction ? 0 : c.front == FrontType.Supply ? 1 : 2)
                .ThenBy(c => c.front == FrontType.MorningAction ? (int)c.morningAction : (int)c.frontSupply)
                .ThenBy(c => c.back)
                .ThenBy(c => c.back == BackType.NightAction ? (int)c.nightAction : (int)c.backSupply);

            foreach (CardData card in ordered)
            {
                CardView view = CardView.Create(grid, cardSize);
                view.Bind(card, false);
                views.Add(view);
            }

            summary.text =
                $"{cards.Count} cards in play.   " +
                $"Fronts: {Count(cards, c => c.front == FrontType.Supply)} Supply, " +
                $"{Count(cards, c => c.HasMorningFront)} Morning (the Key hides behind a Supply front).   " +
                $"Backs: {Count(cards, c => c.IsRealSupply)} Supply, {Count(cards, c => c.back == BackType.Trash)} Trash, " +
                $"{Count(cards, c => c.HasNightBack)} Night, {Count(cards, c => c.IsKey)} Key.   " +
                "Tap a card to flip it.";
        }

        private void ShowAll(bool backs)
        {
            foreach (CardView view in views) view.ShowSide(backs);
        }

        private static int Count(List<CardData> cards, System.Func<CardData, bool> match) => cards.Count(match);

        // ---------- UI construction ----------

        private static void EnsureCameraAndEventSystem()
        {
            if (Camera.main == null)
            {
                var cam = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" }.GetComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = PageColor;
                cam.orthographic = true;
            }

            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private Transform BuildCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var page = NewRect("Background", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            page.gameObject.AddComponent<Image>().color = PageColor;

            return go.transform;
        }

        private void BuildHeader(Transform canvas)
        {
            var header = NewRect("Header", canvas, new Vector2(0f, 1f), Vector2.one, new Vector2(48f, -130f), new Vector2(-48f, -24f));

            var title = NewText("Title", header, 40, FontStyles.Bold, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            title.text = "Ten Days to Sundown  <color=#C9772E>·</color>  Card Gallery";

            summary = NewText("Summary", header, 20, FontStyles.Normal, TextAlignmentOptions.BottomLeft,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            summary.color = new Color32(0xB8, 0xBC, 0xC4, 255);

            var buttons = NewRect("Buttons", header, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-640f, -52f), Vector2.zero);
            var row = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 12f;
            row.childAlignment = TextAnchor.UpperRight;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = true;

            NewButton(buttons, "Show fronts", ButtonColor, () => ShowAll(false));
            NewButton(buttons, "Show backs", ButtonColor, () => ShowAll(true));
            NewButton(buttons, "New shuffle", AccentColor, Deal);
        }

        private void BuildGrid(Transform canvas)
        {
            var area = NewRect("Grid", canvas, Vector2.zero, Vector2.one, new Vector2(48f, 24f), new Vector2(-48f, -150f));
            var layout = area.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = cardSize;
            layout.spacing = new Vector2(8f, 8f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = columns;
            layout.childAlignment = TextAnchor.UpperCenter;
            grid = area;
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, float size, FontStyles style,
            TextAlignmentOptions align, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var label = NewRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax).gameObject.AddComponent<TextMeshProUGUI>();
            SundownFonts.Use(label);
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = align;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static void NewButton(Transform parent, string text, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var rect = NewRect(text, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rect.gameObject.AddComponent<Image>().color = color;
            rect.gameObject.AddComponent<Button>().onClick.AddListener(onClick);

            var label = NewText("Label", rect, 20, FontStyles.Bold, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.text = text;
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TDTS.Data;

namespace TDTS.UI
{
    /// <summary>
    /// One card on screen, showing either its front or its back. Tap to flip.
    /// Built entirely from code so it needs no prefab: use CardView.Create.
    /// </summary>
    public class CardView : MonoBehaviour
    {
        private static readonly Color MorningColor = new Color32(0xD9, 0xA4, 0x41, 255);
        private static readonly Color NightColor = new Color32(0x2A, 0x2F, 0x5C, 255);
        private static readonly Color TrashColor = new Color32(0x55, 0x55, 0x55, 255);
        private static readonly Color KeyColor = new Color32(0xF2, 0xD0, 0x4B, 255);
        private static readonly Color LightText = new Color32(0xF4, 0xF1, 0xEA, 255);
        private static readonly Color DarkText = new Color32(0x22, 0x1E, 0x16, 255);

        private Image background;
        private Outline border;
        private TextMeshProUGUI sideLabel;
        private TextMeshProUGUI nameLabel;
        private TextMeshProUGUI typeLabel;

        private CardData card;
        private bool showingBack;

        public CardData Card => card;
        public bool ShowingBack => showingBack;

        public static CardView Create(Transform parent, Vector2 size)
        {
            var go = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = size;

            var view = go.AddComponent<CardView>();
            view.background = go.GetComponent<Image>();
            view.border = go.GetComponent<Outline>();
            view.border.effectDistance = new Vector2(3f, -3f);

            Button button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(view.Flip);

            view.sideLabel = MakeLabel(go.transform, "Side", 11, FontStyles.Bold, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -26f), new Vector2(-8f, -6f));
            view.nameLabel = MakeLabel(go.transform, "Name", 18, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(6f, 28f), new Vector2(-6f, -28f));
            view.typeLabel = MakeLabel(go.transform, "Type", 10, FontStyles.Normal, TextAlignmentOptions.Bottom,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(6f, 6f), new Vector2(-6f, 26f));

            view.nameLabel.enableAutoSizing = true;
            view.nameLabel.fontSizeMin = 10;
            view.nameLabel.fontSizeMax = 18;

            return view;
        }

        public void Bind(CardData data, bool showBack)
        {
            card = data;
            showingBack = showBack;
            Refresh();
        }

        public void ShowSide(bool showBack)
        {
            showingBack = showBack;
            Refresh();
        }

        public void Flip() => ShowSide(!showingBack);

        private void Refresh()
        {
            if (card == null) return;

            Color color = ColorFor(card, showingBack);
            background.color = color;
            border.effectColor = showingBack ? new Color(0f, 0f, 0f, 0.6f) : new Color(1f, 1f, 1f, 0.35f);

            sideLabel.color = nameLabel.color = typeLabel.color = TextColorOn(color);

            sideLabel.text = showingBack ? "BACK" : "FRONT";
            nameLabel.text = showingBack ? card.BackName : card.FrontName;
            typeLabel.text = TypeName(card, showingBack);
        }

        /// <summary>Card colour for one side, shared with the hand row so cards look the same everywhere.</summary>
        public static Color ColorFor(CardData card, bool back)
        {
            if (back)
            {
                return card.back switch
                {
                    BackType.Supply => SupplyColor(card.backSupply),
                    BackType.NightAction => NightColor,
                    BackType.BunkerKey => KeyColor,
                    _ => TrashColor
                };
            }

            return card.front switch
            {
                FrontType.MorningAction => MorningColor,
                FrontType.BunkerKey => KeyColor,
                _ => SupplyColor(card.frontSupply)
            };
        }

        public static Color TextColorOn(Color background) =>
            (background == MorningColor || background == KeyColor) ? DarkText : LightText;

        public static string TypeName(CardData card, bool back)
        {
            if (back)
            {
                return card.back switch
                {
                    BackType.Supply => "REAL SUPPLY",
                    BackType.NightAction => "NIGHT ACTION",
                    BackType.BunkerKey => "BUNKER KEY",
                    _ => "TRASH"
                };
            }

            return card.front switch
            {
                FrontType.MorningAction => "MORNING ACTION",
                FrontType.BunkerKey => "BUNKER KEY",
                _ => "SUPPLY"
            };
        }

        private static Color SupplyColor(SupplyKind kind) => kind switch
        {
            SupplyKind.Food => new Color32(0x8C, 0x5A, 0x2B, 255),
            SupplyKind.Water => new Color32(0x2B, 0x6A, 0x8C, 255),
            SupplyKind.Medicine => new Color32(0x9C, 0x33, 0x3F, 255),
            _ => new Color32(0x6A, 0x3F, 0x8C, 255)
        };

        private static TextMeshProUGUI MakeLabel(Transform parent, string name, float size, FontStyles style,
            TextAlignmentOptions align, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var label = SundownFonts.Use(go.GetComponent<TextMeshProUGUI>());
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = align;
            label.raycastTarget = false;
            return label;
        }
    }
}

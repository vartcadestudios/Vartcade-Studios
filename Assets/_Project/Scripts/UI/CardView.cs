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
            CardTooltip.Attach(go, () => view.card, () => view.showingBack);

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

            // The design team's illustration when there is one; the coloured card below otherwise
            Sprite art = CardArt.Side(card, showingBack);
            sideLabel.gameObject.SetActive(art == null);
            nameLabel.gameObject.SetActive(art == null);
            typeLabel.gameObject.SetActive(art == null);
            if (art != null)
            {
                background.sprite = art;
                background.type = Image.Type.Simple;
                background.preserveAspect = true;
                background.color = !showingBack && card.HasMorningFront && card.frontUsed ? UsedTint : Color.white;
                border.effectColor = new Color(0f, 0f, 0f, 0.55f);
                return;
            }
            background.sprite = null;
            background.preserveAspect = false;

            Color color = ColorFor(card, showingBack);
            background.color = color;
            border.effectColor = showingBack ? new Color(0f, 0f, 0f, 0.6f) : new Color(1f, 1f, 1f, 0.35f);

            sideLabel.color = nameLabel.color = typeLabel.color = TextColorOn(color);

            sideLabel.text = showingBack ? "BACK" : "FRONT";
            nameLabel.text = WithSymbol(card, showingBack, showingBack ? card.BackName : card.FrontName);
            typeLabel.text = TypeName(card, showingBack);
        }

        /// <summary>
        /// The front of a card as everyone at the table sees it: its colour, its name in the top-left
        /// (so it stays readable when cards overlap) and its type along the bottom. Never blocks taps.
        /// </summary>
        public static RectTransform Front(Transform parent, Vector2 size, CardData card)
        {
            var rect = SundownArt.NewRect("CardFront", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rect.sizeDelta = size;

            Sprite art = CardArt.Front(card);
            if (art != null)
            {
                bool used = card.HasMorningFront && card.frontUsed;
                Image face = rect.gameObject.AddComponent<Image>();
                face.sprite = art;
                face.preserveAspect = true;
                face.color = used ? UsedTint : Color.white;
                face.raycastTarget = false;
                var shade = rect.gameObject.AddComponent<Outline>();
                shade.effectColor = new Color(0f, 0f, 0f, 0.5f);
                shade.effectDistance = new Vector2(1.5f, -1.5f);
                if (used) UsedStamp(rect, size);
                return rect;
            }

            Color color = ColorFor(card, false);
            SundownArt.Rounded(rect.gameObject.AddComponent<Image>(), color, 8).raycastTarget = false;
            var edge = rect.gameObject.AddComponent<Outline>();
            edge.effectColor = new Color(0f, 0f, 0f, 0.45f);
            edge.effectDistance = new Vector2(1.5f, -1.5f);

            float pad = Mathf.Max(3f, size.x * 0.07f);
            var name = MakeLabel(rect, "Name", size.y * 0.16f, FontStyles.Bold, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, new Vector2(pad, size.y * 0.24f), new Vector2(-pad, -pad));
            name.text = WithSymbol(card, false, card.FrontName, inline: true);
            name.enableAutoSizing = true;
            name.fontSizeMin = 7f;
            name.fontSizeMax = size.y * 0.16f;

            var type = MakeLabel(rect, "Type", size.y * 0.08f, FontStyles.Bold, TextAlignmentOptions.BottomLeft,
                Vector2.zero, new Vector2(1f, 0f), new Vector2(pad, pad * 0.6f), new Vector2(-pad, size.y * 0.24f));
            type.text = card.HasMorningFront && card.frontUsed ? "USED" : TypeName(card, false);
            type.enableAutoSizing = true;
            type.fontSizeMin = 5f;
            type.fontSizeMax = size.y * 0.08f;

            name.color = type.color = TextColorOn(color);
            return rect;
        }

        /// <summary>Grey veil for a Morning card that has been played (wireframe 06-5).</summary>
        public static readonly Color UsedTint = new Color(0.55f, 0.55f, 0.55f, 1f);

        /// <summary>A small red USED stamp across the middle of a card drawn with its illustration.</summary>
        public static void UsedStamp(RectTransform card, Vector2 size)
        {
            var stamp = SundownArt.NewRect("Used", card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            stamp.sizeDelta = new Vector2(size.x * 0.86f, size.y * 0.2f);
            stamp.localEulerAngles = new Vector3(0f, 0f, 14f);
            SundownArt.Rounded(stamp.gameObject.AddComponent<Image>(), UITheme.Cream, 6).raycastTarget = false;
            var label = MakeLabel(stamp, "Text", size.y * 0.12f, FontStyles.Bold, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.text = "USED";
            label.color = UITheme.Brick;
            label.enableAutoSizing = true;
            label.fontSizeMin = 6f;
            label.fontSizeMax = size.y * 0.14f;
        }

        /// <summary>
        /// Wireframe 06-1/06-2: Supply sides carry their kind's shape (Food circle, Water drop,
        /// Medicine plus, Entertainment diamond), so colour is never the only clue.
        /// </summary>
        public static string WithSymbol(CardData card, bool back, string name, bool inline = false)
        {
            SupplyKind? kind = back ? (card.back == BackType.Supply ? card.backSupply : (SupplyKind?)null)
                : (card.front == FrontType.Supply || card.front == FrontType.BunkerKey ? card.frontSupply : (SupplyKind?)null);
            if (kind == null) return name;
            string symbol = TableHud.KindSymbol(kind.Value);
            return inline ? $"{symbol} {name}" : $"<size=130%>{symbol}</size>\n{name}";
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

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TDTS.Networking;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// Online lobby: pick a name, host a table or join one by its code, see who has sat down,
    /// and (host only) start the game for everyone. Seats nobody takes are played by bots.
    /// Builds its own UI at runtime: drop it on an empty GameObject and press Play.
    /// </summary>
    public class LobbyController : MonoBehaviour
    {
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string offlineGameSceneName = "Game";

        private static readonly Color PanelColor = new Color32(0x12, 0x15, 0x22, 255);
        private static readonly Color PanelEdge = new Color32(0xFF, 0xFF, 0xFF, 22);
        private static readonly Color FieldColor = new Color32(0x0C, 0x0F, 0x1A, 255);
        private static readonly Color ButtonColor = new Color32(0x2A, 0x31, 0x45, 255);
        private static readonly Color AccentColor = new Color32(0xE0, 0x7A, 0x2E, 255);
        private static readonly Color YouColor = new Color32(0x5F, 0xB8, 0x7A, 255);
        private static readonly Color MutedText = new Color32(0xA9, 0xAE, 0xBD, 255);
        private static readonly Color DimText = new Color32(0x5C, 0x62, 0x74, 255);
        private static readonly Color SeatColor = new Color32(0x1E, 0x23, 0x35, 255);
        private static readonly Color EmptySeatColor = new Color32(0x15, 0x18, 0x26, 255);

        private static readonly Color[] SeatColors =
        {
            new Color32(0xE0, 0x7A, 0x2E, 255), new Color32(0x4F, 0x8F, 0xD9, 255), new Color32(0x5F, 0xB8, 0x7A, 255),
            new Color32(0xB5, 0x6C, 0xD6, 255), new Color32(0xD9, 0x5B, 0x6A, 255), new Color32(0xD6, 0xB8, 0x4A, 255),
        };

        private SessionManager sessions;
        private bool busy;

        private GameObject joinPanel;
        private GameObject tablePanel;
        private TMP_InputField nameField;
        private TMP_InputField codeField;
        private TextMeshProUGUI codeText;
        private TextMeshProUGUI countText;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI waitingText;
        private Button startButton;
        private TextMeshProUGUI startLabel;
        private readonly List<Button> joinButtons = new List<Button>();
        private readonly List<SeatRow> seatRows = new List<SeatRow>();

        private class SeatRow
        {
            public Image background;
            public Image avatar;
            public TextMeshProUGUI initial;
            public TextMeshProUGUI name;
            public GameObject hostBadge;
            public GameObject youBadge;
        }


        private void Start()
        {
            sessions = SessionManager.GetOrCreate();
            sessions.OnSessionChanged += Refresh;
            sessions.OnStatus += ShowStatus;

            EnsureCameraAndEventSystem();
            Transform canvas = BuildCanvas();
            BuildHeader(canvas);
            BuildJoinPanel(canvas);
            BuildTablePanel(canvas);
            BuildStatusBar(canvas);
            ShowStatus("Host a table, or type the code a friend gives you.");

            Refresh();
        }

        private void OnDestroy()
        {
            if (sessions == null) return;
            sessions.OnSessionChanged -= Refresh;
            sessions.OnStatus -= ShowStatus;
        }

        // ---------- Buttons ----------

        private async void Host()
        {
            if (busy) return;
            SetBusy(true);
            await sessions.HostAsync(nameField.text);
            SetBusy(false);
        }

        private async void Join()
        {
            if (busy) return;
            SetBusy(true);
            await sessions.JoinAsync(codeField.text, nameField.text);
            SetBusy(false);
        }

        private async void Leave()
        {
            if (busy) return;
            SetBusy(true);
            await sessions.LeaveAsync();
            SetBusy(false);
        }

        private async void BackToMenu()
        {
            if (sessions.InSession) await sessions.LeaveAsync();
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void PlayOffline()
        {
            if (sessions.InSession) return;
            SessionManager.SavedPlayerName = nameField.text;
            SceneManager.LoadScene(offlineGameSceneName);
        }

        private void CopyCode()
        {
            GUIUtility.systemCopyBuffer = sessions.JoinCode;
            ShowStatus($"Copied {sessions.JoinCode}.");
        }

        private void SetBusy(bool value)
        {
            busy = value;
            foreach (Button b in joinButtons) b.interactable = !value;
        }

        // ---------- Refresh ----------

        private void Refresh()
        {
            if (joinPanel == null) return;

            bool seated = sessions.InSession;
            joinPanel.SetActive(!seated);
            tablePanel.SetActive(seated);
            if (!seated) return;

            List<SessionManager.SeatInfo> seats = sessions.GetSeats();
            codeText.text = sessions.JoinCode;
            int bots = Mathf.Max(0, SessionManager.MinPlayers - seats.Count);
            countText.text = bots > 0
                ? $"{seats.Count} of {SessionManager.MaxPlayers} seated  ·  {bots} bot{(bots == 1 ? "" : "s")} will fill the empty seats"
                : $"{seats.Count} of {SessionManager.MaxPlayers} seated";

            for (int i = 0; i < seatRows.Count; i++)
            {
                SeatRow row = seatRows[i];
                bool taken = i < seats.Count;
                row.background.color = taken ? SeatColor : EmptySeatColor;
                row.hostBadge.SetActive(taken && seats[i].isHost);
                row.youBadge.SetActive(taken && seats[i].isYou);

                if (taken)
                {
                    SessionManager.SeatInfo seat = seats[i];
                    row.avatar.color = SeatColors[i % SeatColors.Length];
                    row.initial.text = seat.name.Substring(0, 1).ToUpperInvariant();
                    row.initial.color = Color.white;
                    row.name.text = seat.name;
                    row.name.color = Color.white;
                }
                else
                {
                    bool bot = i < SessionManager.MinPlayers;
                    row.avatar.color = new Color32(0x24, 0x28, 0x38, 255);
                    row.initial.text = bot ? "B" : "";
                    row.initial.color = DimText;
                    row.name.text = bot ? "Open seat  <size=70%>a bot will play</size>" : "Open seat";
                    row.name.color = bot ? MutedText : DimText;
                }
            }

            startButton.gameObject.SetActive(sessions.IsHost);
            waitingText.gameObject.SetActive(!sessions.IsHost);
            startLabel.text = bots > 0 ? $"Start game  <size=75%>({seats.Count} + {bots} bot{(bots == 1 ? "" : "s")})</size>" : "Start game";
        }

        private void ShowStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }

        // ---------- UI construction ----------

        private static void EnsureCameraAndEventSystem()
        {
            if (Camera.main == null)
            {
                var cam = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" }.GetComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = SkyTop;
                cam.orthographic = true;
            }

            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
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

            // Sky gradient, then a low sun sinking below the bottom edge
            var sky = NewRect("Sky", go.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var skyImage = sky.gameObject.AddComponent<RawImage>();
            skyImage.texture = SkyTexture();
            skyImage.raycastTarget = false;

            var sun = NewRect("Sun", go.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-700f, -760f), new Vector2(700f, 640f));
            var sunImage = sun.gameObject.AddComponent<RawImage>();
            sunImage.texture = GlowTexture();
            sunImage.color = new Color(SunColor.r, SunColor.g, SunColor.b, 0.55f);
            sunImage.raycastTarget = false;

            return go.transform;
        }

        private void BuildHeader(Transform canvas)
        {
            var title = NewText("Title", canvas, 58, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), Vector2.one, new Vector2(48f, -128f), new Vector2(-48f, -44f));
            title.text = "TEN DAYS TO SUNDOWN";
            title.characterSpacing = 10f;

            var subtitle = NewText("Subtitle", canvas, 22, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), Vector2.one, new Vector2(48f, -168f), new Vector2(-48f, -128f));
            subtitle.text = "ONLINE  TABLE";
            subtitle.characterSpacing = 24f;
            subtitle.color = AccentColor;

            var back = NewRect("Back", canvas, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -96f), new Vector2(220f, -44f));
            joinButtons.Add(MakeButton(back, "‹  Main menu", ButtonColor, BackToMenu, 20));
        }

        private void BuildJoinPanel(Transform canvas)
        {
            var panel = NewPanel("JoinPanel", canvas, new Vector2(-360f, -330f), new Vector2(360f, 290f));
            joinPanel = panel.gameObject;
            var column = Column(panel, 16f, 44f);

            Caption(column, "YOUR NAME");
            nameField = NewInput(column, "Type your name", 16, 68f);
            nameField.text = SessionManager.SavedPlayerName;

            joinButtons.Add(Row(column, 72f, r => MakeButton(r, "Host a new table", AccentColor, Host, 26)));

            OrDivider(column);

            Caption(column, "JOIN A FRIEND'S TABLE");
            Row<object>(column, 68f, r =>
            {
                codeField = NewInput(r, "Code", 8, 68f);
                codeField.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
                codeField.textComponent.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
                codeField.textComponent.characterSpacing = 12f;
                codeField.onSubmit.AddListener(_ => Join());
                joinButtons.Add(MakeButton(r, "Join", ButtonColor, Join, 24));
                return null;
            });

            Spacer(column, 4f);
            joinButtons.Add(Row(column, 52f, r => MakeButton(r, "Play offline with bots", SeatColor, PlayOffline, 20)));
        }

        private void BuildTablePanel(Transform canvas)
        {
            var panel = NewPanel("TablePanel", canvas, new Vector2(-460f, -360f), new Vector2(460f, 300f));
            tablePanel = panel.gameObject;
            var column = Column(panel, 8f, 40f);

            Caption(column, "JOIN CODE  ·  SHARE IT WITH YOUR GROUP");
            Row<object>(column, 96f, r =>
            {
                var codeRect = NewRect("Code", r, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                Rounded(codeRect.gameObject.AddComponent<Image>(), FieldColor);
                codeRect.gameObject.AddComponent<Outline>().effectColor = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.35f);
                codeRect.gameObject.AddComponent<LayoutElement>().flexibleWidth = 3f;
                codeText = NewText("Text", codeRect, 64, FontStyles.Bold, TextAlignmentOptions.Center,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                codeText.color = AccentColor;
                codeText.characterSpacing = 22f;
                MakeButton(r, "Copy", ButtonColor, CopyCode, 22);
                return null;
            });

            countText = Caption(column, "");
            countText.fontStyle = FontStyles.Normal;
            countText.characterSpacing = 0f;
            countText.fontSize = 19;
            countText.color = MutedText;

            for (int i = 0; i < SessionManager.MaxPlayers; i++)
                seatRows.Add(BuildSeatRow(column, i));

            Spacer(column, 6f);
            Row<object>(column, 68f, r =>
            {
                joinButtons.Add(MakeButton(r, "Leave", ButtonColor, Leave, 22));
                startButton = MakeButton(r, "Start game", AccentColor, () => sessions.StartGame(), 26);
                startButton.GetComponent<LayoutElement>().flexibleWidth = 2.4f;
                startLabel = startButton.GetComponentInChildren<TextMeshProUGUI>();

                var waitRect = NewRect("Waiting", r, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                waitRect.gameObject.AddComponent<LayoutElement>().flexibleWidth = 2.4f;
                waitingText = NewText("Text", waitRect, 22, FontStyles.Italic, TextAlignmentOptions.Center,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                waitingText.color = MutedText;
                waitingText.text = "Waiting for the host to start...";
                return null;
            });
        }

        private SeatRow BuildSeatRow(Transform column, int index)
        {
            var row = new SeatRow();
            var rect = NewRect($"Seat{index + 1}", column, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.background = Rounded(rect.gameObject.AddComponent<Image>(), SeatColor, 12);
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 12, 6, 6);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var avatar = NewRect("Avatar", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.avatar = avatar.gameObject.AddComponent<Image>();
            row.avatar.sprite = CircleSprite();
            var avatarSize = avatar.gameObject.AddComponent<LayoutElement>();
            avatarSize.preferredWidth = avatarSize.preferredHeight = 32f;
            row.initial = NewText("Initial", avatar, 20, FontStyles.Bold, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            row.name = NewText("Name", rect, 24, FontStyles.Normal, TextAlignmentOptions.MidlineLeft,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            row.hostBadge = Badge(rect, "HOST", AccentColor);
            row.youBadge = Badge(rect, "YOU", YouColor);
            return row;
        }

        private void BuildStatusBar(Transform canvas)
        {
            var bar = NewRect("Status", canvas, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-520f, 28f), new Vector2(520f, 76f));
            Rounded(bar.gameObject.AddComponent<Image>(), new Color(0f, 0f, 0f, 0.35f), 24);
            statusText = NewText("Text", bar, 20, FontStyles.Normal, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(24f, 0f), new Vector2(-24f, 0f));
            statusText.color = MutedText;
        }

        // ---------- Small builders ----------

        private static RectTransform NewPanel(string name, Transform parent, Vector2 offsetMin, Vector2 offsetMax)
        {
            var panel = NewRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), offsetMin, offsetMax);
            Rounded(panel.gameObject.AddComponent<Image>(), PanelColor, 24);
            panel.gameObject.AddComponent<Outline>().effectColor = PanelEdge;
            var shadow = panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(0f, -10f);
            return panel;
        }

        private static RectTransform Column(RectTransform panel, float spacing, float padding)
        {
            var column = NewRect("Column", panel, Vector2.zero, Vector2.one, new Vector2(padding, padding), new Vector2(-padding, -padding));
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return column;
        }

        private static T Row<T>(Transform parent, float height, System.Func<RectTransform, T> fill)
        {
            var row = NewRect("Row", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            return fill(row);
        }

        private static TextMeshProUGUI Caption(Transform parent, string text)
        {
            var label = NewText("Caption", parent, 16, FontStyles.Bold, TextAlignmentOptions.BottomLeft,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;
            label.characterSpacing = 6f;
            label.color = MutedText;
            label.text = text;
            return label;
        }

        private static void Spacer(Transform parent, float height)
        {
            NewRect("Spacer", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero)
                .gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private static void OrDivider(Transform parent)
        {
            Row<object>(parent, 36f, r =>
            {
                r.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
                r.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = false;
                Line(r);
                var or = NewText("Or", r, 16, FontStyles.Bold, TextAlignmentOptions.Center,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var orSize = or.gameObject.AddComponent<LayoutElement>();
                orSize.preferredWidth = 48f;
                orSize.flexibleWidth = 0f;
                or.color = DimText;
                or.text = "OR";
                Line(r);
                return null;
            });
        }

        private static void Line(Transform parent)
        {
            var line = NewRect("Line", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            line.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            var size = line.gameObject.AddComponent<LayoutElement>();
            size.preferredHeight = 2f;
            size.flexibleWidth = 1f;
        }

        private static GameObject Badge(Transform parent, string text, Color color)
        {
            var rect = NewRect(text + "Badge", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Rounded(rect.gameObject.AddComponent<Image>(), new Color(color.r, color.g, color.b, 0.18f), 12);
            var size = rect.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = 72f;
            size.preferredHeight = 28f;
            var label = NewText("Label", rect, 14, FontStyles.Bold, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.characterSpacing = 6f;
            label.color = color;
            label.text = text;
            return rect.gameObject;
        }

        private static TMP_InputField NewInput(Transform parent, string hint, int maxLength, float height)
        {
            var root = NewRect("Input", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image background = Rounded(root.gameObject.AddComponent<Image>(), Color.white, 14);
            root.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.07f);
            var element = root.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.flexibleWidth = 2f;

            var area = NewRect("Text Area", root, Vector2.zero, Vector2.one, new Vector2(20f, 6f), new Vector2(-20f, -6f));
            area.gameObject.AddComponent<RectMask2D>();

            var placeholder = NewText("Placeholder", area, 26, FontStyles.Italic, TextAlignmentOptions.MidlineLeft,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            placeholder.text = hint;
            placeholder.color = DimText;

            var text = NewText("Text", area, 26, FontStyles.Normal, TextAlignmentOptions.MidlineLeft,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var field = root.gameObject.AddComponent<TMP_InputField>();
            field.targetGraphic = background;
            field.colors = Tints(FieldColor);
            field.textViewport = area;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.characterLimit = maxLength;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.caretColor = AccentColor;
            field.customCaretColor = true;
            field.selectionColor = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.35f);
            return field;
        }

        private static Button MakeButton(Transform parent, string text, Color color, UnityEngine.Events.UnityAction onClick, float size)
        {
            var rect = NewRect(text, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image background = Rounded(rect.gameObject.AddComponent<Image>(), Color.white, 14);
            rect.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.colors = Tints(color);
            button.onClick.AddListener(onClick);

            var label = NewText("Label", rect, size, FontStyles.Bold, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
            label.text = text;
            return button;
        }

        /// <summary>Hover lightens, press darkens, disabled fades: the graphic itself stays white so the tint is the colour.</summary>
        private static ColorBlock Tints(Color color)
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
            colors.selectedColor = color;
            colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            colors.disabledColor = new Color(color.r, color.g, color.b, 0.4f);
            colors.fadeDuration = 0.08f;
            return colors;
        }

        private static Image Rounded(Image image, Color color, float cornerScale = 24)
        {
            image.sprite = RoundedSprite();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 32f / cornerScale;
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, float size, FontStyles style,
            TextAlignmentOptions align, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var label = NewRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax).gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = align;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }
    }
}

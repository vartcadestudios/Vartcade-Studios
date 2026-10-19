using TMPro;
using UnityEngine;
using TDTS.Managers;

namespace TDTS.UI
{
    /// <summary>
    /// Shows/hides board elements based on live GameManager state:
    /// Day buttons during DayPhase, Night buttons during NightPhase,
    /// the Rondo dial (RondoDial), and the "eyes closed"
    /// overlay while another player takes a hidden turn (spec C1).
    /// </summary>
    public class BoardController : MonoBehaviour
    {
        [Header("Board Elements (assign in Inspector)")]
        [SerializeField] private GameObject nightButtons;  // Steal / Swap / End turn
        [SerializeField] private GameObject dayButtons;    // Trade / Scavenge / Play card
        [SerializeField] private GameObject rondoWheel;    // the slot RondoDial builds into
        [SerializeField] private GameObject blackoutOverlay;

        [SerializeField] private float overlayDelaySeconds = 1.6f;

        private TextMeshProUGUI overlayLabel;
        private bool wasLocalTurn;
        private float showOverlayAt;

        private void Awake()
        {
            gameObject.AddComponent<TableBackdrop>();
            // The dial lives in the old wheel's slot and shows itself, covered until the Rondo wakes
            if (rondoWheel != null)
            {
                rondoWheel.SetActive(true);
                RondoDial.Attach(rondoWheel);
            }
        }

        private void Start()
        {
            if (GameView.Current == null) return;

            if (blackoutOverlay != null) overlayLabel = CreateOverlayLabel(blackoutOverlay.transform);

            GameView.Current.OnStateChanged += HandleStateChanged;
            GameView.Current.OnTurnChanged += Refresh;
            Refresh(); // GameManager may already have changed state before we subscribed
        }

        private void OnDestroy()
        {
            if (GameView.Current == null) return;
            GameView.Current.OnStateChanged -= HandleStateChanged;
            GameView.Current.OnTurnChanged -= Refresh;
        }

        private void HandleStateChanged(GameState previous, GameState next) => Refresh();

        private void Refresh()
        {
            IGameView gm = GameView.Current;
            GameState state = gm.CurrentState;

            dayButtons.SetActive(state == GameState.DayPhase);
            nightButtons.SetActive(state == GameState.NightPhase);

            // Eyes closed: hide the table while someone else takes a Night or Blackout turn.
            // Right after your own turn, wait a moment so you can still see your last peek.
            if (wasLocalTurn && !gm.IsLocalTurn) showOverlayAt = Time.time + overlayDelaySeconds;
            wasLocalTurn = gm.IsLocalTurn;

            bool hidden = gm.EyesClosed && gm.ActivePlayer != null && !gm.IsLocalTurn;
            if (hidden && Time.time < showOverlayAt)
            {
                hidden = false;
                CancelInvoke(nameof(Refresh));
                Invoke(nameof(Refresh), showOverlayAt - Time.time);
            }
            if (blackoutOverlay != null) blackoutOverlay.SetActive(hidden);
            if (hidden && overlayLabel != null)
            {
                string phase = state == GameState.NightPhase ? "Night" : "Blackout";
                overlayLabel.text =
                    $"<size=60>Eyes closed</size>\n\n{gm.ActivePlayer.playerName} is taking their {phase} turn...\n" +
                    $"<size=70%>Day {gm.CurrentDay} of {gm.SundownDay}</size>";
            }
        }

        public static string RondoName(RondoResult result) => result switch
        {
            RondoResult.TradingOnly => "Trading Only",
            RondoResult.HungerDisaster => "Hunger Disaster",
            RondoResult.DoomsdayClock => "Doomsday Clock",
            RondoResult.PeacefulDay => "Peaceful Day",
            _ => result.ToString()
        };

        private static TextMeshProUGUI CreateOverlayLabel(Transform overlay)
        {
            var go = new GameObject("OverlayText", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(overlay, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var label = SundownFonts.Use(go.GetComponent<TextMeshProUGUI>());
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 36;
            label.color = new Color32(0xE8, 0xE4, 0xDA, 255);
            label.raycastTarget = false;
            return label;
        }
    }
}

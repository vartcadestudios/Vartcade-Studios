using System.Text;
using TMPro;
using UnityEngine;
using TDTS.Managers;

namespace TDTS.UI
{
    /// <summary>Shows the last few things that happened this phase, plus actions left.</summary>
    public class PlaySpaceController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private string emptyText = "Play Space";
        [SerializeField] private int maxLines = 4;

        [Tooltip("The scene box is card-sized; the log needs room for whole sentences.")]
        [SerializeField] private Vector2 boxSize = new Vector2(360f, 200f);

        private void Start()
        {
            FitLogToBox();

            if (GameManager.Instance == null)
            {
                label.text = emptyText;
                return;
            }
            GameManager.Instance.OnPlayLogChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayLogChanged -= Refresh;
        }

        private void Refresh()
        {
            GameManager gm = GameManager.Instance;
            var log = gm.PlayLog;
            var sb = new StringBuilder();

            if (log.Count == 0)
            {
                sb.Append(emptyText);
            }
            else
            {
                int start = Mathf.Max(0, log.Count - maxLines);
                for (int i = start; i < log.Count; i++)
                {
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append(log[i]);
                }
            }

            if (gm.LimitActions)
                sb.Append($"\n<b>Actions left: {gm.ActionsLeft}</b>");

            label.text = sb.ToString();
        }

        /// <summary>Widens the box and keeps the text inside it: smaller wrapped lines, shrinking if needed.</summary>
        private void FitLogToBox()
        {
            ((RectTransform)transform).sizeDelta = boxSize;

            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(14f, 10f);
            rect.offsetMax = new Vector2(-14f, -10f);

            label.textWrappingMode = TextWrappingModes.Normal;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = 30f;
            label.overflowMode = TextOverflowModes.Truncate;
            label.alignment = TextAlignmentOptions.TopLeft;
        }
    }
}
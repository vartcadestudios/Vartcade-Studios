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

        private void Start()
        {
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
                sb.Append($"\n\nActions left: {gm.ActionsLeft}");

            label.text = sb.ToString();
        }
    }
}
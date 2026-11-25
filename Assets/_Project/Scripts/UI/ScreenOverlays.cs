using UnityEngine;
using UnityEngine.UI;
using TDTS.Managers;
using static TDTS.UI.SundownArt;

namespace TDTS.UI
{
    /// <summary>
    /// The v3 screen filters, laid over the scene but under the HUD so text stays crisp:
    /// - OV10 warm balance: a light warm wash that ties the colours together (always).
    /// - OV7 storybook paper: paper fibre over everything (always).
    /// - OV3 dusk: a purple-to-orange wash that grows as Sundown nears (table only).
    /// - OV8 night: dark blue everywhere except the lamp (table only, at Night).
    /// Settings > Screen filters: All, Light (warm + paper only) or Off.
    /// </summary>
    public class ScreenOverlays : MonoBehaviour
    {
        private Image warm, paper, dusk, nightShade;
        private bool table;
        private float night;

        public static ScreenOverlays Create(Transform parent, bool tableScene)
        {
            if (!ArtKit.Has("Overlays/ov_paper")) return null;
            var root = NewRect("ScreenOverlays", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var overlays = root.gameObject.AddComponent<ScreenOverlays>();
            overlays.table = tableScene;
            overlays.Build(root);
            return overlays;
        }

        private void Build(Transform root)
        {
            if (table)
            {
                dusk = ArtKit.Full(root, "OV3 Dusk", ArtKit.Get("Overlays/ov_dusk"), new Color(1f, 1f, 1f, 0f));
                nightShade = ArtKit.Full(root, "OV8 Night", ArtKit.Get("Overlays/ov_night"), new Color(1f, 1f, 1f, 0f));
            }
            warm = ArtKit.Full(root, "OV10 Warm", null, new Color(1f, 0.66f, 0.36f, 0.08f));
            paper = ArtKit.Full(root, "OV7 Paper", ArtKit.Get("Overlays/ov_paper"));
            paper.type = Image.Type.Tiled;
            paper.pixelsPerUnitMultiplier = 1f;
        }

        private void Update()
        {
            GameSettings.Filters mode = GameSettings.ScreenFilters;
            bool on = mode != GameSettings.Filters.Off, all = mode == GameSettings.Filters.All;
            warm.enabled = paper.enabled = on;
            paper.color = new Color(1f, 1f, 1f, 0.5f);
            if (!table) return;

            IGameView view = GameView.Current;
            float progress = 0f;
            bool isNight = false;
            if (view != null)
            {
                progress = Mathf.Clamp01((view.CurrentDay - 1f) / Mathf.Max(1f, view.SundownDay - 1f));
                isNight = view.EyesClosed;
            }
            night = Mathf.MoveTowards(night, isNight ? 1f : 0f, Time.unscaledDeltaTime / 1.2f);
            dusk.enabled = nightShade.enabled = all;
            dusk.color = new Color(1f, 1f, 1f, 0.06f + 0.16f * progress);
            nightShade.color = new Color(1f, 1f, 1f, 0.75f * night);
        }
    }
}

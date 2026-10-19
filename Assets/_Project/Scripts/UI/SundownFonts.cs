using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TDTS.UI
{
    /// <summary>
    /// The game's two typefaces, both free fonts that ship with Unity's TextMesh Pro examples:
    /// Roboto for everything you read, Anton (a tall poster face) for titles.
    /// Built at runtime from the .ttf files in Resources/Fonts, so there's no font asset to bake.
    /// Every scene's text is switched over as it loads; text made in code calls Use().
    /// </summary>
    public static class SundownFonts
    {
        private static TMP_FontAsset body;
        private static TMP_FontAsset title;

        public static TMP_FontAsset Body => body != null ? body : body = Build("Fonts/Roboto-Regular", "Fonts/Roboto-Bold");
        public static TMP_FontAsset Title => title != null ? title : title = Build("Fonts/Anton", null);

        /// <summary>Puts a label made in code on the body font.</summary>
        public static TextMeshProUGUI Use(TextMeshProUGUI label)
        {
            if (Body != null) label.font = Body;
            return label;
        }

        /// <summary>Puts a heading on the title font.</summary>
        public static TextMeshProUGUI UseTitle(TextMeshProUGUI label)
        {
            if (Title != null) label.font = Title;
            label.fontStyle &= ~FontStyles.Bold; // Anton is heavy already; faux bold only smears it
            return label;
        }

        private static TMP_FontAsset Build(string regularPath, string boldPath)
        {
            Font regular = Resources.Load<Font>(regularPath);
            if (regular == null)
            {
                Debug.LogWarning($"[Fonts] Missing Resources/{regularPath}.ttf");
                return null;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(regular);
            asset.name = regular.name;

            // A real bold face for <b> and FontStyles.Bold, instead of a smeared regular
            Font bold = boldPath != null ? Resources.Load<Font>(boldPath) : null;
            if (bold != null) asset.fontWeightTable[7].regularTypeface = TMP_FontAsset.CreateFontAsset(bold);

            // Anything these fonts lack (rare symbols) falls back to TMP's default font
            if (TMP_Settings.defaultFontAsset != null)
                asset.fallbackFontAssetTable = new List<TMP_FontAsset> { TMP_Settings.defaultFontAsset };
            return asset;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void WatchScenes()
        {
            SceneManager.sceneLoaded -= SwitchSceneText;
            SceneManager.sceneLoaded += SwitchSceneText;
        }

        // Text placed in scenes still uses TMP's default font; move it over (any other font was a choice, so keep it)
        private static void SwitchSceneText(Scene scene, LoadSceneMode mode)
        {
            if (Body == null) return;
            TMP_FontAsset original = TMP_Settings.defaultFontAsset;
            foreach (TextMeshProUGUI label in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
                if (label.font == null || label.font == original) label.font = Body;
        }
    }
}

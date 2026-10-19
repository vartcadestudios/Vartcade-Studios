using UnityEngine;

namespace TDTS.Managers
{
    /// <summary>
    /// Player settings, saved on this device and applied as soon as the game starts.
    /// </summary>
    public static class GameSettings
    {
        public enum BotSpeed { Slow, Normal, Fast }

        private const string VolumeKey = "settings.volume";
        private const string FullscreenKey = "settings.fullscreen";
        private const string BotSpeedKey = "settings.botSpeed";

        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 0.8f);
            set
            {
                PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
                AudioListener.volume = Volume;
            }
        }

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
                Screen.fullScreen = value;
            }
        }

        public static BotSpeed Bots
        {
            get => (BotSpeed)PlayerPrefs.GetInt(BotSpeedKey, (int)BotSpeed.Normal);
            set => PlayerPrefs.SetInt(BotSpeedKey, (int)value);
        }

        /// <summary>How long a bot "thinks" before each move, scaled by the Bot speed setting.</summary>
        public static float BotThinkSeconds(float normalSeconds) => Bots switch
        {
            BotSpeed.Slow => normalSeconds * 1.8f,
            BotSpeed.Fast => normalSeconds * 0.4f,
            _ => normalSeconds,
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplySaved()
        {
            AudioListener.volume = Volume;
            if (!Application.isEditor) Screen.fullScreen = Fullscreen;
        }
    }
}

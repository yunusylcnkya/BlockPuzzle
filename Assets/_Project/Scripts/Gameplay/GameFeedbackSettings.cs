
using System;
using UnityEngine;

namespace BlockPuzzle.Gameplay
{
    public static class GameFeedbackSettings
    {
        private const string SoundKey =
            "BlockPuzzle.Settings.SoundEnabled";

        private const string VolumeKey =
            "BlockPuzzle.Settings.SfxVolume";

        private const string HapticsKey =
            "BlockPuzzle.Settings.HapticsEnabled";

        public static event Action SettingsChanged;

        public static bool SoundEnabled
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(SoundKey, value ? 1 : 0);
                PlayerPrefs.Save();
                SettingsChanged?.Invoke();
            }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 0.7f);
            set
            {
                PlayerPrefs.SetFloat(
                    VolumeKey,
                    Mathf.Clamp01(value)
                );

                PlayerPrefs.Save();
                SettingsChanged?.Invoke();
            }
        }

        public static bool HapticsEnabled
        {
            get => PlayerPrefs.GetInt(HapticsKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(
                    HapticsKey,
                    value ? 1 : 0
                );

                PlayerPrefs.Save();
                SettingsChanged?.Invoke();
            }
        }
    }
}

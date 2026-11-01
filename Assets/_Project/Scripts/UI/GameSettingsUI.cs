
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Gameplay;

namespace BlockPuzzle.UI
{
    public sealed class GameSettingsUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Controls")]
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle hapticsToggle;
        [SerializeField] private TMP_Text volumeValueText;

        private void Awake()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (soundToggle != null)
            {
                soundToggle.onValueChanged.AddListener(
                    SetSoundEnabled
                );
            }

            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.AddListener(
                    SetVolume
                );
            }

            if (hapticsToggle != null)
            {
                hapticsToggle.onValueChanged.AddListener(
                    SetHapticsEnabled
                );
            }
        }

        private void OnDisable()
        {
            if (soundToggle != null)
            {
                soundToggle.onValueChanged.RemoveListener(
                    SetSoundEnabled
                );
            }

            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.RemoveListener(
                    SetVolume
                );
            }

            if (hapticsToggle != null)
            {
                hapticsToggle.onValueChanged.RemoveListener(
                    SetHapticsEnabled
                );
            }
        }

        public void OpenSettings()
        {
            RefreshControls();

            if (settingsPanel != null)
                settingsPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void RefreshControls()
        {
            if (soundToggle != null)
            {
                soundToggle.SetIsOnWithoutNotify(
                    GameFeedbackSettings.SoundEnabled
                );
            }

            if (volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(
                    GameFeedbackSettings.SfxVolume
                );
            }

            if (hapticsToggle != null)
            {
                hapticsToggle.SetIsOnWithoutNotify(
                    GameFeedbackSettings.HapticsEnabled
                );
            }

            UpdateVolumeLabel();
        }

        public void SetSoundEnabled(bool enabled)
        {
            GameFeedbackSettings.SoundEnabled = enabled;
        }

        public void SetVolume(float volume)
        {
            GameFeedbackSettings.SfxVolume = volume;
            UpdateVolumeLabel();
        }

        public void SetHapticsEnabled(bool enabled)
        {
            GameFeedbackSettings.HapticsEnabled = enabled;
        }

        private void UpdateVolumeLabel()
        {
            if (volumeValueText == null)
                return;

            int percentage = Mathf.RoundToInt(
                GameFeedbackSettings.SfxVolume * 100f
            );

            volumeValueText.text = $"{percentage}%";
        }
    }
}

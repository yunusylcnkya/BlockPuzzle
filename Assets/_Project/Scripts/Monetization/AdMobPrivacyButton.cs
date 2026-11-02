
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Monetization
{
    public sealed class AdMobPrivacyButton : MonoBehaviour
    {
        [SerializeField] private Button privacyButton;

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (privacyButton == null)
                return;

            bool required =
                AdMobManager.Instance != null &&
                AdMobManager.Instance.CanShowPrivacyOptions;

            if (privacyButton.gameObject.activeSelf != required)
            {
                privacyButton.gameObject.SetActive(required);

                Debug.Log(
                    "[AdMob Privacy] Required: " + required
                );
            }
        }

        public void OpenPrivacyOptions()
        {
            AdMobManager.Instance?.ShowPrivacyOptions();
        }
    }
}

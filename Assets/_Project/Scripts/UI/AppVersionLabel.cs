
using TMPro;
using UnityEngine;

namespace BlockPuzzle.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class AppVersionLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text versionText;

        [SerializeField] private string prefix = "v";

        private void Reset()
        {
            versionText = GetComponent<TMP_Text>();
        }

        private void Awake()
        {
            if (versionText == null)
                versionText = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            RefreshVersion();
        }

        public void RefreshVersion()
        {
            if (versionText == null)
                return;

            versionText.text =
                prefix + Application.version;
        }
    }
}

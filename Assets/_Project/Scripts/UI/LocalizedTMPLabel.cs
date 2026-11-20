using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace BlockPuzzle.UI
{
    /// <summary>
    /// Localizes an otherwise static TMP label.
    /// Use only on labels NOT overwritten by gameplay scripts.
    /// Unity Localization manages loading and updates after language changes.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedTMPLabel : MonoBehaviour
    {
        [SerializeField] private string tableName = "GameUI";
        [SerializeField] private string entryKey;

        private TMP_Text label;
        private LocalizedString localized;

        private void Awake()
        {
            label = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(entryKey)) return;
            if (label == null) label = GetComponent<TMP_Text>();
            localized = new LocalizedString(tableName, entryKey);
            localized.StringChanged += OnStringChanged;
        }

        private void OnDisable()
        {
            if (localized == null) return;
            localized.StringChanged -= OnStringChanged;
            localized = null;
        }

        private void OnStringChanged(string value)
        {
            if (label != null && value != null)
                label.text = value;
        }
    }
}

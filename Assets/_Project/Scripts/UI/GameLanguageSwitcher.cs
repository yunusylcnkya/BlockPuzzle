using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace BlockPuzzle.UI
{
    /// <summary>
    /// Attach to a SettingsPanel GameObject and connect the two language
    /// buttons to SelectTurkish and SelectEnglish through Button.onClick.
    ///
    /// Persistence is intentionally delegated to Unity Localization's
    /// Player Pref Locale Selector in Project Settings > Localization.
    /// Configure that selector before the System Locale Selector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameLanguageSwitcher : MonoBehaviour
    {
        private Coroutine localeChange;

        public void SelectTurkish() => ChangeLanguage("tr");
        public void SelectEnglish() => ChangeLanguage("en");

        private void ChangeLanguage(string code)
        {
            if (localeChange != null)
                StopCoroutine(localeChange);

            localeChange = StartCoroutine(ApplyLanguage(code));
        }

        private IEnumerator ApplyLanguage(string code)
        {
            // Locale tables and available locales are initialized asynchronously.
            yield return LocalizationSettings.InitializationOperation;

            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(
                new LocaleIdentifier(code));

            if (locale == null)
            {
                Debug.LogError("[Language] Locale is not configured: " + code);
                localeChange = null;
                yield break;
            }

            LocalizationSettings.SelectedLocale = locale;
            Debug.Log("[Language] Selected locale: " + locale.Identifier.Code);
            localeChange = null;
        }

        private void OnDisable()
        {
            if (localeChange == null) return;
            StopCoroutine(localeChange);
            localeChange = null;
        }
    }
}

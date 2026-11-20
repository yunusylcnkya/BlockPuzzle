using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace BlockPuzzle.UI
{
    /// <summary>
    /// Localizes script-driven UI text AFTER the original game code updates it.
    /// No score/coins/ads/undo/persistence logic is changed. Only attach to
    /// TMP fields that are intentionally rewritten by gameplay scripts.
    /// Original text is retained for switching back to English.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedDynamicLabel : MonoBehaviour
    {
        private TMP_Text label;
        private string sourceEnglish;
        private string lastRendered;
        private string lastLocale;

        private void OnEnable()
        {
            label = GetComponent<TMP_Text>();
            sourceEnglish = label != null ? label.text : null;
            lastRendered = null;
            lastLocale = null;
            LocalizationSettings.SelectedLocaleChanged += LocaleChanged;
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= LocaleChanged;
        }

        private void LocaleChanged(Locale locale)
        {
            lastLocale = null;
            Refresh();
        }

        private void LateUpdate()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (label == null) return;
            // Script-driven labels can be overwritten any time; re-translate
            // only when the original value or the selected language changes.
            if (lastRendered != label.text)
                sourceEnglish = label.text;

            Locale locale = LocalizationSettings.SelectedLocale;
            string code = locale != null ? locale.Identifier.Code : "en";
            if (code == lastLocale && lastRendered == label.text) return;
            lastLocale = code;

            string translated = code == "tr"
                ? Translator.TranslateTurkish(sourceEnglish ?? string.Empty)
                : sourceEnglish ?? string.Empty;
            if (label.text != translated) label.text = translated;
            lastRendered = translated;
        }

        private static class Translator
        {
            private sealed class Pair
            {
                public readonly string Key;
                public readonly string Turkish;
                public Pair(string key, string turkish)
                { Key = key; Turkish = turkish; }
            }

            private static readonly Dictionary<string, Pair> Exact =
                new Dictionary<string, Pair>(StringComparer.Ordinal)
            {
                { "AD READY", new Pair("ad.ready", "REKLAM HAZIR") },
                { "BEST", new Pair("game.best", "EN YÜKSEK") },
                { "REPLACE REMAINING BLOCKS", new Pair("shop.reroll_desc", "KALAN BLOKLARI DEĞİŞTİR") },
                { "WATCH AD & CONTINUE", new Pair("game.continue_ad", "REKLAM İZLE VE DEVAM ET") },
                { "AD NOT READY", new Pair("ad.not_ready", "REKLAM HAZIR DEĞİL") },
                { "LOADING AD...", new Pair("ad.loading", "REKLAM YÜKLENİYOR...") },
                { "REROLL LIMIT REACHED", new Pair("shop.limit", "KARIŞTIRMA SINIRINA ULAŞILDI") },
                { "NO AVAILABLE REROLL", new Pair("shop.no_reroll", "KARIŞTIRILACAK BLOK YOK") },
                { "NOT ENOUGH COINS", new Pair("shop.not_enough_coins", "YETERSİZ JETON") },
                { "PURCHASE NOT COMPLETED", new Pair("shop.purchase_failed", "SATIN ALMA TAMAMLANAMADI") },
                { "NO MOVE TO UNDO", new Pair("shop.no_undo", "GERİ ALINACAK HAMLE YOK") },
                { "UNDO NOT COMPLETED", new Pair("shop.undo_failed", "GERİ ALMA TAMAMLANAMADI") },
                { "EXTRA MOVES UNAVAILABLE", new Pair("shop.no_extra", "EK HAMLE KULLANILAMIYOR") },
                { "WATCH AD - UNDO", new Pair("classic.undo_ad", "REKLAM İZLE - GERİ AL") },
                { "WATCH AD & CONTINUE + 3 BLOCKS", new Pair("classic.continue_ad3", "REKLAM İZLE, 3 BLOKLA DEVAM ET") },
                { "LEVEL COMPLETE!", new Pair("game.level_complete", "BÖLÜM TAMAMLANDI!") },
                { "LEVEL FAILED", new Pair("game.level_failed", "BÖLÜM BAŞARISIZ") },
                { "OBJECTIVE COMPLETED", new Pair("game.objective_completed", "HEDEF TAMAMLANDI") },
                { "NO MOVES LEFT", new Pair("game.no_moves_left", "HAMLE HAKKI KALMADI") },
                { "NO AVAILABLE PLACEMENTS", new Pair("game.no_available_placements", "YERLEŞTİRİLEBİLECEK BLOK YOK") },
                { "REWARD ALREADY CLAIMED", new Pair("game.reward_already_claimed", "ÖDÜL ÖNCEDEN ALINDI") },
                { "REACH SCORE", new Pair("objective.reach_score", "HEDEF PUANA ULAŞ") },
                { "CLEAR LINES", new Pair("objective.clear_lines", "SATIR/SÜTUN TEMİZLE") },
                { "PLACE BLOCKS", new Pair("objective.place_blocks", "BLOK YERLEŞTİR") },
                { "CLEAR OBSTACLES", new Pair("objective.clear_obstacles", "ENGELLERİ TEMİZLE") },
                { "CLEAR ROWS", new Pair("objective.clear_rows", "SATIR TEMİZLE") },
                { "CLEAR COLUMNS", new Pair("objective.clear_columns", "SÜTUN TEMİZLE") },
                { "OBJECTIVE", new Pair("objective.default", "HEDEF") }
            };

            private static readonly RegexOptions Options = RegexOptions.CultureInvariant;
            private static readonly Regex Level = new Regex(@"^LEVEL\s+(\d+)\s+—\s+(.+)$", Options);
            private static readonly Regex Unlocked = new Regex(@"^LEVEL\s+(\d+)\s+UNLOCKED$", Options);
            private static readonly Regex Moves = new Regex(@"^MOVES\s+(.+?)\s*/\s*(.+)$", Options);
            private static readonly Regex BestResult = new Regex(@"^BEST SCORE:\s*(.*?)\s*\nBEST MOVES:\s*(.+)$", Options);
            private static readonly Regex Score = new Regex(@"^SCORE\s+(.+)$", Options);
            private static readonly Regex Best = new Regex(@"^BEST\s+(.+)$", Options);
            private static readonly Regex CoinCount = new Regex(@"^COINS:\s*(.+)$", Options);
            private static readonly Regex CoinsReceived = new Regex(@"^\+(.+?)\s+COINS RECEIVED!$", Options);
            private static readonly Regex PlusCoins = new Regex(@"^\+(.+?)\s+COINS$", Options);
            private static readonly Regex CoinPrice = new Regex(@"^(.+?)\s+COINS$", Options);
            private static readonly Regex RerollUsage = new Regex(@"^REPLACE REMAINING BLOCKS\s*\((\d+)\s*/\s*(\d+)\)$", Options);
            private static readonly Regex ExtraMoves = new Regex(@"^\+(.+?)\s+MOVES\s*\((\d+)\s*/\s*(\d+)\)$", Options);
            private static readonly Regex AdShuffle = new Regex(@"^AD SHUFFLE\s*\((\d+)\)$", Options);

            public static string TranslateTurkish(string original)
            {
                if (string.IsNullOrEmpty(original)) return original;
                if (Exact.TryGetValue(original, out Pair pair))
                    return Lookup(pair.Key, pair.Turkish);
                Match m;
                m = BestResult.Match(original);
                if (m.Success) return Format("ui.best_result", "EN YÜKSEK PUAN: {0}\nEN AZ HAMLE: {1}", m.Groups[1].Value, m.Groups[2].Value);
                m = Level.Match(original);
                if (m.Success) return Format("ui.level_name", "BÖLÜM {0} — {1}", m.Groups[1].Value, m.Groups[2].Value);
                m = Unlocked.Match(original);
                if (m.Success) return Format("map.unlocked", "BÖLÜM {0} AÇILDI", m.Groups[1].Value);
                m = Moves.Match(original);
                if (m.Success) return Format("ui.moves_count", "HAMLE {0} / {1}", m.Groups[1].Value, m.Groups[2].Value);
                m = RerollUsage.Match(original);
                if (m.Success) return Format("ui.reroll_usage", "KALAN BLOKLARI DEĞİŞTİR ({0}/{1})", m.Groups[1].Value, m.Groups[2].Value);
                m = ExtraMoves.Match(original);
                if (m.Success) return Format("ui.extra_moves_usage", "+{0} HAMLE ({1}/{2})", m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value);
                m = AdShuffle.Match(original);
                if (m.Success) return Format("classic.shuffle_ad", "REKLAMLA KARIŞTIR ({0})", m.Groups[1].Value);
                m = CoinsReceived.Match(original);
                if (m.Success) return Format("ad.coins_received", "+{0} JETON KAZANILDI!", m.Groups[1].Value);
                m = CoinCount.Match(original);
                if (m.Success) return Format("ui.coins_count", "JETON: {0}", m.Groups[1].Value);
                m = PlusCoins.Match(original);
                if (m.Success) return Format("ui.coins_received", "+{0} JETON", m.Groups[1].Value);
                m = CoinPrice.Match(original);
                if (m.Success) return Format("ui.coin_price", "{0} JETON", m.Groups[1].Value);
                m = Score.Match(original);
                if (m.Success) return Format("ui.score_count", "PUAN {0}", m.Groups[1].Value);
                m = Best.Match(original);
                if (m.Success) return Format("ui.best_count", "EN YÜKSEK {0}", m.Groups[1].Value);
                return original;
            }

            private static string Format(string key, string fallback, params string[] values)
            {
                string text = Lookup(key, fallback);
                for (int i = 0; i < values.Length; i++)
                    text = text.Replace("{" + i + "}", values[i]);
                return text;
            }

            private static string Lookup(string key, string fallback)
            {
                // Use String Table values after initialization. The fallback
                // is intentionally local and avoids any missing-table output.
                if (LocalizationSettings.InitializationOperation.IsDone)
                {
                    string localized = LocalizationSettings.StringDatabase.GetLocalizedString("GameUI", key);
                    if (!string.IsNullOrEmpty(localized) &&
                        !localized.StartsWith("No translation found", StringComparison.Ordinal))
                        return localized;
                }
                return fallback;
            }
        }
    }
}

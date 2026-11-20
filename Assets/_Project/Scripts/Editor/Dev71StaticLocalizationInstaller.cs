#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using BlockPuzzle.UI;

namespace BlockPuzzle.EditorTools
{
    /// <summary>
    /// Dev 7.1a: idempotent localization import + static TMP binding.
    /// Does not change game scripts, gameplay state, saved data or ad logic.
    /// Dynamic labels are intentionally excluded.
    /// </summary>
    public static class Dev71StaticLocalizationInstaller
    {
        private sealed class Translation
        {
            public readonly string Key;
            public readonly string English;
            public readonly string Turkish;

            public Translation(string key, string english, string turkish)
            {
                Key = key; English = english; Turkish = turkish;
            }
        }

        private static readonly Translation[] Values =
        {
            new Translation("menu.classic", "CLASSIC", "KLASİK"),
            new Translation("menu.adventure", "ADVENTURE", "MACERA"),
            new Translation("settings.language", "LANGUAGE", "DİL"),
            new Translation("game.restart", "RESTART", "YENİDEN BAŞLAT"),
            new Translation("game.over", "GAME OVER", "OYUN BİTTİ"),
            new Translation("game.continue_ad", "WATCH AD & CONTINUE", "REKLAM İZLE VE DEVAM ET"),
            new Translation("settings.title", "SETTINGS", "AYARLAR"),
            new Translation("settings.music", "MUSIC", "MÜZİK"),
            new Translation("settings.sound", "SOUND EFFECTS", "SES EFEKTLERİ"),
            new Translation("settings.haptics", "VIBRATION", "TİTREŞİM"),
            new Translation("settings.close", "CLOSE", "KAPAT"),
            new Translation("settings.privacy", "PRIVACY CHOICES", "GİZLİLİK SEÇENEKLERİ"),
            new Translation("nav.main_menu", "MAIN MENU", "ANA MENÜ"),
            new Translation("nav.back", "BACK", "GERİ"),
            new Translation("game.best", "BEST", "EN YÜKSEK"),
            new Translation("game.your_score", "YOUR SCORE", "PUANIN"),
            new Translation("game.next_level", "NEXT LEVEL", "SONRAKİ BÖLÜM"),
            new Translation("game.retry", "RETRY", "TEKRAR DENE"),
            new Translation("game.map", "ADVENTURE MAP", "MACERA HARİTASI"),
            new Translation("game.buy_extra_moves", "BUY +5 MOVES", "+5 HAMLE AL"),
            new Translation("game.level_complete", "LEVEL COMPLETE!", "BÖLÜM TAMAMLANDI!"),
            new Translation("game.level_failed", "LEVEL FAILED", "BÖLÜM BAŞARISIZ"),
            new Translation("game.objective_completed", "OBJECTIVE COMPLETED", "HEDEF TAMAMLANDI"),
            new Translation("game.no_moves_left", "NO MOVES LEFT", "HAMLE HAKKI KALMADI"),
            new Translation("game.no_available_placements", "NO AVAILABLE PLACEMENTS", "YERLEŞTİRİLEBİLECEK BLOK YOK"),
            new Translation("objective.reach_score", "REACH SCORE", "HEDEF PUANA ULAŞ"),
            new Translation("objective.clear_lines", "CLEAR LINES", "SATIR/SÜTUN TEMİZLE"),
            new Translation("objective.place_blocks", "PLACE BLOCKS", "BLOK YERLEŞTİR"),
            new Translation("objective.clear_obstacles", "CLEAR OBSTACLES", "ENGELLERİ TEMİZLE"),
            new Translation("objective.clear_rows", "CLEAR ROWS", "SATIR TEMİZLE"),
            new Translation("objective.clear_columns", "CLEAR COLUMNS", "SÜTUN TEMİZLE"),
            new Translation("objective.default", "OBJECTIVE", "HEDEF"),
            new Translation("shop.title", "COIN SHOP", "JETON MAĞAZASI"),
            new Translation("shop.open", "SHOP", "MAĞAZA"),
            new Translation("shop.undo", "UNDO", "GERİ AL"),
            new Translation("shop.reroll", "REROLL", "KARIŞTIR"),
            new Translation("shop.buy_moves", "BUY MOVES", "HAMLE SATIN AL"),
            new Translation("shop.undo_desc", "UNDO LAST MOVE", "SON HAMLEYİ GERİ AL"),
            new Translation("shop.reroll_desc", "REPLACE REMAINING BLOCKS", "KALAN BLOKLARI DEĞİŞTİR"),
            new Translation("map.free_coins", "FREE COINS", "ÜCRETSİZ JETON"),
            new Translation("map.watch_ad", "WATCH AD", "REKLAM İZLE"),
            new Translation("classic.undo_ad", "WATCH AD - UNDO", "REKLAM İZLE - GERİ AL"),
            new Translation("classic.shuffle_ad", "AD SHUFFLE ({0})", "REKLAMLA KARIŞTIR ({0})"),
            new Translation("classic.continue_ad3", "WATCH AD & CONTINUE + 3 BLOCKS", "REKLAM İZLE, 3 BLOKLA DEVAM ET"),
            new Translation("ad.not_ready", "AD NOT READY", "REKLAM HAZIR DEĞİL"),
            new Translation("ad.loading", "LOADING AD...", "REKLAM YÜKLENİYOR..."),
            new Translation("ad.reward_coins", "+{0} COINS", "+{0} JETON"),
            new Translation("game.coins", "COINS", "JETON"),
            new Translation("game.reward_already_claimed", "REWARD ALREADY CLAIMED", "ÖDÜL ÖNCEDEN ALINDI"),
            new Translation("game.best_score", "BEST SCORE", "EN YÜKSEK PUAN"),
            new Translation("game.best_moves", "BEST MOVES", "EN AZ HAMLE"),
            new Translation("game.score", "SCORE", "PUAN"),
            new Translation("game.level", "LEVEL", "BÖLÜM"),
            new Translation("game.moves", "MOVES", "HAMLE"),
            new Translation("map.unlocked", "LEVEL {0} UNLOCKED", "BÖLÜM {0} AÇILDI"),
        };

        private static readonly Dictionary<string, string> StaticTextKeys =
            new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "SETTINGS", "settings.title" },
            { "MUSIC", "settings.music" },
            { "Sound EFFECTS", "settings.sound" },
            { "SOUND EFFECTS", "settings.sound" },
            { "VIBRATION", "settings.haptics" },
            { "close", "settings.close" },
            { "CLOSE", "settings.close" },
            { "PRIVACY CHOICES", "settings.privacy" },
            { "MAIN MENU", "nav.main_menu" },
            { "BEST", "game.best" },
            { "YOUR SCORE", "game.your_score" },
            { "RESTART", "game.restart" },
            { "GAME OVER", "game.over" },
            { "NEXT LEVEL", "game.next_level" },
            { "RETRY", "game.retry" },
            { "ADVENTURE MAP", "game.map" },
            { "BUY +5 MOVES", "game.buy_extra_moves" },
            { "COIN SHOP", "shop.title" },
            { "SHOP", "shop.open" },
            { "UNDO", "shop.undo" },
            { "REROLL", "shop.reroll" },
            { "BUY MOVES", "shop.buy_moves" },
            { "UNDO LAST MOVE", "shop.undo_desc" },
            { "REPLACE REMAINING BLOCKS", "shop.reroll_desc" },
            { "FREE COINS", "map.free_coins" },
            { "WATCH AD", "map.watch_ad" },
            { "ADVENTURE", "menu.adventure" },
        };

        private static readonly string[] ScenePaths =
        {
            "Assets/_Project/Scenes/MainMenu.unity",
            "Assets/_Project/Scenes/ClassicGame.unity",
            "Assets/_Project/Scenes/AdventureMap.unity",
            "Assets/_Project/Scenes/AdventureGame.unity"
        };

        [MenuItem("Tools/Block Puzzle/v2.0/Dev7.1/1. Audit Translation Tables (Read Only)")]
        public static void AuditTables()
        {
            try
            {
                GetTables(out StringTable english, out StringTable turkish);
                int mismatches = 0;
                foreach (Translation t in Values)
                {
                    var en = english.GetEntry(t.Key);
                    var tr = turkish.GetEntry(t.Key);
                    if (en == null || tr == null ||
                        en.LocalizedValue != t.English || tr.LocalizedValue != t.Turkish)
                    {
                        mismatches++;
                        Debug.LogWarning("[Dev7.1] Missing/wrong translation: " + t.Key);
                    }
                }
                Debug.Log("[Dev7.1] Translation table audit: checked " +
                          Values.Length + ", mismatches=" + mismatches +
                          ". Audit does not write assets.");
            }
            catch (Exception e)
            {
                Debug.LogError("[Dev7.1] Audit error: " + e.Message);
            }
        }

        [MenuItem("Tools/Block Puzzle/v2.0/Dev7.1/2. Fix and Expand GameUI Tables")]
        public static void ImportTranslations()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Dev7.1] Exit Play mode.");
                return;
            }

            if (!EditorUtility.DisplayDialog("Dev7.1 GameUI translations",
                    "Update the existing GameUI English/Turkish translations? " +
                    "This corrects two misplaced entries and adds new UI keys. " +
                    "Use Git to review the changes.", "Update GameUI", "Cancel"))
                return;

            try
            {
                GetTables(out StringTable english, out StringTable turkish);
                int changed = 0;
                Undo.RecordObjects(new UnityEngine.Object[] {
                    english, turkish, english.SharedData
                }, "Update GameUI Localizations");
                foreach (Translation t in Values)
                {
                    var en = english.GetEntry(t.Key);
                    var tr = turkish.GetEntry(t.Key);
                    if (en == null || en.LocalizedValue != t.English)
                    {
                        if (en == null) english.AddEntry(t.Key, t.English);
                        else en.Value = t.English;
                        changed++;
                    }
                    if (tr == null || tr.LocalizedValue != t.Turkish)
                    {
                        if (tr == null) turkish.AddEntry(t.Key, t.Turkish);
                        else tr.Value = t.Turkish;
                        changed++;
                    }
                }
                EditorUtility.SetDirty(english);
                EditorUtility.SetDirty(turkish);
                EditorUtility.SetDirty(english.SharedData);
                AssetDatabase.SaveAssets();
                Debug.Log("[Dev7.1] GameUI import complete. Keys=" +
                          Values.Length + ", changed locale entries=" + changed);
                AuditTables();
            }
            catch (Exception e)
            {
                Debug.LogError("[Dev7.1] Table import failed: " + e);
            }
        }

        [MenuItem("Tools/Block Puzzle/v2.0/Dev7.1/3. Bind Static UI Texts")]
        public static void BindStaticTexts()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Dev7.1] Exit Play mode.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            try
            {
                GetTables(out StringTable english, out StringTable turkish);
                int bound = 0, skipped = 0;
                foreach (string path in ScenePaths)
                {
                    if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                        throw new Exception("Missing scene: " + path);
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
                    {
                        string current = label.text == null ? string.Empty : label.text.Trim();
                        if (!StaticTextKeys.TryGetValue(current, out string key))
                            continue;
                        if (label.GetComponent<LocalizeStringEvent>() != null)
                        {
                            skipped++; // Keep the existing manual GameUI bindings.
                            continue;
                        }
                        LocalizedTMPLabel existing = label.GetComponent<LocalizedTMPLabel>();
                        if (existing != null)
                        {
                            skipped++; // Idempotent on repeated installation.
                            continue;
                        }
                        var component = Undo.AddComponent<LocalizedTMPLabel>(label.gameObject);
                        var serialized = new SerializedObject(component);
                        serialized.FindProperty("tableName").stringValue = "GameUI";
                        serialized.FindProperty("entryKey").stringValue = key;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(component);
                        bound++;
                    }
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new Exception("Cannot save scene: " + scene.name);
                }
                EditorSceneManager.OpenScene(ScenePaths[0], OpenSceneMode.Single);
                Debug.Log("[Dev7.1] Static UI binding: new=" + bound +
                          ", already-bound=" + skipped +
                          ". Dynamic texts will be handled in dev.7.1b.");
            }
            catch (Exception e)
            {
                Debug.LogError("[Dev7.1] Bind failed: " + e);
            }
        }

        private static void GetTables(out StringTable english, out StringTable turkish)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection("GameUI");
            if (collection == null)
                throw new Exception("GameUI collection missing; verify Localization Tables.");
            english = collection.GetTable("en") as StringTable;
            turkish = collection.GetTable("tr") as StringTable;
            if (english == null || turkish == null)
                throw new Exception("GameUI must have en and tr tables.");
        }
    }
}
#endif

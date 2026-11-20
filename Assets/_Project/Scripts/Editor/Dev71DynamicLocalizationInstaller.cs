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
    public static class Dev71DynamicLocalizationInstaller
    {
        private sealed class Entry
        {
            public readonly string Key;
            public readonly string English;
            public readonly string Turkish;
            public Entry(string key, string en, string tr)
            { Key = key; English = en; Turkish = tr; }
        }

        private static readonly Entry[] DynamicKeys =
        {
            new Entry("ad.ready", "AD READY", "REKLAM HAZIR"),
            new Entry("ad.coins_received", "+{0} COINS RECEIVED!", "+{0} JETON KAZANILDI!"),
            new Entry("shop.limit", "REROLL LIMIT REACHED", "KARIŞTIRMA SINIRINA ULAŞILDI"),
            new Entry("shop.no_reroll", "NO AVAILABLE REROLL", "KARIŞTIRILACAK BLOK YOK"),
            new Entry("shop.not_enough_coins", "NOT ENOUGH COINS", "YETERSİZ JETON"),
            new Entry("shop.purchase_failed", "PURCHASE NOT COMPLETED", "SATIN ALMA TAMAMLANAMADI"),
            new Entry("shop.no_undo", "NO MOVE TO UNDO", "GERİ ALINACAK HAMLE YOK"),
            new Entry("shop.undo_failed", "UNDO NOT COMPLETED", "GERİ ALMA TAMAMLANAMADI"),
            new Entry("shop.no_extra", "EXTRA MOVES UNAVAILABLE", "EK HAMLE KULLANILAMIYOR"),
            new Entry("ui.coins_count", "COINS: {0}", "JETON: {0}"),
            new Entry("ui.coin_price", "{0} COINS", "{0} JETON"),
            new Entry("ui.moves_count", "MOVES {0} / {1}", "HAMLE {0} / {1}"),
            new Entry("ui.level_name", "LEVEL {0} — {1}", "BÖLÜM {0} — {1}"),
            new Entry("ui.score_count", "SCORE {0}", "PUAN {0}"),
            new Entry("ui.best_count", "BEST {0}", "EN YÜKSEK {0}"),
            new Entry("ui.best_result", "BEST SCORE: {0}\nBEST MOVES: {1}", "EN YÜKSEK PUAN: {0}\nEN AZ HAMLE: {1}"),
            new Entry("ui.reroll_usage", "REPLACE REMAINING BLOCKS ({0}/{1})", "KALAN BLOKLARI DEĞİŞTİR ({0}/{1})"),
            new Entry("ui.extra_moves_usage", "+{0} MOVES ({1}/{2})", "+{0} HAMLE ({1}/{2})"),
            new Entry("ui.coins_received", "+{0} COINS", "+{0} JETON"),
        };

        private static readonly Dictionary<string, string[]> Targets = new Dictionary<string, string[]>
        {
            { "MainMenu", new[] { "CoinText" } },
            { "ClassicGame", new[] { "BestScoreText" } },
            { "AdventureMap", new[] { "ProgressText", "TotalCoinsText",
                "RewardStatusText", "RewardAmountText" } },
            { "AdventureGame", new[] {
                "LevelTitleText", "ObjectiveText", "MovesText",
                "ResultTitleText", "ResultDescriptionText", "ResultScoreText",
                "ResultBestText", "ResultCoinsText", "ShopBalanceText",
                "RerollPriceText", "UndoPriceText", "ExtraMovesPriceText",
                "RerollDescriptionText", "ExtraMovesDescriptionText", "ShopStatusText" } }
        };

        [MenuItem("Tools/Block Puzzle/v2.0/Dev7.1b/1. Expand Dynamic GameUI Keys")]
        public static void ExpandKeys()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { Debug.LogError("[Dev7.1b] Exit Play Mode."); return; }
            if (!EditorUtility.DisplayDialog("BlockPuzzle Dev7.1b",
                "Add dynamic UI translation keys to existing GameUI en/tr tables?",
                "Add Keys", "Cancel")) return;
            try
            {
                var collection = LocalizationEditorSettings.GetStringTableCollection("GameUI");
                if (collection == null) throw new Exception("GameUI collection not found.");
                StringTable en = collection.GetTable("en") as StringTable;
                StringTable tr = collection.GetTable("tr") as StringTable;
                if (en == null || tr == null) throw new Exception("GameUI en/tr tables missing.");
                Undo.RecordObjects(new UnityEngine.Object[] { en, tr, en.SharedData }, "Add Dynamic UI Keys");
                int changed = 0;
                foreach (Entry e in DynamicKeys)
                {
                    var enEntry = en.GetEntry(e.Key);
                    if (enEntry == null) { en.AddEntry(e.Key, e.English); changed++; }
                    else if (enEntry.Value != e.English) { enEntry.Value = e.English; changed++; }
                    var trEntry = tr.GetEntry(e.Key);
                    if (trEntry == null) { tr.AddEntry(e.Key, e.Turkish); changed++; }
                    else if (trEntry.Value != e.Turkish) { trEntry.Value = e.Turkish; changed++; }
                }
                EditorUtility.SetDirty(en); EditorUtility.SetDirty(tr);
                EditorUtility.SetDirty(en.SharedData); AssetDatabase.SaveAssets();
                Debug.Log("[Dev7.1b] GameUI dynamic keys=" + DynamicKeys.Length + ", changed=" + changed);
            }
            catch (Exception e) { Debug.LogError("[Dev7.1b] Import error: " + e); }
        }

        [MenuItem("Tools/Block Puzzle/v2.0/Dev7.1b/2. Bind Dynamic Labels")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { Debug.LogError("[Dev7.1b] Exit Play Mode."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            int created = 0, skipped = 0, missing = 0;
            try
            {
                foreach (var pair in Targets)
                {
                    string path = "Assets/_Project/Scenes/" + pair.Key + ".unity";
                    if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                        throw new Exception("Scene missing: " + path);
                    Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    foreach (string name in pair.Value)
                    {
                        int found = 0;
                        foreach (GameObject obj in FindAll(scene, name))
                        {
                            if (obj.GetComponent<TMP_Text>() == null) continue;
                            found++;
                            if (Attach(obj)) created++; else skipped++;
                        }
                        if (found == 0)
                        {
                            Debug.LogWarning("[Dev7.1b] Dynamic label not found: " + pair.Key + "/" + name);
                            missing++;
                        }
                    }
                    if (pair.Key == "ClassicGame")
                    {
                        foreach (string buttonName in new[] { "ClassicRewardedUndoButton",
                                     "ClassicRewardedShuffleButton", "ClassicRewardedContinueButton" })
                        {
                            GameObject button = Find(scene, buttonName);
                            TMP_Text child = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
                            if (child == null) { missing++; continue; }
                            if (Attach(child.gameObject)) created++; else skipped++;
                        }
                    }
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new Exception("Could not save " + path);
                }
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity", OpenSceneMode.Single);
                Debug.Log("[Dev7.1b] Dynamic labels installed=" + created +
                          ", already localized=" + skipped + ", missing=" + missing);
            }
            catch (Exception e) { Debug.LogError("[Dev7.1b] Installation failed: " + e); }
        }

        private static bool Attach(GameObject go)
        {
            if (go.GetComponent<LocalizedDynamicLabel>() != null) return false;
            // The previous static installer may have attached a one-time
            // localization to a label later rewritten by gameplay. Replace
            // that attachment here to prevent conflicting text writers.
            LocalizedTMPLabel staticLabel = go.GetComponent<LocalizedTMPLabel>();
            if (staticLabel != null) Undo.DestroyObjectImmediate(staticLabel);
            LocalizeStringEvent staticEvent = go.GetComponent<LocalizeStringEvent>();
            if (staticEvent != null) Undo.DestroyObjectImmediate(staticEvent);
            Undo.AddComponent<LocalizedDynamicLabel>(go);
            return true;
        }

        private static GameObject Find(Scene scene, string name)
        {
            foreach (GameObject obj in FindAll(scene, name)) return obj;
            return null;
        }

        private static IEnumerable<GameObject> FindAll(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                if (node.name == name) yield return node.gameObject;
        }
    }
}
#endif

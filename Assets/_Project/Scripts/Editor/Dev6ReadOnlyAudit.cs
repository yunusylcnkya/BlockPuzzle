#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BlockPuzzle.Adventure;
using BlockPuzzle.AdventureRules;
using BlockPuzzle.Blocks;
using BlockPuzzle.Classic;
using BlockPuzzle.Gameplay;
using BlockPuzzle.Grid;
using BlockPuzzle.Monetization;
using BlockPuzzle.UI;

namespace BlockPuzzle.EditorTools
{
    /// <summary>
    /// Dev6 regression preflight. Opens scene assets additively in Edit Mode,
    /// reads serialized references, and closes only the scenes it opened.
    /// No PlayerPrefs changes, level progress reset, scene saves or asset writes.
    /// </summary>
    public static class Dev6ReadOnlyAudit
    {
        private const string Scenes = "Assets/_Project/Scenes/";
        private const string CatalogPath =
            "Assets/_Project/ScriptableObjects/Adventure/AdventureCatalog.asset";
        private static string lastReport = "No QA report has been generated.";

        private sealed class Report
        {
            public int Passed;
            public int Failed;
            public int Warnings;
            public readonly StringBuilder Text = new StringBuilder();

            public void Check(bool good, string message)
            {
                Text.AppendLine((good ? "[PASS] " : "[FAIL] ") + message);
                if (good) Passed++; else Failed++;
            }
            public void Warn(string message)
            {
                Text.AppendLine("[WARN] " + message);
                Warnings++;
            }
            public override string ToString() =>
                "BlockPuzzle dev.6 Read-Only Audit\n" +
                "PASS=" + Passed + " FAIL=" + Failed + " WARN=" + Warnings +
                "\n--------------------------------\n" + Text;
        }

        [MenuItem("Tools/Block Puzzle/v2.0/Dev6/1. Audit Project (Read Only)")]
        public static void Audit()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Dev6 QA", "Exit Play Mode before auditing.", "OK");
                return;
            }

            var report = new Report();
            try
            {
                AuditCatalog(report);
                AuditScene(report, "Boot", CheckBoot);
                AuditScene(report, "MainMenu", CheckMenu);
                AuditScene(report, "ClassicGame", CheckClassic);
                AuditScene(report, "AdventureMap", CheckMap);
                AuditScene(report, "AdventureGame", CheckAdventure);
                report.Warn("Android rewarded ads, sound after fullscreen, 60/120 Hz and signed AAB " +
                            "require real-device verification; this audit cannot certify them.");
                report.Warn("Turkish glyph coverage and TMP font material must be visually checked " +
                            "before adding localization.");
            }
            catch (Exception ex)
            {
                report.Check(false, "Audit interrupted: " + ex);
            }

            lastReport = report.ToString();
            if (report.Failed > 0) Debug.LogError(lastReport);
            else Debug.Log(lastReport);
            EditorUtility.DisplayDialog("BlockPuzzle Dev6 QA",
                "Passed: " + report.Passed + "\nFailed: " + report.Failed +
                "\nWarnings: " + report.Warnings +
                "\n\nFull details are in the Console. No assets or saves were modified.",
                "OK");
        }

        [MenuItem("Tools/Block Puzzle/v2.0/Dev6/2. Copy Last QA Report")]
        public static void CopyReport()
        {
            EditorGUIUtility.systemCopyBuffer = lastReport;
            Debug.Log("[Dev6 QA] The last read-only QA report was copied to the clipboard.");
        }

        private static void AuditCatalog(Report r)
        {
            AdventureLevelCatalog catalog =
                AssetDatabase.LoadAssetAtPath<AdventureLevelCatalog>(CatalogPath);
            r.Check(catalog != null, "Adventure catalog asset exists");
            if (catalog == null) return;
            r.Check(catalog.Levels.Count == 120, "Adventure catalog holds exactly 120 levels");

            var ids = new HashSet<int>();
            int specialCount = 0;
            foreach (AdventureLevelDefinition level in catalog.Levels)
            {
                if (level == null)
                {
                    r.Check(false, "Catalog has a missing level asset");
                    continue;
                }
                r.Check(ids.Add(level.LevelId), "Unique level id " + level.LevelId);
                if (level.TargetValue < 1 || level.MoveLimit < 1)
                    r.Check(false, "Invalid target/move limit at level " + level.LevelId);
                if (level.LevelId >= 101 && level.LevelId <= 120)
                {
                    if (level.EnableSpecialBoard) specialCount++;
                    var cells = new List<SpecialCellSpec>();
                    if (level.SpecialCells != null)
                    {
                        foreach (AdventureSpecialCell c in level.SpecialCells)
                            if (c != null)
                                cells.Add(new SpecialCellSpec(c.row, c.column, (int)c.kind));
                    }
                    bool layoutValid = level.EnableSpecialBoard &&
                        level.SpecialCells != null && cells.Count == level.SpecialCells.Count &&
                        SpecialBoardRules.TryValidate(8, 8, cells, out _);
                    r.Check(layoutValid, "Special layout valid for level " + level.LevelId);
                }
            }
            bool contiguous = true;
            for (int i = 1; i <= 120; i++)
                if (!ids.Contains(i)) contiguous = false;
            r.Check(contiguous, "Adventure level IDs contain 1..120 with no gaps");
            r.Check(specialCount == 20, "All 20 new levels enable special-board mode");
        }

        private static void AuditScene(Report r, string name, Action<Report, Scene> inspect)
        {
            string path = Scenes + name + ".unity";
            bool exists = AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null;
            r.Check(exists, "Scene asset exists: " + name);
            if (!exists) return;

            Scene scene = SceneManager.GetSceneByPath(path);
            bool openedByAudit = !scene.IsValid() || !scene.isLoaded;
            try
            {
                if (openedByAudit)
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                inspect(r, scene);
            }
            catch (Exception exception)
            {
                r.Check(false, "Scene check failed for " + name + ": " + exception.Message);
            }
            finally
            {
                if (openedByAudit && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject Find(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child.gameObject;
            return null;
        }

        private static T FindComponent<T>(Scene scene, string gameObjectName)
            where T : Component
        {
            GameObject go = Find(scene, gameObjectName);
            return go != null ? go.GetComponent<T>() : null;
        }

        private static bool ReferenceExists(Component component, string fieldName)
        {
            if (component == null) return false;
            var property = new SerializedObject(component).FindProperty(fieldName);
            return property != null && property.objectReferenceValue != null;
        }

        private static void CheckBoot(Report r, Scene scene)
        {
            AdMobManager ads = FindComponent<AdMobManager>(scene, "AdMobManager");
            r.Check(ads != null, "Boot contains AdMobManager");
            if (ads == null) return;
            SerializedObject s = new SerializedObject(ads);
            SerializedProperty testMode = s.FindProperty("useTestAds");
            r.Check(testMode != null && testMode.boolValue,
                "QA Boot uses AdMob TEST ads (change only in release candidate)");
            var rewarded = s.FindProperty("androidRewardedId");
            if (rewarded == null || string.IsNullOrWhiteSpace(rewarded.stringValue))
                r.Warn("Live AdMob rewarded ID is empty; configure and test before production.");
        }

        private static void CheckMenu(Report r, Scene scene)
        {
            GameObject classic = Find(scene, "ClassicButton");
            GameObject newClassic = Find(scene, "NewClassicButton");
            r.Check(classic != null && classic.activeSelf, "MainMenu ClassicButton is active");
            r.Check(newClassic == null || !newClassic.activeSelf,
                "MainMenu old NewClassicButton is inactive");
            if (classic != null)
            {
                Button b = classic.GetComponent<Button>();
                bool playClassic = false;
                if (b != null)
                for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                    if (b.onClick.GetPersistentMethodName(i) == "PlayClassic") playClassic = true;
                r.Check(playClassic, "ClassicButton calls PlayClassic");
            }
        }

        private static void CheckClassic(Report r, Scene scene)
        {
            r.Check(FindComponent<GridManager>(scene, "GridManager") != null,
                "Classic GridManager present");
            r.Check(FindComponent<BlockSpawner>(scene, "BlockSpawner") != null,
                "Classic BlockSpawner present");
            r.Check(FindComponent<ClassicGameController>(scene, "ClassicGameController") != null,
                "ClassicGameController present");
            r.Check(FindComponent<ClassicRunController>(scene, "ClassicGameController") != null,
                "Classic Continue component present");
            r.Check(FindComponent<ClassicRewardedTools>(scene, "ClassicGameController") != null,
                "Classic RewardedTools component present");
            r.Check(Find(scene, "ClassicRewardedContinueButton") != null,
                "Classic rewarded Continue button present");
            r.Check(Find(scene, "ClassicRewardedUndoButton") != null,
                "Classic rewarded Undo button present");
            r.Check(Find(scene, "ClassicRewardedShuffleButton") != null,
                "Classic rewarded Shuffle button present");
            CheckBoardFX(r, scene, "Classic");
        }

        private static void CheckAdventure(Report r, Scene scene)
        {
            r.Check(FindComponent<AdventureGameController>(scene, "AdventureGameController") != null,
                "AdventureGameController present");
            r.Check(FindComponent<AdventureCoinShopUI>(scene, "AdventureCoinShopUI") != null,
                "AdventureCoinShopUI present");
            Button offer = FindComponent<Button>(scene, "ResultExtraMovesOfferButton");
            r.Check(offer != null, "Adventure BUY +5 MOVES offer button present");
            if (offer != null)
            {
                bool wrong = false;
                for (int i = 0; i < offer.onClick.GetPersistentEventCount(); i++)
                    if (offer.onClick.GetPersistentMethodName(i) == "ReturnToMap") wrong = true;
                r.Check(!wrong, "BUY +5 MOVES has no erroneous ReturnToMap OnClick");
            }
            CheckBoardFX(r, scene, "Adventure");
        }

        private static void CheckMap(Report r, Scene scene)
        {
            r.Check(FindComponent<AdventureMapController>(scene, "AdventureMapController") != null,
                "AdventureMapController present");
            r.Check(Find(scene, "LevelScrollView") != null,
                "Adventure level ScrollView present");
        }

        private static void CheckBoardFX(Report r, Scene scene, string label)
        {
            GameObject board = Find(scene, "BoardContainer");
            r.Check(board != null, label + " BoardContainer present");
            if (board == null) return;
            var fx = board.GetComponent<DirectionalLineClearFX>();
            var shake = board.GetComponent<BoardShakeFeedback>();
            r.Check(fx != null, label + " DirectionalLineClearFX installed");
            r.Check(shake != null, label + " BoardShakeFeedback installed");
            r.Check(ReferenceExists(fx, "gridManager") && ReferenceExists(fx, "gridRoot"),
                label + " directional FX references assigned");
            r.Check(ReferenceExists(shake, "gridManager"),
                label + " shake GridManager reference assigned");
        }
    }
}
#endif

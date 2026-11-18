#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BlockPuzzle.Adventure;
using BlockPuzzle.AdventureRules;

namespace BlockPuzzle.EditorTools
{
    /// <summary>
    /// Safe, idempotent addition of the curated 101..120 Adventure level pack.
    /// Creates new assets and appends references only. Never re-generates
    /// original 1..100 level assets or touches AdventureSaveService/PlayerPrefs.
    /// </summary>
    public static class AdventureDev5Installer
    {
        private const string CatalogPath =
            "Assets/_Project/ScriptableObjects/Adventure/AdventureCatalog.asset";
        private const string OutputParent = "Assets/_Project/Data/Adventure";
        private const string OutputFolder = OutputParent + "/Dev5Levels";

        private sealed class CatalogState
        {
            public AdventureLevelCatalog Catalog;
            public readonly Dictionary<int, AdventureLevelDefinition> ById = new();
            public int Registered;
            public int OnDisk;
        }

        [MenuItem("Tools/Block Puzzle/v2.0/Dev5/1. Analyze Level 101-120 (Read Only)")]
        public static void Analyze()
        {
            try
            {
                CatalogState state = Preflight();
                string summary = "Original levels 1..100: verified, unchanged.\n" +
                    "Dev5 level assets on disk: " + state.OnDisk + "/20\n" +
                    "Dev5 catalog entries: " + state.Registered + "/20\n" +
                    "Missing assets: " + (20 - state.OnDisk) + "\n" +
                    "Missing catalog entries: " + (20 - state.Registered) + "\n\n" +
                    "No project files or save data changed.";
                Debug.Log("[Dev5] PREVIEW OK: " + summary.Replace("\n", " | "));
                EditorUtility.DisplayDialog("Dev5 Level Pack - Preview", summary, "OK");
            }
            catch (Exception error)
            {
                ReportError("Dev5 preview", error);
            }
        }

        [MenuItem("Tools/Block Puzzle/v2.0/Dev5/2. Install Levels 101-120")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Dev5] Exit Play Mode before installing levels.");
                return;
            }
            CatalogState state;
            try { state = Preflight(); }
            catch (Exception error) { ReportError("Dev5 preflight", error); return; }

            if (state.Registered == AdventureDev5Pack.LevelCount)
            {
                Debug.Log("[Dev5] All 20 levels are already installed; nothing to change.");
                EditorUtility.DisplayDialog("Already Installed",
                    "All levels 101..120 are present and verified. No changes made.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Install 20 Adventure levels?",
                "Create/reuse curated level assets 101-120 and append them to:\n" +
                CatalogPath + "\n\n" +
                "Existing 1-100 level assets remain untouched. The catalog " +
                "is updated ONLY to add missing references.\n\n" +
                "Make a Git commit or project backup first.",
                "Install Levels", "Cancel"))
                return;

            int created = 0;
            int appended = 0;
            try
            {
                if (!AssetDatabase.IsValidFolder(OutputFolder))
                    AssetDatabase.CreateFolder(OutputParent, "Dev5Levels");

                var assets = new Dictionary<int, AdventureLevelDefinition>();
                foreach (AdventureDev5Blueprint plan in AdventureDev5Pack.Levels)
                {
                    string path = AssetPath(plan.Id);
                    AdventureLevelDefinition level =
                        AssetDatabase.LoadAssetAtPath<AdventureLevelDefinition>(path);
                    if (level == null)
                    {
                        level = ScriptableObject.CreateInstance<AdventureLevelDefinition>();
                        level.name = "Level_" + plan.Id.ToString("000");
                        ApplyBlueprint(level, plan);
                        AssetDatabase.CreateAsset(level, path);
                        created++;
                    }
                    if (!Matches(level, plan))
                        throw new InvalidOperationException("Level changed during install: " + path);
                    assets.Add(plan.Id, level);
                }

                // Assets are now verified. Only append missing references.
                SerializedObject catalogSO = new SerializedObject(state.Catalog);
                SerializedProperty levels = catalogSO.FindProperty("levels");
                if (levels == null || !levels.isArray)
                    throw new InvalidOperationException("Catalog levels field not found.");

                Undo.RecordObject(state.Catalog, "Install Adventure Dev5 101-120");
                foreach (AdventureDev5Blueprint plan in AdventureDev5Pack.Levels)
                {
                    if (state.ById.ContainsKey(plan.Id)) continue;
                    int index = levels.arraySize;
                    levels.InsertArrayElementAtIndex(index);
                    levels.GetArrayElementAtIndex(index).objectReferenceValue = assets[plan.Id];
                    appended++;
                }
                catalogSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(state.Catalog);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Inspect final state after saving (no further mutations).
                CatalogState verified = Preflight();
                if (verified.Registered != AdventureDev5Pack.LevelCount)
                    throw new InvalidOperationException("Installed catalog does not contain 20 dev5 levels.");

                Debug.Log("[Dev5] INSTALL OK: assets created=" + created +
                          ", catalog references added=" + appended +
                          ", total catalog entries=" + verified.ById.Count +
                          ". Existing 1..100 assets and saves not modified.");
                EditorUtility.DisplayDialog("Dev5 Installation Complete",
                    "Created assets: " + created + "\n" +
                    "Catalog entries added: " + appended + "\n" +
                    "Dev5 installed: 20/20\n" +
                    "Existing levels and save data unchanged.\n\n" +
                    "Next: Audit, then Test Runner/EditMode.", "OK");
            }
            catch (Exception error)
            {
                // Safe to rerun. Valid assets may exist even if append failed.
                // Preflight checks every asset and refuses conflicting changes.
                AssetDatabase.SaveAssets();
                ReportError("Dev5 installation", error);
            }
        }

        [MenuItem("Tools/Block Puzzle/v2.0/Dev5/3. Audit Installed Levels (Read Only)")]
        public static void Audit()
        {
            try
            {
                CatalogState state = Preflight();
                if (state.Registered != AdventureDev5Pack.LevelCount)
                    throw new InvalidOperationException("Only " + state.Registered +
                        "/20 Dev5 levels are registered in the catalog.");
                Debug.Log("[Dev5] AUDIT PASS: 1..100 preserved; 101..120 unique and " +
                    "fully registered; all special board specs and targets valid. " +
                    "Total catalog entries=" + state.ById.Count);
                EditorUtility.DisplayDialog("Dev5 Audit Passed",
                    "All 20 new levels are installed.\n" +
                    "Original 1..100 entries are present.\n" +
                    "No duplicate IDs or invalid board rules.\n" +
                    "This audit does not prove gameplay difficulty or completion.", "OK");
            }
            catch (Exception error) { ReportError("Dev5 audit", error); }
        }

        [MenuItem("Tools/Block Puzzle/v2.0/Dev5/4. Select Level 101 in Inspector")]
        public static void SelectFirst()
        {
            AdventureLevelDefinition level =
                AssetDatabase.LoadAssetAtPath<AdventureLevelDefinition>(AssetPath(101));
            if (level == null)
            {
                Debug.LogWarning("[Dev5] Install the level pack first.");
                return;
            }
            global::UnityEditor.Selection.activeObject = level;
            EditorGUIUtility.PingObject(level);
        }

        private static CatalogState Preflight()
        {
            if (!AdventureDev5Pack.TryValidate(out string reason))
                throw new InvalidOperationException("Invalid blueprint: " + reason);

            AdventureLevelCatalog catalog =
                AssetDatabase.LoadAssetAtPath<AdventureLevelCatalog>(CatalogPath);
            if (catalog == null)
                throw new InvalidOperationException("Production catalog not found at " +
                    CatalogPath + ". No changes made.");

            var state = new CatalogState { Catalog = catalog };
            if (catalog.Levels == null)
                throw new InvalidOperationException("Catalog list is missing.");

            foreach (AdventureLevelDefinition level in catalog.Levels)
            {
                if (level == null || level.LevelId < 1 ||
                    !state.ById.TryAdd(level.LevelId, level))
                    throw new InvalidOperationException("Null, invalid or duplicate catalog level ID.");
            }
            for (int id = 1; id <= 100; id++)
                if (!state.ById.ContainsKey(id))
                    throw new InvalidOperationException("Existing level " + id +
                        " is missing. Refusing to change the catalog.");

            foreach (AdventureDev5Blueprint plan in AdventureDev5Pack.Levels)
            {
                string path = AssetPath(plan.Id);
                AdventureLevelDefinition onDisk =
                    AssetDatabase.LoadAssetAtPath<AdventureLevelDefinition>(path);
                if (onDisk != null)
                {
                    if (!Matches(onDisk, plan))
                        throw new InvalidOperationException("Conflicting Dev5 asset: " + path);
                    state.OnDisk++;
                }
                else if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                    throw new InvalidOperationException("Different asset exists at " + path);

                if (state.ById.TryGetValue(plan.Id, out AdventureLevelDefinition registered))
                {
                    if (AssetDatabase.GetAssetPath(registered) != path ||
                        !Matches(registered, plan))
                        throw new InvalidOperationException("Level " + plan.Id +
                            " already exists in catalog but differs from the Dev5 pack. " +
                            "It will not be replaced.");
                    state.Registered++;
                }
            }
            return state;
        }

        private static bool Matches(AdventureLevelDefinition level,
            AdventureDev5Blueprint plan)
        {
            if (level == null || level.LevelId != plan.Id ||
                level.LevelName != plan.Title ||
                (int)level.Difficulty != plan.Difficulty ||
                (int)level.Objective != plan.Objective ||
                level.TargetValue != plan.Target ||
                level.MoveLimit != plan.Moves ||
                !level.EnableSpecialBoard ||
                level.SpecialCells == null ||
                level.SpecialCells.Count != plan.Cells.Count)
                return false;

            for (int i = 0; i < plan.Cells.Count; i++)
            {
                AdventureSpecialCell cell = level.SpecialCells[i];
                SpecialCellSpec desired = plan.Cells[i];
                if (cell == null || cell.row != desired.Row ||
                    cell.column != desired.Column || (int)cell.kind != desired.Kind)
                    return false;
            }
            return true;
        }

        private static void ApplyBlueprint(AdventureLevelDefinition asset,
            AdventureDev5Blueprint plan)
        {
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("levelId").intValue = plan.Id;
            so.FindProperty("levelName").stringValue = plan.Title;
            so.FindProperty("difficulty").enumValueIndex = plan.Difficulty;
            so.FindProperty("objective").enumValueIndex = plan.Objective;
            so.FindProperty("targetValue").intValue = plan.Target;
            so.FindProperty("moveLimit").intValue = plan.Moves;
            so.FindProperty("enableSpecialBoard").boolValue = true;
            SerializedProperty cells = so.FindProperty("specialCells");
            cells.arraySize = plan.Cells.Count;
            for (int i = 0; i < plan.Cells.Count; i++)
            {
                SerializedProperty cell = cells.GetArrayElementAtIndex(i);
                cell.FindPropertyRelative("row").intValue = plan.Cells[i].Row;
                cell.FindPropertyRelative("column").intValue = plan.Cells[i].Column;
                cell.FindPropertyRelative("kind").enumValueIndex = plan.Cells[i].Kind;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string AssetPath(int id) =>
            OutputFolder + "/Level_" + id.ToString("000") + ".asset";

        private static void ReportError(string title, Exception e)
        {
            Debug.LogError("[Dev5] " + title + " failed: " + e);
            EditorUtility.DisplayDialog("Dev5 Check Failed", e.Message +
                "\n\nNo existing level content will be overwritten.", "OK");
        }
    }
}
#endif

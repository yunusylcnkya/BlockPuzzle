#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BlockPuzzle.Adventure;
using BlockPuzzle.AdventureRules;
using BlockPuzzle.Grid;

namespace BlockPuzzle.EditorTools
{
    /// <summary>
    /// Editor-only tools. No progress, coins or catalog assets are modified.
    /// </summary>
    public static class AdventureSpecialBoardQA
    {
        private const string SamplesFolder =
            "Assets/_Project/Data/Adventure/Dev4Samples";

        [MenuItem("Tools/Block Puzzle/v2.0/Audit Special Boards (Read Only)")]
        public static void AuditAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:AdventureLevelDefinition");
            int legacy = 0;
            int special = 0;
            int invalid = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AdventureLevelDefinition level =
                    AssetDatabase.LoadAssetAtPath<AdventureLevelDefinition>(path);
                if (level == null) continue;
                if (!level.EnableSpecialBoard)
                {
                    legacy++;
                    if (level.Objective == AdventureObjective.ClearObstacles)
                    {
                        invalid++;
                        Debug.LogError("[Dev4 Audit] ClearObstacles without a special board: " + path);
                    }
                    continue;
                }

                special++;
                var specs = new List<SpecialCellSpec>();
                int breakables = 0;
                bool missing = level.SpecialCells == null;
                if (!missing)
                {
                    foreach (AdventureSpecialCell cell in level.SpecialCells)
                    {
                        if (cell == null) { missing = true; break; }
                        specs.Add(new SpecialCellSpec(cell.row, cell.column, (int)cell.kind));
                        if (cell.kind == SpecialCellKind.BreakableObstacle)
                            breakables++;
                    }
                }

                string reason = missing ? "missing special cells" : null;
                bool valid = !missing &&
                    SpecialBoardRules.TryValidate(8, 8, specs, out reason);
                bool impossibleTarget = level.Objective == AdventureObjective.ClearObstacles &&
                    (breakables == 0 || level.TargetValue > breakables);
                if (!valid || impossibleTarget)
                {
                    invalid++;
                    Debug.LogError("[Dev4 Audit] Invalid special level: " + path +
                        " (" + (impossibleTarget ? "obstacle target exceeds count" : reason) + ")");
                }
            }
            Debug.Log($"[Dev4 Audit] Legacy={legacy}, Special={special}, Invalid={invalid}. " +
                "Read only; existing 100 levels, save records and catalog were not changed.");
        }

        [MenuItem("Tools/Block Puzzle/v2.0/Create Dev4 Example Assets (No Catalog Changes)")]
        public static void CreateExamples()
        {
            string baseFolder = "Assets/_Project/Data/Adventure";
            if (!AssetDatabase.IsValidFolder(SamplesFolder))
                AssetDatabase.CreateFolder(baseFolder, "Dev4Samples");

            CreateExample("Level_101_ObstacleBreaker.asset", 101,
                "OBSTACLE BREAKER", AdventureDifficulty.Easy,
                AdventureObjective.ClearObstacles, 2, 18,
                new[] {
                    new SpecialCellSpec(0, 1, 2),
                    new SpecialCellSpec(0, 5, 2),
                    new SpecialCellSpec(4, 4, 1)
                });

            CreateExample("Level_102_LockedColumns.asset", 102,
                "LOCKED COLUMNS", AdventureDifficulty.Medium,
                AdventureObjective.ClearColumns, 2, 24,
                new[] {
                    new SpecialCellSpec(0, 1, 1),
                    new SpecialCellSpec(7, 6, 1),
                    new SpecialCellSpec(2, 3, 2)
                });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dev4 QA] Examples created. Production catalog was NOT modified.");
        }

        private static void CreateExample(string fileName, int id, string title,
            AdventureDifficulty difficulty, AdventureObjective objective,
            int target, int moveLimit, IReadOnlyList<SpecialCellSpec> specs)
        {
            string path = SamplesFolder + "/" + fileName;
            if (AssetDatabase.LoadAssetAtPath<AdventureLevelDefinition>(path) != null)
            {
                Debug.Log("[Dev4 QA] Already exists, not overwritten: " + path);
                return;
            }

            AdventureLevelDefinition level =
                ScriptableObject.CreateInstance<AdventureLevelDefinition>();
            var so = new SerializedObject(level);
            so.FindProperty("levelId").intValue = id;
            so.FindProperty("levelName").stringValue = title;
            so.FindProperty("difficulty").enumValueIndex = (int)difficulty;
            so.FindProperty("objective").enumValueIndex = (int)objective;
            so.FindProperty("targetValue").intValue = target;
            so.FindProperty("moveLimit").intValue = moveLimit;
            so.FindProperty("enableSpecialBoard").boolValue = true;

            SerializedProperty layout = so.FindProperty("specialCells");
            layout.arraySize = specs.Count;
            for (int i = 0; i < specs.Count; i++)
            {
                SerializedProperty entry = layout.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("row").intValue = specs[i].Row;
                entry.FindPropertyRelative("column").intValue = specs[i].Column;
                entry.FindPropertyRelative("kind").enumValueIndex = specs[i].Kind;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(level, path);
        }
    }
}
#endif

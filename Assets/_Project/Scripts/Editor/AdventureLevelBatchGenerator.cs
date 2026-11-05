#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using BlockPuzzle.Adventure;

namespace BlockPuzzle.EditorTools
{
    public sealed class AdventureLevelBatchGenerator : EditorWindow
    {
        private const string DefaultOutputFolder =
            "Assets/_Project/Data/Adventure/GeneratedLevels";

        private AdventureLevelCatalog catalog;
        private AdventureLevelDefinition template;
        private int targetLevelCount = 100;
        private int generatorSeed = 2026;
        private string outputFolder = DefaultOutputFolder;

        [MenuItem("Tools/Block Puzzle/Adventure/Generate Levels")]
        public static void Open()
        {
            GetWindow<AdventureLevelBatchGenerator>("Adventure Generator");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Adventure Level Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Creates missing level assets and adds them to the existing catalog. " +
                "Existing catalog entries are never overwritten.",
                MessageType.Info);

            catalog = (AdventureLevelCatalog)EditorGUILayout.ObjectField(
                "Adventure Catalog", catalog, typeof(AdventureLevelCatalog), false);

            template = (AdventureLevelDefinition)EditorGUILayout.ObjectField(
                "Template Level", template, typeof(AdventureLevelDefinition), false);

            targetLevelCount = EditorGUILayout.IntSlider(
                "Total Level Count", targetLevelCount, 10, 1000);

            generatorSeed = EditorGUILayout.IntField("Generator Seed", generatorSeed);
            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

            EditorGUILayout.Space(12);

            using (new EditorGUI.DisabledScope(catalog == null || template == null))
            {
                if (GUILayout.Button("1. Analyze Catalog", GUILayout.Height(30)))
                    Analyze();

                if (GUILayout.Button("2. Generate Missing Levels", GUILayout.Height(36)))
                    Generate();
            }
        }

        private void Analyze()
        {
            try
            {
                SerializedObject so = new SerializedObject(catalog);
                SerializedProperty list = FindCatalogList(so);
                Dictionary<int, AdventureLevelDefinition> entries = ReadEntries(list);
                int missing = 0;

                for (int i = 1; i <= targetLevelCount; i++)
                {
                    if (!entries.ContainsKey(i))
                        missing++;
                }

                VerifyLevelFields(new SerializedObject(template));

                Debug.Log(
                    $"[LevelGenerator] Catalog entries: {entries.Count}; " +
                    $"missing within 1..{targetLevelCount}: {missing}; " +
                    $"existing entries are protected.");

                EditorUtility.DisplayDialog(
                    "Adventure Catalog Analysis",
                    $"Catalog levels: {entries.Count}\n" +
                    $"Missing in 1..{targetLevelCount}: {missing}\n" +
                    "No changes made.",
                    "OK");
            }
            catch (Exception error)
            {
                Debug.LogError("[LevelGenerator] Analyze failed: " + error);
                EditorUtility.DisplayDialog("Analyze Failed", error.Message, "OK");
            }
        }

        private void Generate()
        {
            if (!EditorUtility.DisplayDialog(
                "Generate Adventure Levels?",
                $"Generate missing levels up to {targetLevelCount}?\n\n" +
                "Existing catalog entries will not be changed. " +
                "Back up your project before continuing.",
                "Generate", "Cancel"))
            {
                return;
            }

            int created = 0;
            int recovered = 0;
            int skipped = 0;

            try
            {
                SerializedObject catalogSO = new SerializedObject(catalog);
                SerializedProperty levelList = FindCatalogList(catalogSO);
                Dictionary<int, AdventureLevelDefinition> existing =
                    ReadEntries(levelList);

                VerifyLevelFields(new SerializedObject(template));
                outputFolder = outputFolder.Replace('\\', '/').TrimEnd('/');
                EnsureFolder(outputFolder);
                Undo.RecordObject(catalog, "Generate Adventure Levels");

                for (int id = 1; id <= targetLevelCount; id++)
                {
                    EditorUtility.DisplayProgressBar(
                        "Generating Adventure Levels",
                        $"Checking Level {id} / {targetLevelCount}",
                        (float)id / targetLevelCount);

                    if (existing.ContainsKey(id))
                    {
                        skipped++;
                        continue;
                    }

                    string path = $"{outputFolder}/Level_{id:000}.asset";
                    AdventureLevelDefinition definition =
                        AssetDatabase.LoadAssetAtPath<AdventureLevelDefinition>(path);

                    if (definition != null)
                    {
                        if (definition.LevelId != id)
                        {
                            throw new InvalidOperationException(
                                $"Asset at {path} contains LevelId {definition.LevelId}, " +
                                $"expected {id}. It was not overwritten.");
                        }

                        recovered++;
                    }
                    else
                    {
                        if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                        {
                            throw new InvalidOperationException(
                                $"A different asset already exists at {path}.");
                        }

                        definition = Instantiate(template);
                        definition.name = $"Level_{id:000}";

                        try
                        {
                            ConfigureLevel(definition, id, generatorSeed);
                            AssetDatabase.CreateAsset(definition, path);
                        }
                        catch
                        {
                            if (!AssetDatabase.Contains(definition))
                                DestroyImmediate(definition);
                            throw;
                        }

                        created++;
                    }

                    catalogSO.Update();
                    levelList = FindCatalogList(catalogSO);
                    int position = levelList.arraySize;
                    levelList.InsertArrayElementAtIndex(position);
                    levelList.GetArrayElementAtIndex(position).objectReferenceValue = definition;
                    catalogSO.ApplyModifiedProperties();
                    existing.Add(id, definition);
                }

                // Keep map button order stable even if old entries were unordered.
                catalogSO.Update();
                SortCatalogByLevelId(catalogSO, existing);
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                string summary =
                    $"Created: {created}\n" +
                    $"Reconnected existing generated assets: {recovered}\n" +
                    $"Already in catalog: {skipped}\n" +
                    $"Catalog levels now: {existing.Count}";

                Debug.Log("[LevelGenerator] Finished. " + summary.Replace("\n", "; "));
                EditorUtility.DisplayDialog("Generation Completed", summary, "OK");
            }
            catch (Exception error)
            {
                // Save successful steps so re-running can resume safely.
                AssetDatabase.SaveAssets();
                Debug.LogError("[LevelGenerator] Generation stopped: " + error);
                EditorUtility.DisplayDialog(
                    "Generation Stopped",
                    error.Message + "\n\nFix the problem and run again. " +
                    "Previously created levels will be skipped.",
                    "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void SortCatalogByLevelId(
            SerializedObject catalogSO,
            Dictionary<int, AdventureLevelDefinition> entries)
        {
            var ordered = new List<AdventureLevelDefinition>(entries.Values);
            ordered.Sort((a, b) => a.LevelId.CompareTo(b.LevelId));

            SerializedProperty list = FindCatalogList(catalogSO);
            list.arraySize = ordered.Count;

            for (int index = 0; index < ordered.Count; index++)
            {
                list.GetArrayElementAtIndex(index).objectReferenceValue = ordered[index];
            }

            catalogSO.ApplyModifiedProperties();
        }

        private static void ConfigureLevel(
            AdventureLevelDefinition definition, int id, int seed)
        {
            var rng = new System.Random(unchecked(seed + id * 7919));
            int tier = Mathf.Clamp((id - 1) / 20, 0, 4);
            int moveLimit = 16 + tier * 3 + rng.Next(0, 3);

            AdventureObjective objective;
            int target;
            string levelName;

            switch (id % 3)
            {
                case 0:
                    objective = AdventureObjective.ReachScore;
                    target = 350 + id * 45 + tier * 120 + rng.Next(0, 4) * 50;
                    levelName = "SCORE CHALLENGE";
                    break;
                case 1:
                    objective = AdventureObjective.ClearLines;
                    target = 1 + id / 18 + rng.Next(0, 2);
                    levelName = "LINE CHALLENGE";
                    break;
                default:
                    objective = AdventureObjective.PlaceBlocks;
                    target = Math.Min(moveLimit, 4 + id / 9 + rng.Next(0, 3));
                    levelName = "BLOCK CHALLENGE";
                    break;
            }

            var so = new SerializedObject(definition);
            SetInt(so, id, "levelId", "id");
            SetString(so, levelName, "levelName", "nameOfLevel");
            SetEnum(so, objective.ToString(), "objective", "objectiveType");
            SetInt(so, target, "targetValue", "target", "goalValue");
            SetInt(so, moveLimit, "moveLimit", "maxMoves", "movesLimit");

            // Optional: allow existing enum-based difficulty definitions.
            SerializedProperty difficulty = Find(so, "difficulty");
            if (difficulty != null &&
                difficulty.propertyType == SerializedPropertyType.Enum &&
                difficulty.enumNames.Length > 0)
            {
                difficulty.enumValueIndex =
                    Mathf.Min(tier, difficulty.enumNames.Length - 1);
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            if (definition.LevelId != id ||
                definition.Objective != objective ||
                definition.TargetValue != target ||
                definition.MoveLimit != moveLimit)
            {
                throw new InvalidOperationException(
                    $"Level {id} failed verification. " +
                    "Inspect AdventureLevelDefinition serialized field names.");
            }
        }

        private static void VerifyLevelFields(SerializedObject so)
        {
            Require(so, SerializedPropertyType.Integer, "levelId", "id");
            Require(so, SerializedPropertyType.String, "levelName", "nameOfLevel");
            Require(so, SerializedPropertyType.Enum, "objective", "objectiveType");
            Require(so, SerializedPropertyType.Integer,
                "targetValue", "target", "goalValue");
            Require(so, SerializedPropertyType.Integer,
                "moveLimit", "maxMoves", "movesLimit");
        }

        private static SerializedProperty Require(
            SerializedObject so, SerializedPropertyType type, params string[] names)
        {
            SerializedProperty prop = Find(so, names);
            if (prop == null || prop.propertyType != type)
            {
                throw new InvalidOperationException(
                    $"Cannot find serialized {type} property for '{names[0]}' " +
                    $"in {so.targetObject.GetType().Name}. " +
                    "Share AdventureLevelDefinition.cs to adapt this mapping.");
            }
            return prop;
        }

        private static void SetInt(SerializedObject so, int value, params string[] names)
        {
            Require(so, SerializedPropertyType.Integer, names).intValue = value;
        }

        private static void SetString(SerializedObject so, string value, params string[] names)
        {
            Require(so, SerializedPropertyType.String, names).stringValue = value;
        }

        private static void SetEnum(SerializedObject so, string value, params string[] names)
        {
            SerializedProperty prop = Require(so, SerializedPropertyType.Enum, names);
            int index = Array.FindIndex(
                prop.enumNames,
                x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));

            if (index < 0)
            {
                throw new InvalidOperationException(
                    $"Enum option '{value}' does not exist in '{prop.propertyPath}'.");
            }
            prop.enumValueIndex = index;
        }

        private static SerializedProperty Find(
            SerializedObject so, params string[] names)
        {
            SerializedProperty iterator = so.GetIterator();
            if (!iterator.NextVisible(true))
                return null;

            do
            {
                if (iterator.depth != 0 || iterator.name == "m_Script")
                    continue;

                string candidate = Normalize(iterator.name);
                foreach (string name in names)
                {
                    if (candidate == Normalize(name))
                        return so.FindProperty(iterator.propertyPath);
                }
            }
            while (iterator.NextVisible(false));

            return null;
        }

        private static string Normalize(string value)
        {
            string source = value.Replace("k__BackingField", "");
            if (source.StartsWith("m_", StringComparison.Ordinal))
                source = source.Substring(2);
            var builder = new StringBuilder(source.Length);
            foreach (char c in source)
            {
                if (char.IsLetterOrDigit(c))
                    builder.Append(char.ToLowerInvariant(c));
            }
            return builder.ToString();
        }

        private static SerializedProperty FindCatalogList(SerializedObject so)
        {
            for (Type type = typeof(AdventureLevelCatalog);
                 type != null && type != typeof(object);
                 type = type.BaseType)
            {
                FieldInfo[] fields = type.GetFields(
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                foreach (FieldInfo field in fields)
                {
                    Type fieldType = field.FieldType;
                    bool matches = fieldType == typeof(AdventureLevelDefinition[]) ||
                        (fieldType.IsGenericType &&
                         fieldType.GetGenericTypeDefinition() == typeof(List<>) &&
                         fieldType.GetGenericArguments()[0] == typeof(AdventureLevelDefinition));

                    if (!matches)
                        continue;

                    SerializedProperty prop = so.FindProperty(field.Name);
                    if (prop != null && prop.isArray)
                        return prop;
                }
            }

            throw new InvalidOperationException(
                "AdventureLevelCatalog has no serialized " +
                "AdventureLevelDefinition list/array. " +
                "Share AdventureLevelCatalog.cs for a precise adapter.");
        }

        private static Dictionary<int, AdventureLevelDefinition> ReadEntries(
            SerializedProperty list)
        {
            var entries = new Dictionary<int, AdventureLevelDefinition>();

            for (int i = 0; i < list.arraySize; i++)
            {
                var definition = list.GetArrayElementAtIndex(i)
                    .objectReferenceValue as AdventureLevelDefinition;
                if (definition == null)
                    continue;

                if (definition.LevelId < 1)
                    throw new InvalidOperationException(
                        $"Catalog contains invalid LevelId {definition.LevelId}.");

                if (entries.ContainsKey(definition.LevelId))
                    throw new InvalidOperationException(
                        $"Catalog contains duplicate LevelId {definition.LevelId}.");

                entries.Add(definition.LevelId, definition);
            }
            return entries;
        }

        private static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) ||
                path.Contains(".."))
            {
                throw new InvalidOperationException(
                    "Output folder must be a valid path inside Assets/.");
            }

            string[] parts = path.Split('/');
            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif

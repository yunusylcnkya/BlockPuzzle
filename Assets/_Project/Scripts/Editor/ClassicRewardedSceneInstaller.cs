#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BlockPuzzle.Blocks;
using BlockPuzzle.Classic;
using BlockPuzzle.Gameplay;
using BlockPuzzle.Grid;

namespace BlockPuzzle.EditorTools
{
    public static class ClassicRewardedSceneInstaller
    {
        private const string ClassicScene = "Assets/_Project/Scenes/ClassicGame.unity";
        private const string AdventureScene = "Assets/_Project/Scenes/AdventureGame.unity";

        [MenuItem("Tools/Block Puzzle/v2.0/Install Rewarded Classic + Fix Adventure Button")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("BlockPuzzle", "Exit Play Mode before installing.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(ClassicScene) ||
                    !AssetDatabase.LoadAssetAtPath<SceneAsset>(AdventureScene))
                    throw new Exception("ClassicGame or AdventureGame scene is missing.");

                InstallClassic(EditorSceneManager.OpenScene(ClassicScene, OpenSceneMode.Single));
                FixAdventure(EditorSceneManager.OpenScene(AdventureScene, OpenSceneMode.Single));
                EditorSceneManager.OpenScene(ClassicScene, OpenSceneMode.Single);
                AssetDatabase.SaveAssets();
                Debug.Log("[BlockPuzzle] Rewarded Classic UI installed and Adventure BUY +5 MOVES OnClick fixed.");
                EditorUtility.DisplayDialog("BlockPuzzle", "Classic rewarded UI installed. Adventure BUY +5 MOVES now opens the coin shop via its runtime listener. Save project and test Android ads.", "OK");
            }
            catch (Exception exception)
            {
                Debug.LogError("[BlockPuzzle] Scene installer failed: " + exception);
                EditorUtility.DisplayDialog("BlockPuzzle", "Installer failed: " + exception.Message + "\nInspect Console.", "OK");
            }
        }

        private static void FixAdventure(Scene scene)
        {
            Button offer = Find<Button>(scene, "ResultExtraMovesOfferButton");
            if (offer == null) throw new Exception("Adventure ResultExtraMovesOfferButton missing.");
            var serialized = new SerializedObject(offer);
            var calls = serialized.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            if (calls == null) throw new Exception("Adventure button OnClick property missing.");
            int removed = 0;
            for (int i = calls.arraySize - 1; i >= 0; i--)
            {
                var entry = calls.GetArrayElementAtIndex(i);
                var method = entry.FindPropertyRelative("m_MethodName");
                if (method != null && method.stringValue == "ReturnToMap")
                {
                    calls.DeleteArrayElementAtIndex(i);
                    removed++;
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(offer);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BlockPuzzle] Removed " + removed + " erroneous Adventure offer -> ReturnToMap persistent calls.");
        }

        private static void InstallClassic(Scene scene)
        {
            ClassicGameController game = Find<ClassicGameController>(scene, "ClassicGameController");
            ClassicRunController run = Find<ClassicRunController>(scene, "ClassicGameController");
            GridManager grid = Find<GridManager>(scene, "GridManager");
            BlockSpawner spawner = Find<BlockSpawner>(scene, "BlockSpawner");
            ScoreManager score = Find<ScoreManager>(scene, "ScoreManager");
            RectTransform gridRoot = Find<RectTransform>(scene, "GridRoot");
            RectTransform safeRoot = Find<RectTransform>(scene, "SafeAreaRoot");
            RectTransform resultCard = Find<RectTransform>(scene, "ResultCard");
            Button restart = Find<Button>(scene, "RestartButton");
            Button home = Find<Button>(scene, "HomeButton");
            if (game == null || run == null || grid == null || spawner == null ||
                score == null || gridRoot == null || safeRoot == null ||
                resultCard == null || restart == null || home == null)
                throw new Exception("Classic scene references incomplete; inspect Hierarchy before installing.");

            var tools = game.GetComponent<ClassicRewardedTools>();
            if (tools == null) tools = game.gameObject.AddComponent<ClassicRewardedTools>();

            Button rewardedContinue = CloneOrFind(scene, restart, resultCard,
                "ClassicRewardedContinueButton", "WATCH AD & CONTINUE");
            Button undo = CloneOrFind(scene, restart, safeRoot,
                "ClassicRewardedUndoButton", "WATCH AD - UNDO");
            Button shuffle = CloneOrFind(scene, restart, safeRoot,
                "ClassicRewardedShuffleButton", "AD SHUFFLE (5)");

            // Extra game-over action fits without overlapping the existing score.
            resultCard.sizeDelta = new Vector2(850, 1140);
            LayoutButton(rewardedContinue, new Vector2(0.5f, 0.5f),
                new Vector2(0, -190), new Vector2(690, 120));
            LayoutButton(restart, new Vector2(0.5f, 0.5f),
                new Vector2(0, -330), new Vector2(675, 125));
            LayoutButton(home, new Vector2(0.5f, 0.5f),
                new Vector2(0, -470), new Vector2(650, 125));

            LayoutButton(undo, new Vector2(0.5f, 1f),
                new Vector2(-195, -330), new Vector2(370, 92));
            LayoutButton(shuffle, new Vector2(0.5f, 1f),
                new Vector2(195, -330), new Vector2(370, 92));

            SetObject(tools, "gridManager", grid);
            SetObject(tools, "gridRoot", gridRoot);
            SetObject(tools, "blockSpawner", spawner);
            SetObject(tools, "scoreManager", score);
            SetObject(tools, "classicGame", game);
            SetObject(tools, "classicRun", run);
            SetObject(tools, "undoAdButton", undo);
            SetObject(tools, "shuffleAdButton", shuffle);
            SetObject(tools, "continueAdButton", rewardedContinue);

            EditorUtility.SetDirty(tools);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetObject(Component component, string propertyName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(component);
            var field = serialized.FindProperty(propertyName);
            if (field == null) throw new Exception("Installer property missing: " + propertyName);
            field.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button CloneOrFind(Scene scene, Button template,
            RectTransform parent, string name, string caption)
        {
            Button existing = Find<Button>(scene, name);
            if (existing != null) return existing;
            GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, parent, false);
            clone.name = name;
            clone.SetActive(true);
            Button button = clone.GetComponent<Button>();
            if (button == null) throw new Exception("Could not clone button: " + name);
            // A cloned RestartButton inherits RestartGame() in a persistent
            // UnityEvent. Remove it, or a reward click would restart the scene!
            var serialized = new SerializedObject(button);
            var calls = serialized.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            if (calls == null) throw new Exception("Could not clear cloned button OnClick: " + name);
            calls.ClearArray();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            button.onClick.RemoveAllListeners();
            TMP_Text text = clone.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = caption;
                text.enableAutoSizing = true;
                text.fontSizeMin = 19f;
                text.fontSizeMax = 54f;
            }
            return button;
        }

        private static void LayoutButton(Button button, Vector2 anchor,
            Vector2 position, Vector2 size)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private static T Find<T>(Scene scene, string objectName) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == objectName)
                    return child.GetComponent<T>();
            return null;
        }
    }
}
#endif

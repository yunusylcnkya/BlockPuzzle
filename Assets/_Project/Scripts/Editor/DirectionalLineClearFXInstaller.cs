#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BlockPuzzle.Gameplay;
using BlockPuzzle.Grid;

namespace BlockPuzzle.EditorTools
{
    /// <summary>
    /// Installs the additive, cosmetic component; does not edit GridManager,
    /// LineClearResult, save files, level definitions, or existing components.
    /// Can be run again safely without making duplicate components.
    /// </summary>
    public static class DirectionalLineClearFXInstaller
    {
        private const string Classic = "Assets/_Project/Scenes/ClassicGame.unity";
        private const string Adventure = "Assets/_Project/Scenes/AdventureGame.unity";

        [MenuItem("Tools/Block Puzzle/v2.0/Dev5.2 Install Horizontal + Vertical Clear FX")]
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("BlockPuzzle", "Exit Play Mode first.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Classic) == null ||
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(Adventure) == null)
                    throw new InvalidOperationException("ClassicGame or AdventureGame scene is missing.");

                InstallInto(EditorSceneManager.OpenScene(Classic, OpenSceneMode.Single));
                InstallInto(EditorSceneManager.OpenScene(Adventure, OpenSceneMode.Single));
                EditorSceneManager.OpenScene(Classic, OpenSceneMode.Single);
                Debug.Log("[Dev5.2] Directional line clear FX installed in Classic and Adventure.");
                EditorUtility.DisplayDialog("BlockPuzzle Dev5.2",
                    "Horizontal and vertical clear FX installed in both gameplay scenes. " +
                    "Press Play, select BoardContainer, and use the DEBUG preview commands " +
                    "on DirectionalLineClearFX to view each animation without changing scores.", "OK");
            }
            catch (Exception error)
            {
                Debug.LogError("[Dev5.2] FX installer failed: " + error);
                EditorUtility.DisplayDialog("BlockPuzzle Dev5.2", error.Message, "OK");
            }
        }

        private static void InstallInto(Scene scene)
        {
            GameObject board = Find(scene, "BoardContainer");
            GameObject gridRoot = Find(scene, "GridRoot");
            GameObject gridManager = Find(scene, "GridManager");
            if (board == null || board.GetComponent<RectTransform>() == null ||
                gridRoot == null || gridRoot.GetComponent<RectTransform>() == null ||
                gridManager == null || gridManager.GetComponent<GridManager>() == null)
                throw new InvalidOperationException("Missing BoardContainer, GridRoot or GridManager in " + scene.name);

            DirectionalLineClearFX fx = board.GetComponent<DirectionalLineClearFX>();
            if (fx == null) fx = Undo.AddComponent<DirectionalLineClearFX>(board);
            var serialized = new SerializedObject(fx);
            SerializedProperty grid = serialized.FindProperty("gridManager");
            SerializedProperty root = serialized.FindProperty("gridRoot");
            if (grid == null || root == null)
                throw new InvalidOperationException("DirectionalLineClearFX reference fields missing.");

            grid.objectReferenceValue = gridManager.GetComponent<GridManager>();
            root.objectReferenceValue = gridRoot.GetComponent<RectTransform>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fx);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save scene: " + scene.name);
        }

        private static GameObject Find(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name)
                    return child.gameObject;
            return null;
        }
    }
}
#endif

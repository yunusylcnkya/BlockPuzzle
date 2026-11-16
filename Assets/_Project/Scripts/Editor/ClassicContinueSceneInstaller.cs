#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BlockPuzzle.Blocks;
using BlockPuzzle.Classic;
using BlockPuzzle.Gameplay;
using BlockPuzzle.Grid;
using BlockPuzzle.UI;

namespace BlockPuzzle.EditorTools
{
    public static class ClassicContinueSceneInstaller
    {
        private const string ClassicScene = "Assets/_Project/Scenes/ClassicGame.unity";
        private const string MenuScene = "Assets/_Project/Scenes/MainMenu.unity";

        [MenuItem("Tools/Block Puzzle/v2.0/Install Classic Continue Components")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("BlockPuzzle", "Stop Play Mode first.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!EditorUtility.DisplayDialog("BlockPuzzle dev.3",
                "Add ClassicRunController to ClassicGame and ClassicMenuActions to MainMenu? " +
                "These two scenes will be saved. No save data will be modified.",
                "Install", "Cancel")) return;

            Scene classic = EditorSceneManager.OpenScene(ClassicScene, OpenSceneMode.Single);
            var controller = Find<ClassicGameController>(classic);
            var grid = Find<GridManager>(classic);
            var spawner = Find<BlockSpawner>(classic);
            var score = Find<ScoreManager>(classic);
            var root = FindRect(classic, "GridRoot");
            if (controller == null || grid == null || spawner == null ||
                score == null || root == null)
            {
                Debug.LogError("[Dev3 Installer] Missing ClassicGame objects. Scene not modified.");
                return;
            }
            var resume = controller.GetComponent<ClassicRunController>();
            if (resume == null) resume = Undo.AddComponent<ClassicRunController>(controller.gameObject);
            var props = new SerializedObject(resume);
            props.FindProperty("gridManager").objectReferenceValue = grid;
            props.FindProperty("gridRoot").objectReferenceValue = root;
            props.FindProperty("blockSpawner").objectReferenceValue = spawner;
            props.FindProperty("scoreManager").objectReferenceValue = score;
            props.ApplyModifiedProperties();
            EditorUtility.SetDirty(resume);
            EditorSceneManager.MarkSceneDirty(classic);
            EditorSceneManager.SaveScene(classic);

            Scene menu = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
            var menuController = Find<MainMenuController>(menu);
            if (menuController == null)
            {
                Debug.LogError("[Dev3 Installer] MainMenuController missing. " +
                               "ClassicGame was configured successfully.");
                return;
            }
            if (menuController.GetComponent<ClassicMenuActions>() == null)
                Undo.AddComponent<ClassicMenuActions>(menuController.gameObject);
            EditorSceneManager.MarkSceneDirty(menu);
            EditorSceneManager.SaveScene(menu);
            Debug.Log("[Dev3 Installer] Classic Continue installed. " +
                      "Existing Classic menu button now auto-resumes. " +
                      "Add an optional New Game button calling ClassicMenuActions.StartNewClassic().");
            EditorUtility.DisplayDialog("BlockPuzzle dev.3",
                "Both scenes configured. Run EditMode tests and the manual Continue test.", "OK");
        }

        private static T Find<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<T>(true))
                .FirstOrDefault();

        private static RectTransform FindRect(Scene scene, string name) =>
            scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<RectTransform>(true))
                .FirstOrDefault(rect => rect.name == name);
    }
}
#endif

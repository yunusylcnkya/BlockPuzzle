#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BlockPuzzle.Gameplay;
using BlockPuzzle.Grid;

namespace BlockPuzzle.EditorTools
{
    public static class GameplayPolishInstaller
    {
        private const string MainMenu = "Assets/_Project/Scenes/MainMenu.unity";
        private const string Classic = "Assets/_Project/Scenes/ClassicGame.unity";
        private const string Adventure = "Assets/_Project/Scenes/AdventureGame.unity";

        [MenuItem("Tools/Block Puzzle/v2.0/Dev5.1 Install Gameplay Polish")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Polish] Exit Play mode before running installer.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                Scene menu = EditorSceneManager.OpenScene(MainMenu, OpenSceneMode.Single);
                GameObject newGame = Find(menu, "NewClassicButton");
                GameObject classic = Find(menu, "ClassicButton");
                if (newGame == null || classic == null)
                    throw new Exception("MainMenu ClassicButton/NewClassicButton not found.");
                newGame.SetActive(false); // Keep component and old scene data; no deletion.
                TMP_Text caption = classic.GetComponentInChildren<TMP_Text>(true);
                if (caption == null) throw new Exception("ClassicButton text missing.");
                caption.text = "CLASSIC";
                RectTransform rt = classic.GetComponent<RectTransform>();
                Vector2 p = rt.anchoredPosition;
                rt.anchoredPosition = new Vector2(p.x, -110f);
                EditorUtility.SetDirty(newGame);
                EditorUtility.SetDirty(caption);
                EditorUtility.SetDirty(rt);
                Save(menu);

                InstallShake(EditorSceneManager.OpenScene(Classic, OpenSceneMode.Single));
                InstallShake(EditorSceneManager.OpenScene(Adventure, OpenSceneMode.Single));
                EditorSceneManager.OpenScene(Classic, OpenSceneMode.Single);
                Debug.Log("[Polish] MainMenu Classic simplified, board shake installed in both scenes.");
                EditorUtility.DisplayDialog("BlockPuzzle Dev5.1",
                    "Classic button consolidated and optional board shake attached. " +
                    "Check drag preview, sound after ads and rewarded continue on Android.", "OK");
            }
            catch (Exception e)
            {
                Debug.LogError("[Polish] Installer failed: " + e);
                EditorUtility.DisplayDialog("BlockPuzzle Dev5.1", e.Message, "OK");
            }
        }

        private static void InstallShake(Scene scene)
        {
            GameObject board = Find(scene, "BoardContainer");
            GameObject grid = Find(scene, "GridManager");
            if (board == null || grid == null || grid.GetComponent<GridManager>() == null)
                throw new Exception("BoardContainer/GridManager missing: " + scene.name);
            BoardShakeFeedback shake = board.GetComponent<BoardShakeFeedback>();
            if (shake == null) shake = board.AddComponent<BoardShakeFeedback>();
            var so = new SerializedObject(shake);
            so.FindProperty("gridManager").objectReferenceValue = grid.GetComponent<GridManager>();
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(shake);
            Save(scene);
        }

        private static GameObject Find(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == name) return child.gameObject;
            return null;
        }

        private static void Save(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new Exception("Scene save failed: " + scene.name);
        }
    }
}
#endif


using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockPuzzle.Adventure
{
    public sealed class AdventureGameController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField]
        private AdventureLevelCatalog catalog;

        [Header("UI")]
        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private TMP_Text levelNameText;

        [SerializeField]
        private TMP_Text objectiveText;

        private void Start()
        {
            int levelId = AdventureSession.SelectedLevelId;

            if (levelId < 1 ||
                !AdventureProgress.IsUnlocked(levelId))
            {
                Debug.LogWarning(
                    "[AdventureGame] No valid level selected."
                );

                ReturnToMap();
                return;
            }

            if (catalog == null)
            {
                Debug.LogError(
                    "[AdventureGame] Catalog not assigned."
                );

                ReturnToMap();
                return;
            }

            AdventureLevelDefinition level =
                catalog.GetLevel(levelId);

            if (level == null)
            {
                Debug.LogError(
                    $"[AdventureGame] Level {levelId} not found."
                );

                ReturnToMap();
                return;
            }

            if (titleText != null)
                titleText.text = $"LEVEL {level.LevelId}";

            if (levelNameText != null)
                levelNameText.text = level.LevelName;

            if (objectiveText != null)
            {
                string objectiveLabel =
                    level.Objective switch
                    {
                        AdventureObjective.ReachScore =>
                            "TARGET SCORE",

                        AdventureObjective.ClearLines =>
                            "CLEAR LINES",

                        AdventureObjective.PlaceBlocks =>
                            "PLACE BLOCKS",

                        _ => "TARGET"
                    };

                objectiveText.text =
                    $"{objectiveLabel}: {level.TargetValue}\n" +
                    $"MOVE LIMIT: {level.MoveLimit}";
            }

            Debug.Log(
                $"[AdventureGame] Opened Level {level.LevelId}"
            );
        }

        public void ReturnToMap()
        {
            AdventureSession.Clear();
            SceneManager.LoadScene("AdventureMap");
        }
    }
}

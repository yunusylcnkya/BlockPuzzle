
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using BlockPuzzle.Blocks;
using BlockPuzzle.Core;
using BlockPuzzle.Gameplay;
using BlockPuzzle.Grid;
using BlockPuzzle.SaveSystem;

namespace BlockPuzzle.Adventure
{
    public sealed class AdventureGameController : MonoBehaviour
    {
        [Header("Level Data")]
        [SerializeField]
        private AdventureLevelCatalog catalog;

        [Header("Gameplay Systems")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private BlockSpawner blockSpawner;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private GameFeedbackController feedbackController;

        [Header("Objective UI")]
        [SerializeField] private TMP_Text levelTitleText;
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text progressValueText;
        [SerializeField] private TMP_Text movesText;
        [SerializeField] private Image progressFill;

        [Header("Result UI")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultTitleText;
        [SerializeField] private TMP_Text resultDescriptionText;
        [SerializeField] private TMP_Text resultScoreText;

        [SerializeField] private GameObject nextLevelButton;
        [SerializeField] private GameObject retryButton;

        [Header("Save Result UI")]
        [SerializeField] private TMP_Text resultStarsText;
        [SerializeField] private TMP_Text resultCoinsText;
        [SerializeField] private TMP_Text resultBestText;

        private AdventureLevelDefinition currentLevel;
        private int movesUsed;
        private int totalLinesCleared;
        private int totalBlocksPlaced;

        private bool gameFinished;
        private bool initialized;
        private Coroutine pendingEvaluation;

        private void Awake()
        {
            if (resultPanel != null)
                resultPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (gridManager != null)
                gridManager.BlockPlaced += HandleBlockPlaced;

            if (blockSpawner != null)
                blockSpawner.TrayChanged += HandleTrayChanged;
        }

        private void Start()
        {
            if (!ValidateReferences())
            {
                Debug.LogError(
                    "[Adventure] Missing Inspector references."
                );
                enabled = false;
                return;
            }

            int levelId = AdventureSession.SelectedLevelId;

            if (levelId < 1 ||
                !AdventureProgress.IsUnlocked(levelId))
            {
                Debug.LogWarning(
                    "[Adventure] Invalid selected level."
                );

                ReturnToMap();
                return;
            }

            currentLevel = catalog.GetLevel(levelId);

            if (currentLevel == null)
            {
                Debug.LogError(
                    $"[Adventure] Level {levelId} not found."
                );

                ReturnToMap();
                return;
            }

            movesUsed = 0;
            totalLinesCleared = 0;
            totalBlocksPlaced = 0;
            gameFinished = false;
            initialized = true;

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Playing);

            RefreshObjectiveUI();
            RequestEvaluation();

            Debug.Log(
                $"[Adventure] Level {currentLevel.LevelId} started."
            );
        }

        private bool ValidateReferences()
        {
            return catalog != null &&
                   gridManager != null &&
                   blockSpawner != null &&
                   scoreManager != null &&
                   levelTitleText != null &&
                   objectiveText != null &&
                   progressValueText != null &&
                   movesText != null &&
                   progressFill != null &&
                   resultPanel != null &&
                   resultTitleText != null &&
                   resultDescriptionText != null &&
                   resultScoreText != null;
        }

        private void OnDisable()
        {
            if (gridManager != null)
                gridManager.BlockPlaced -= HandleBlockPlaced;

            if (blockSpawner != null)
                blockSpawner.TrayChanged -= HandleTrayChanged;

            if (pendingEvaluation != null)
            {
                StopCoroutine(pendingEvaluation);
                pendingEvaluation = null;
            }
        }

        private void HandleBlockPlaced(
            BlockPlacementResult placement)
        {
            if (!initialized || gameFinished ||
                placement == null)
                return;

            movesUsed++;
            totalBlocksPlaced++;

            if (placement.LineClear != null)
            {
                totalLinesCleared +=
                    placement.LineClear.TotalLines;
            }

            RefreshObjectiveUI();
            RequestEvaluation();
        }

        private void HandleTrayChanged()
        {
            if (!initialized || gameFinished)
                return;

            RequestEvaluation();
        }

        private int GetObjectiveProgress()
        {
            if (currentLevel == null)
                return 0;

            switch (currentLevel.Objective)
            {
                case AdventureObjective.ReachScore:
                    return scoreManager.CurrentScore;

                case AdventureObjective.ClearLines:
                    return totalLinesCleared;

                case AdventureObjective.PlaceBlocks:
                    return totalBlocksPlaced;

                default:
                    return 0;
            }
        }

        private string GetObjectiveLabel()
        {
            if (currentLevel == null)
                return string.Empty;

            switch (currentLevel.Objective)
            {
                case AdventureObjective.ReachScore:
                    return "REACH SCORE";

                case AdventureObjective.ClearLines:
                    return "CLEAR LINES";

                case AdventureObjective.PlaceBlocks:
                    return "PLACE BLOCKS";

                default:
                    return "OBJECTIVE";
            }
        }

        private void RefreshObjectiveUI()
        {
            if (currentLevel == null)
                return;

            int progress = GetObjectiveProgress();
            int target = currentLevel.TargetValue;

            levelTitleText.text =
                $"LEVEL {currentLevel.LevelId} — " +
                currentLevel.LevelName.ToUpperInvariant();

            objectiveText.text = GetObjectiveLabel();

            progressValueText.text =
                $"{Mathf.Min(progress, target)} / {target}";

            movesText.text =
                $"MOVES {movesUsed} / {currentLevel.MoveLimit}";

            progressFill.fillAmount =
                Mathf.Clamp01((float)progress / target);
        }

        private void RequestEvaluation()
        {
            if (!isActiveAndEnabled ||
                !initialized ||
                gameFinished)
                return;

            if (pendingEvaluation != null)
                StopCoroutine(pendingEvaluation);

            pendingEvaluation =
                StartCoroutine(EvaluateNextFrame());
        }

        private IEnumerator EvaluateNextFrame()
        {
            // Wait for ScoreManager to process BlockPlaced,
            // and for BlockSpawner to update its tray.
            yield return null;

            pendingEvaluation = null;

            if (gameFinished || currentLevel == null)
                yield break;

            RefreshObjectiveUI();

            // Winning takes priority on the final move.
            if (GetObjectiveProgress() >= currentLevel.TargetValue)
            {
                CompleteLevel();
                yield break;
            }

            if (movesUsed >= currentLevel.MoveLimit)
            {
                FailLevel("NO MOVES LEFT");
                yield break;
            }

            // Do not interpret an uninitialized tray as Game Over.
            if (gridManager.GetCell(0, 0) == null)
                yield break;

            bool anyBlockAvailable = false;
            bool hasValidMove = false;

            foreach (BlockView block in blockSpawner.ActiveBlocks)
            {
                if (block == null || block.Definition == null)
                    continue;

                anyBlockAvailable = true;

                if (gridManager.HasAnyValidPlacement(
                    block.Definition))
                {
                    hasValidMove = true;
                    break;
                }
            }

            if (anyBlockAvailable && !hasValidMove)
            {
                FailLevel("NO AVAILABLE PLACEMENTS");
            }
        }


        private void CompleteLevel()
        {
            if (gameFinished)
                return;

            gameFinished = true;

            AdventureCompletionResult completion =
                AdventureSaveService.CompleteLevel(
                    currentLevel.LevelId,
                    scoreManager.CurrentScore,
                    movesUsed,
                    currentLevel.MoveLimit
                );

            AdventureLevelRecord record =
                AdventureSaveService.GetLevelRecord(
                    currentLevel.LevelId
                );

            if (resultStarsText != null)
            {
                resultStarsText.text =
                    GetStarDisplay(completion.EarnedStars);
            }

            if (resultCoinsText != null)
            {
                resultCoinsText.text =
                    completion.CoinsEarned > 0
                        ? $"+{completion.CoinsEarned} COINS"
                        : "REWARD ALREADY CLAIMED";
            }

            if (resultBestText != null)
            {
                resultBestText.text =
                    $"BEST SCORE: {record.bestScore:N0}\n" +
                    $"BEST MOVES: {record.bestMoves}";
            }

            ShowResult(
                true,
                "LEVEL COMPLETE!",
                "OBJECTIVE COMPLETED"
            );

            Debug.Log(
                $"[Adventure] Level {currentLevel.LevelId} completed. " +
                $"Stars: {completion.EarnedStars}, " +
                $"Coins: +{completion.CoinsEarned}"
            );
        }


        private string GetStarDisplay(int stars)
        {
            stars = Mathf.Clamp(stars, 0, 3);

            return new string('★', stars) +
                   new string('☆', 3 - stars);
        }

        private void FailLevel(string reason)
        {
            if (gameFinished)
                return;

            gameFinished = true;

            if (resultStarsText != null)
                resultStarsText.text = string.Empty;

            if (resultCoinsText != null)
                resultCoinsText.text = string.Empty;

            if (resultBestText != null)
                resultBestText.text = string.Empty;

            ShowResult(
                false,
                "LEVEL FAILED",
                reason
            );

            if (feedbackController != null)
                feedbackController.PlayGameOver();

            Debug.Log(
                $"[Adventure] Level {currentLevel.LevelId} failed: {reason}"
            );
        }

        private void ShowResult(
            bool completed,
            string title,
            string description)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.GameOver);

            // Prevent further drag interactions.
            foreach (BlockView block in blockSpawner.ActiveBlocks)
            {
                if (block == null)
                    continue;

                BlockDragHandler drag =
                    block.GetComponent<BlockDragHandler>();

                if (drag != null)
                    drag.enabled = false;
            }

            resultTitleText.text = title;
            resultDescriptionText.text = description;
            resultScoreText.text =
                $"SCORE {scoreManager.CurrentScore:N0}";

            if (nextLevelButton != null)
            {
                bool canGoNext =
                    completed &&
                    catalog.ContainsLevel(
                        currentLevel.LevelId + 1
                    );

                nextLevelButton.SetActive(canGoNext);
            }

            if (retryButton != null)
                retryButton.SetActive(!completed);

            resultPanel.SetActive(true);
        }

        public void RestartLevel()
        {
            if (currentLevel == null)
                return;

            AdventureSession.SelectLevel(
                currentLevel.LevelId
            );

            SceneManager.LoadScene("AdventureGame");
        }

        public void NextLevel()
        {
            if (currentLevel == null || !gameFinished)
                return;

            int nextLevel = currentLevel.LevelId + 1;

            if (!catalog.ContainsLevel(nextLevel) ||
                !AdventureProgress.IsUnlocked(nextLevel))
            {
                ReturnToMap();
                return;
            }

            AdventureSession.SelectLevel(nextLevel);
            SceneManager.LoadScene("AdventureGame");
        }

        public void ReturnToMap()
        {
            AdventureSession.Clear();
            SceneManager.LoadScene("AdventureMap");
        }
    }
}

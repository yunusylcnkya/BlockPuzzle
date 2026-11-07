using System;
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
using BlockPuzzle.Monetization;
using BlockPuzzle.UI;

namespace BlockPuzzle.Adventure
{
    public sealed class AdventureGameController : MonoBehaviour
    {
        [Header("Level Data")]
        [SerializeField] private AdventureLevelCatalog catalog;

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
        [SerializeField] private TMP_Text resultCoinsText;
        [SerializeField] private TMP_Text resultBestText;

        [Header("Result Star Rating")]
        [SerializeField] private UIStarRating resultStarRating;

        [Header("Extra Moves - Coin Shop")]
        [SerializeField, Min(1)] private int extraMovesPerPurchase = 5;
        [SerializeField, Range(1, 5)] private int maxExtraMovePurchases = 2;
        [SerializeField] private Button resultExtraMovesOfferButton;

        private AdventureLevelDefinition currentLevel;
        private int movesUsed;
        private int totalLinesCleared;
        private int totalBlocksPlaced;
        private int extraMovePurchases;
        private bool failedDueToMoveLimit;
        private bool gameFinished;
        private bool initialized;
        private Coroutine pendingEvaluation;

        public int ExtraMovesPerPurchase => extraMovesPerPurchase;
        public int MaxExtraMovePurchases => maxExtraMovePurchases;
        public int ExtraMovePurchasesUsed => extraMovePurchases;
        public int EffectiveMoveLimit => currentLevel == null
            ? 0 : currentLevel.MoveLimit + extraMovePurchases * extraMovesPerPurchase;

        // The shop may revive only the NO MOVES LEFT result, never a completed
        // level or a NO AVAILABLE PLACEMENTS result.
        public bool IsAwaitingExtraMoves => initialized && gameFinished &&
            failedDueToMoveLimit && currentLevel != null &&
            GameManager.Instance != null &&
            GameManager.Instance.CurrentState == GameState.GameOver;

        public bool CanPurchaseExtraMoves
        {
            get
            {
                if (!initialized || currentLevel == null || gridManager == null ||
                    blockSpawner == null || extraMovePurchases >= maxExtraMovePurchases ||
                    extraMovesPerPurchase <= 0 || blockSpawner.IsAnyBlockDragging ||
                    gridManager.GetCell(0, 0) == null || GameManager.Instance == null)
                    return false;

                // Without a playable block, buying moves alone cannot help.
                if (!HasValidMoveInTray())
                    return false;

                if (gameFinished)
                    return IsAwaitingExtraMoves;

                return GameManager.Instance.CurrentState == GameState.Playing &&
                    movesUsed < EffectiveMoveLimit;
            }
        }

        private bool HasValidMoveInTray()
        {
            foreach (BlockView block in blockSpawner.ActiveBlocks)
            {
                if (block != null && block.Definition != null &&
                    gridManager.HasAnyValidPlacement(block.Definition))
                    return true;
            }
            return false;
        }

        // The payment callback is called only after all gameplay checks pass.
        // The extra move allowance lasts for this level attempt only.
        public bool TryPurchaseExtraMoves(Func<bool> tryCommitPayment)
        {
            if (tryCommitPayment == null || !CanPurchaseExtraMoves)
                return false;

            if (!tryCommitPayment())
                return false;

            extraMovePurchases++;
            bool wasRescue = gameFinished && failedDueToMoveLimit;

            if (wasRescue)
            {
                gameFinished = false;
                failedDueToMoveLimit = false;

                if (resultPanel != null)
                    resultPanel.SetActive(false);

                if (resultExtraMovesOfferButton != null)
                    resultExtraMovesOfferButton.gameObject.SetActive(false);

                foreach (BlockView block in blockSpawner.ActiveBlocks)
                {
                    if (block == null) continue;
                    BlockDragHandler drag = block.GetComponent<BlockDragHandler>();
                    if (drag != null)
                        drag.enabled = true;
                }

                if (GameManager.Instance != null)
                    GameManager.Instance.SetState(GameState.Playing);
            }

            RefreshObjectiveUI();
            RequestEvaluation();
            Debug.Log($"[ExtraMoves] +{extraMovesPerPurchase} purchased; " +
                $"limit {EffectiveMoveLimit}; purchases {extraMovePurchases}/{maxExtraMovePurchases}; " +
                $"revived={wasRescue}.");
            return true;
        }

        // The Undo system deliberately captures only the actual move counters.
        // Purchased extra moves are not refunded or lost when a move is undone.
        public bool CanUndoMove => initialized && !gameFinished && currentLevel != null &&
            GameManager.Instance != null &&
            GameManager.Instance.CurrentState == GameState.Playing;

        public readonly struct UndoProgressState
        {
            public readonly int Moves;
            public readonly int Lines;
            public readonly int Blocks;

            public UndoProgressState(int moves, int lines, int blocks)
            {
                Moves = moves;
                Lines = lines;
                Blocks = blocks;
            }
        }

        public UndoProgressState CaptureUndoProgress() =>
            new UndoProgressState(movesUsed, totalLinesCleared, totalBlocksPlaced);

        public void RestoreUndoProgress(UndoProgressState state)
        {
            if (pendingEvaluation != null)
            {
                StopCoroutine(pendingEvaluation);
                pendingEvaluation = null;
            }
            movesUsed = state.Moves;
            totalLinesCleared = state.Lines;
            totalBlocksPlaced = state.Blocks;
            RefreshObjectiveUI();
            RequestEvaluation();
        }

        private void Awake()
        {
            if (resultPanel != null) resultPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (gridManager != null) gridManager.BlockPlaced += HandleBlockPlaced;
            if (blockSpawner != null) blockSpawner.TrayChanged += HandleTrayChanged;
        }

        private void Start()
        {
            if (!ValidateReferences())
            {
                Debug.LogError("[Adventure] Missing Inspector references.");
                enabled = false;
                return;
            }
            if (resultStarRating == null)
                Debug.LogWarning("[Adventure] Result Star Rating is not assigned. " +
                    "Gameplay will run, but the star display will be missing.");

            int levelId = AdventureSession.SelectedLevelId;
            if (levelId < 1 || !AdventureProgress.IsUnlocked(levelId))
            {
                Debug.LogWarning("[Adventure] Invalid selected level.");
                ReturnToMapImmediate();
                return;
            }

            currentLevel = catalog.GetLevel(levelId);
            if (currentLevel == null)
            {
                Debug.LogError($"[Adventure] Level {levelId} not found.");
                ReturnToMapImmediate();
                return;
            }

            movesUsed = 0;
            totalLinesCleared = 0;
            totalBlocksPlaced = 0;
            extraMovePurchases = 0;
            failedDueToMoveLimit = false;
            gameFinished = false;
            initialized = true;

            if (resultExtraMovesOfferButton != null)
                resultExtraMovesOfferButton.gameObject.SetActive(false);

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Playing);

            RefreshObjectiveUI();
            RequestEvaluation();
            Debug.Log($"[Adventure] Level {currentLevel.LevelId} started.");
        }

        private bool ValidateReferences()
        {
            return catalog != null && gridManager != null && blockSpawner != null &&
                scoreManager != null && levelTitleText != null && objectiveText != null &&
                progressValueText != null && movesText != null && progressFill != null &&
                resultPanel != null && resultTitleText != null &&
                resultDescriptionText != null && resultScoreText != null;
        }

        private void OnDisable()
        {
            if (gridManager != null) gridManager.BlockPlaced -= HandleBlockPlaced;
            if (blockSpawner != null) blockSpawner.TrayChanged -= HandleTrayChanged;
            if (pendingEvaluation != null)
            {
                StopCoroutine(pendingEvaluation);
                pendingEvaluation = null;
            }
        }

        private void HandleBlockPlaced(BlockPlacementResult placement)
        {
            if (!initialized || gameFinished || placement == null) return;
            movesUsed++;
            totalBlocksPlaced++;
            if (placement.LineClear != null)
                totalLinesCleared += placement.LineClear.TotalLines;
            RefreshObjectiveUI();
            RequestEvaluation();
        }

        private void HandleTrayChanged()
        {
            if (!initialized || gameFinished) return;
            RequestEvaluation();
        }

        private int GetObjectiveProgress()
        {
            if (currentLevel == null) return 0;
            switch (currentLevel.Objective)
            {
                case AdventureObjective.ReachScore: return scoreManager.CurrentScore;
                case AdventureObjective.ClearLines: return totalLinesCleared;
                case AdventureObjective.PlaceBlocks: return totalBlocksPlaced;
                default: return 0;
            }
        }

        private string GetObjectiveLabel()
        {
            if (currentLevel == null) return string.Empty;
            switch (currentLevel.Objective)
            {
                case AdventureObjective.ReachScore: return "REACH SCORE";
                case AdventureObjective.ClearLines: return "CLEAR LINES";
                case AdventureObjective.PlaceBlocks: return "PLACE BLOCKS";
                default: return "OBJECTIVE";
            }
        }

        private void RefreshObjectiveUI()
        {
            if (currentLevel == null) return;
            int progress = GetObjectiveProgress();
            int target = currentLevel.TargetValue;
            levelTitleText.text = $"LEVEL {currentLevel.LevelId} — " +
                currentLevel.LevelName.ToUpperInvariant();
            objectiveText.text = GetObjectiveLabel();
            progressValueText.text = $"{Mathf.Min(progress, target)} / {target}";
            movesText.text = $"MOVES {movesUsed} / {EffectiveMoveLimit}";
            progressFill.fillAmount = target > 0
                ? Mathf.Clamp01((float)progress / target) : 0f;
        }

        private void RequestEvaluation()
        {
            if (!isActiveAndEnabled || !initialized || gameFinished) return;
            if (pendingEvaluation != null) StopCoroutine(pendingEvaluation);
            pendingEvaluation = StartCoroutine(EvaluateNextFrame());
        }

        private IEnumerator EvaluateNextFrame()
        {
            yield return null;
            pendingEvaluation = null;
            if (gameFinished || currentLevel == null) yield break;
            RefreshObjectiveUI();

            if (GetObjectiveProgress() >= currentLevel.TargetValue)
            {
                CompleteLevel();
                yield break;
            }
            if (movesUsed >= EffectiveMoveLimit)
            {
                FailLevel("NO MOVES LEFT");
                yield break;
            }
            if (gridManager.GetCell(0, 0) == null) yield break;

            bool anyBlockAvailable = false;
            bool hasValidMove = false;
            foreach (BlockView block in blockSpawner.ActiveBlocks)
            {
                if (block == null || block.Definition == null) continue;
                anyBlockAvailable = true;
                if (gridManager.HasAnyValidPlacement(block.Definition))
                {
                    hasValidMove = true;
                    break;
                }
            }
            if (anyBlockAvailable && !hasValidMove)
                FailLevel("NO AVAILABLE PLACEMENTS");
        }

        private void CompleteLevel()
        {
            if (gameFinished) return;
            gameFinished = true;
            failedDueToMoveLimit = false;

            // Extra moves do not inflate the star rating: grades still use the
            // original move limit. Going beyond it yields at most one star.
            int starRatingMoveLimit = Mathf.Max(currentLevel.MoveLimit, movesUsed);
            AdventureCompletionResult completion = AdventureSaveService.CompleteLevel(
                currentLevel.LevelId, scoreManager.CurrentScore,
                movesUsed, starRatingMoveLimit);
            AdventureLevelRecord record = AdventureSaveService.GetLevelRecord(currentLevel.LevelId);

            if (resultCoinsText != null)
                resultCoinsText.text = completion.CoinsEarned > 0
                    ? $"+{completion.CoinsEarned} COINS" : "REWARD ALREADY CLAIMED";
            if (resultBestText != null)
                resultBestText.text = $"BEST SCORE: {record.bestScore:N0}\nBEST MOVES: {record.bestMoves}";

            ShowResult(true, "LEVEL COMPLETE!", "OBJECTIVE COMPLETED");
            UpdateResultStars(completion.EarnedStars, true);
            Debug.Log($"[Adventure] Level {currentLevel.LevelId} completed. " +
                $"Stars: {completion.EarnedStars}, Coins: +{completion.CoinsEarned}");
        }

        private void FailLevel(string reason)
        {
            if (gameFinished) return;
            gameFinished = true;
            failedDueToMoveLimit = reason == "NO MOVES LEFT";
            if (resultCoinsText != null) resultCoinsText.text = string.Empty;
            if (resultBestText != null) resultBestText.text = string.Empty;
            ShowResult(false, "LEVEL FAILED", reason);
            UpdateResultStars(0, false);
            if (feedbackController != null) feedbackController.PlayGameOver();
            Debug.Log($"[Adventure] Level {currentLevel.LevelId} failed: {reason}");
        }

        private void UpdateResultStars(int earnedStars, bool visible)
        {
            if (resultStarRating == null) return;
            if (!visible)
            {
                resultStarRating.SetStars(0);
                resultStarRating.gameObject.SetActive(false);
                return;
            }
            resultStarRating.gameObject.SetActive(true);
            resultStarRating.SetStars(Mathf.Clamp(earnedStars, 0, 3));
            Debug.Log($"[Adventure] Result stars: {resultStarRating.CurrentStars}");
        }

        private void ShowResult(bool completed, string title, string description)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.GameOver);
            foreach (BlockView block in blockSpawner.ActiveBlocks)
            {
                if (block == null) continue;
                BlockDragHandler drag = block.GetComponent<BlockDragHandler>();
                if (drag != null) drag.enabled = false;
            }
            resultTitleText.text = title;
            resultDescriptionText.text = description;
            resultScoreText.text = $"SCORE {scoreManager.CurrentScore:N0}";
            if (nextLevelButton != null)
                nextLevelButton.SetActive(completed &&
                    catalog.ContainsLevel(currentLevel.LevelId + 1));
            if (retryButton != null)
                retryButton.SetActive(!completed);
            if (resultExtraMovesOfferButton != null)
                resultExtraMovesOfferButton.gameObject.SetActive(!completed && CanPurchaseExtraMoves);
            resultPanel.SetActive(true);
        }

        private void RestartLevelImmediate()
        {
            if (currentLevel == null) return;
            AdventureSession.SelectLevel(currentLevel.LevelId);
            SceneManager.LoadScene("AdventureGame");
        }
        public void RestartLevel() { RunAdTransition(RestartLevelImmediate); }

        private void NextLevelImmediate()
        {
            if (currentLevel == null || !gameFinished) return;
            int nextLevel = currentLevel.LevelId + 1;
            if (!catalog.ContainsLevel(nextLevel) ||
                !AdventureProgress.IsUnlocked(nextLevel))
            {
                ReturnToMapImmediate();
                return;
            }
            AdventureSession.SelectLevel(nextLevel);
            SceneManager.LoadScene("AdventureGame");
        }
        public void NextLevel() { RunAdTransition(NextLevelImmediate); }

        private void ReturnToMapImmediate()
        {
            AdventureSession.Clear();
            SceneManager.LoadScene("AdventureMap");
        }
        public void ReturnToMap()
        {
            if (!gameFinished)
            {
                ReturnToMapImmediate();
                return;
            }
            RunAdTransition(ReturnToMapImmediate);
        }
        private void RunAdTransition(Action transition)
        {
            if (AdMobManager.Instance == null)
            {
                transition?.Invoke();
                return;
            }
            AdMobManager.Instance.RunAtTransition(transition);
        }
    }
}

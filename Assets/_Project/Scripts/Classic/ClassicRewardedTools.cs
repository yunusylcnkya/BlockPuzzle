using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Blocks;
using BlockPuzzle.Core;
using BlockPuzzle.Gameplay;
using BlockPuzzle.Grid;
using BlockPuzzle.Monetization;

namespace BlockPuzzle.Classic
{
    /// <summary>
    /// Classic-only rewarded undo and five rewarded tray shuffles per saved run.
    /// No changes to Adventure's economy, rewards, or one-step paid Undo.
    /// Attach using Tools > Block Puzzle > v2.0 > Install Classic Rewarded UI.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ClassicRewardedTools : MonoBehaviour
    {
        private const int MaximumShuffles = 5;

        [Header("Gameplay references")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private RectTransform gridRoot;
        [SerializeField] private BlockSpawner blockSpawner;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private ClassicGameController classicGame;
        [SerializeField] private ClassicRunController classicRun;

        [Header("Buttons - bound by editor installer")]
        [SerializeField] private Button undoAdButton;
        [SerializeField] private Button shuffleAdButton;
        [SerializeField] private Button continueAdButton;

        private enum RewardAction { Undo, Shuffle, Continue }

        private struct CellState
        {
            public bool Occupied;
            public int BlockId;
            public Color Color;
        }

        private sealed class TurnSnapshot
        {
            public CellState[,] Cells;
            public BlockDefinition[] Tray;
            public ScoreManager.ScoreState Score;
        }

        private GridCellView[,] views;
        private TurnSnapshot pending;
        private TurnSnapshot available;
        private AdMobManager ads;
        private bool waitingForAd;
        private bool rewardReceived;
        private int currentRewardRequest;
        private float nextRefresh;

        // The Classic save must remain open while an in-memory last-turn rescue
        // is possible. Game Over with no rescue retains previous close behavior.
        public bool CanRescue => available != null && classicGame != null &&
                                 classicGame.IsGameOver && CanRestore(available);

        private bool IsPlaying => classicGame != null && !classicGame.IsGameOver &&
            GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing;

        private bool HasSetup => gridManager != null && gridRoot != null &&
            blockSpawner != null && scoreManager != null && classicGame != null &&
            classicRun != null && gridManager.GetCell(0, 0) != null;

        private void OnEnable()
        {
            BlockDragHandler.BeforeValidPlacement += BeforePlacement;
            BlockDragHandler.AfterPlacement += AfterPlacement;
            if (blockSpawner != null)
                blockSpawner.RerollCompleted += OnTrayRerolled;
            if (undoAdButton != null) undoAdButton.onClick.AddListener(WatchUndo);
            if (shuffleAdButton != null) shuffleAdButton.onClick.AddListener(WatchShuffle);
            if (continueAdButton != null) continueAdButton.onClick.AddListener(WatchContinue);
        }

        private void Start()
        {
            ads = AdMobManager.Instance;
            if (ads != null) ads.RewardedStateChanged += OnRewardedStateChanged;
            RefreshButtons();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.25f;
            RefreshButtons();
        }

        private void OnDisable()
        {
            BlockDragHandler.BeforeValidPlacement -= BeforePlacement;
            BlockDragHandler.AfterPlacement -= AfterPlacement;
            if (blockSpawner != null)
                blockSpawner.RerollCompleted -= OnTrayRerolled;
            if (ads != null) ads.RewardedStateChanged -= OnRewardedStateChanged;
            if (undoAdButton != null) undoAdButton.onClick.RemoveListener(WatchUndo);
            if (shuffleAdButton != null) shuffleAdButton.onClick.RemoveListener(WatchShuffle);
            if (continueAdButton != null) continueAdButton.onClick.RemoveListener(WatchContinue);
            pending = null;
            available = null;
        }

        private bool EnsureViews()
        {
            if (!HasSetup) return false;
            int rows = gridManager.Rows, columns = gridManager.Columns;
            if (views != null && views.GetLength(0) == rows &&
                views.GetLength(1) == columns) return true;

            views = new GridCellView[rows, columns];
            foreach (GridCellView view in gridRoot.GetComponentsInChildren<GridCellView>(true))
            {
                if (view.Cell == null) continue;
                int row = view.Cell.Row, col = view.Cell.Column;
                if (row >= 0 && row < rows && col >= 0 && col < columns)
                    views[row, col] = view;
            }
            for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
                if (views[row, col] == null) return false;
            return true;
        }

        private TurnSnapshot CaptureCurrentTurn(BlockDefinition[] tray)
        {
            if (!EnsureViews() || tray == null) return null;
            var state = new CellState[gridManager.Rows, gridManager.Columns];
            for (int row = 0; row < gridManager.Rows; row++)
            for (int col = 0; col < gridManager.Columns; col++)
            {
                GridCell cell = gridManager.GetCell(row, col);
                if (cell == null) return null;
                state[row, col] = new CellState
                {
                    Occupied = cell.IsOccupied,
                    BlockId = cell.BlockId,
                    Color = views[row, col].GetComponent<Image>().color
                };
            }
            return new TurnSnapshot
            {
                Cells = state,
                Tray = (BlockDefinition[])tray.Clone(),
                Score = scoreManager.CaptureScoreState()
            };
        }

        private void BeforePlacement(BlockDragHandler handler)
        {
            pending = null;
            if (!HasSetup || !IsPlaying || waitingForAd ||
                !classicRun.IsReady) return;
            pending = CaptureCurrentTurn(blockSpawner.CaptureTraySnapshot());
        }

        private void AfterPlacement(bool success)
        {
            if (success && pending != null) available = pending;
            pending = null;
            RefreshButtons();
        }

        private void OnTrayRerolled()
        {
            // A paid / rewarded reshuffle changes the tray and invalidates Undo.
            pending = null;
            available = null;
            RefreshButtons();
        }

        private bool CanRestore(TurnSnapshot snapshot)
        {
            return snapshot != null && snapshot.Cells != null && HasSetup &&
                snapshot.Cells.GetLength(0) == gridManager.Rows &&
                snapshot.Cells.GetLength(1) == gridManager.Columns &&
                !blockSpawner.IsAnyBlockDragging &&
                blockSpawner.CanRestoreTraySnapshot(snapshot.Tray);
        }

        private bool CanUndoNow => !waitingForAd && classicRun != null &&
                                   classicRun.IsReady && CanRestore(available);

        private bool CanShuffleNow => !waitingForAd && IsPlaying &&
            classicRun != null && classicRun.IsReady &&
            classicRun.RewardedRerollsUsed < MaximumShuffles &&
            !blockSpawner.IsAnyBlockDragging &&
            blockSpawner.CanRerollRemainingBlocks();

        private void WatchUndo() => RequestReward(RewardAction.Undo);
        private void WatchShuffle() => RequestReward(RewardAction.Shuffle);
        private void WatchContinue() => RequestReward(RewardAction.Continue);

        private void RequestReward(RewardAction action)
        {
            if (waitingForAd) return;
            bool allowed = action == RewardAction.Shuffle ? CanShuffleNow :
                action == RewardAction.Continue ? IsGameOverRescueAvailable() :
                IsPlaying && CanUndoNow;
            if (!allowed) return;

            AdMobManager manager = ads != null ? ads : AdMobManager.Instance;
            if (manager == null || !manager.IsRewardedReady)
            {
                Debug.LogWarning("[ClassicAds] Rewarded ad is not ready.");
                return;
            }

            waitingForAd = true;
            rewardReceived = false;
            int requestId = ++currentRewardRequest;
            if (!manager.ShowRewardedForGameplay(() => OnRewardEarned(action, requestId)))
                waitingForAd = false;
            RefreshButtons();
        }

        private bool IsGameOverRescueAvailable() => CanUndoNow &&
            classicGame != null && classicGame.IsGameOver;

        private void OnRewardEarned(RewardAction action, int requestId)
        {
            // A platform callback may arrive just AFTER the full-screen close
            // notification. The request id accepts that genuine late reward,
            // while rejecting callbacks from a superseded ad request.
            if (this == null || requestId != currentRewardRequest || rewardReceived) return;
            rewardReceived = true;
            bool success = false;
            try
            {
                if (action == RewardAction.Shuffle)
                {
                    if (classicRun != null && classicRun.IsReady &&
                        classicRun.RewardedRerollsUsed < MaximumShuffles && IsPlaying &&
                        blockSpawner.CanRerollRemainingBlocks())
                    {
                        success = blockSpawner.TryRerollRemainingBlocks(() => true);
                        if (success) classicRun.RegisterRewardedReroll();
                    }
                }
                else if (CanRestore(available) &&
                    (IsPlaying || classicGame.IsGameOver))
                {
                    success = RestorePreviousTurn();
                }
                Debug.Log("[ClassicAds] Rewarded action " + action + ": " + success);
            }
            finally
            {
                waitingForAd = false;
                RefreshButtons();
            }
        }

        private void OnRewardedStateChanged()
        {
            // A closed/dismissed advertisement must never leave the buttons
            // locked forever. No reward callback => no gameplay change.
            if (waitingForAd && ads != null && !ads.IsFullScreenAdShowing &&
                !rewardReceived)
                waitingForAd = false;
            RefreshButtons();
        }

        private bool RestorePreviousTurn()
        {
            TurnSnapshot snapshot = available;
            if (!CanRestore(snapshot)) return false;
            TurnSnapshot before = CaptureCurrentTurn(blockSpawner.CaptureTraySnapshot());
            if (before == null) return false;

            if (!ApplyTurn(snapshot))
            {
                if (!ApplyTurn(before))
                    Debug.LogError("[ClassicAds] Undo rollback failed; inspect scene.");
                return false;
            }

            available = null;
            pending = null;
            if (classicGame.IsGameOver)
                classicGame.ResumeAfterRewardedUndo();
            classicRun.SaveAfterRewardedAction();
            return true;
        }

        private bool ApplyTurn(TurnSnapshot snapshot)
        {
            if (!CanRestore(snapshot)) return false;
            gridManager.ClearPreview();
            for (int row = 0; row < gridManager.Rows; row++)
            for (int col = 0; col < gridManager.Columns; col++)
                gridManager.ClearCell(row, col);

            for (int row = 0; row < gridManager.Rows; row++)
            for (int col = 0; col < gridManager.Columns; col++)
            {
                CellState cell = snapshot.Cells[row, col];
                if (cell.Occupied && !gridManager.TryOccupyCell(row, col,
                        cell.BlockId, cell.Color))
                    return false;
            }
            if (!blockSpawner.RestoreTraySnapshot(snapshot.Tray)) return false;
            scoreManager.RestoreScoreState(snapshot.Score);
            return true;
        }

        private void RefreshButtons()
        {
            if (classicRun == null) return;
            bool adReady = ads != null && ads.IsRewardedReady;
            if (undoAdButton != null)
            {
                undoAdButton.gameObject.SetActive(IsPlaying);
                undoAdButton.interactable = adReady && CanUndoNow;
                SetText(undoAdButton, "WATCH AD - UNDO");
            }
            if (shuffleAdButton != null)
            {
                shuffleAdButton.gameObject.SetActive(IsPlaying);
                shuffleAdButton.interactable = adReady && CanShuffleNow;
                int left = Mathf.Max(0, MaximumShuffles - classicRun.RewardedRerollsUsed);
                SetText(shuffleAdButton, "AD SHUFFLE (" + left + ")");
            }
            if (continueAdButton != null)
            {
                continueAdButton.gameObject.SetActive(IsGameOverRescueAvailable() ||
                    (waitingForAd && classicGame != null && classicGame.IsGameOver));
                continueAdButton.interactable = adReady && IsGameOverRescueAvailable();
                SetText(continueAdButton, "WATCH AD & CONTINUE");
            }
        }

        private static void SetText(Button button, string text)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null && label.text != text) label.text = text;
        }
    }
}

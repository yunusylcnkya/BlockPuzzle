using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Blocks;
using BlockPuzzle.ClassicPersistence;
using BlockPuzzle.Core;
using BlockPuzzle.Gameplay;
using BlockPuzzle.Grid;
using BlockPuzzle.SaveSystem;

namespace BlockPuzzle.Classic
{
    /// <summary>
    /// Attach ONLY in ClassicGame. Saves after a committed drag, not while dragging.
    /// Automatically restores when entering ClassicGame through the existing menu.
    /// The existing ClassicGameController stays unchanged.
    /// </summary>
    public sealed class ClassicRunController : MonoBehaviour
    {
        [Header("Existing ClassicGame references")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private RectTransform gridRoot;
        [SerializeField] private BlockSpawner blockSpawner;
        [SerializeField] private ScoreManager scoreManager;

        private GridCellView[,] views;
        private bool ready;
        private bool restoring;
        private bool protectExistingSave;
        private bool runClosed;
        private int rewardedRerollsUsed;
        public int RewardedRerollsUsed => rewardedRerollsUsed;
        public bool IsReady => ready && !restoring && !protectExistingSave && !runClosed;

        /// <summary>Called after an ad-earned reshuffle, at most five per run.</summary>
        public void RegisterRewardedReroll()
        {
            if (!IsReady || rewardedRerollsUsed >= 5) return;
            rewardedRerollsUsed++;
            SaveCommittedState();
        }

        /// <summary>Save the board/tray after a successful rewarded Undo.</summary>
        public void SaveAfterRewardedAction()
        {
            if (IsReady) SaveCommittedState();
        }

        private void OnEnable()
        {
            BlockDragHandler.AfterPlacement += HandleAfterPlacement;
        }

        private IEnumerator Start()
        {
            // GridManager and BlockSpawner generate their UI in Start().
            yield return null;
            if (!EnsureViews())
            {
                Debug.LogError("[ClassicContinue] Missing references or grid views. Save disabled.");
                protectExistingSave = true;
                yield break;
            }

            if (ClassicRunSaveService.TryLoad(out ClassicRunSnapshot snapshot))
            {
                restoring = true;
                bool restored = TryRestore(snapshot);
                restoring = false;
                if (!restored)
                {
                    // Do not overwrite a valid save whose assets no longer match.
                    protectExistingSave = true;
                    Debug.LogError("[ClassicContinue] Restore rejected. Existing save protected.");
                }
                else
                {
                    rewardedRerollsUsed = snapshot.rewardedRerollsUsed;
                    Debug.Log("[ClassicContinue] Restored Classic run; score=" + snapshot.score +
                              ", cells=" + snapshot.occupiedCells.Length);
                }
            }
            else if (ClassicRunSaveService.RecoveryBlocked)
            {
                protectExistingSave = true;
                Debug.LogError("[ClassicContinue] Corrupt save protected. " +
                               "Use explicit New Game to discard it.");
            }
            ready = true;
        }

        private bool EnsureViews()
        {
            if (gridManager == null || gridRoot == null || blockSpawner == null ||
                scoreManager == null || gridManager.Rows != 8 || gridManager.Columns != 8 ||
                gridManager.GetCell(0, 0) == null)
                return false;

            views = new GridCellView[gridManager.Rows, gridManager.Columns];
            foreach (GridCellView view in gridRoot.GetComponentsInChildren<GridCellView>(true))
            {
                if (view.Cell == null) continue;
                int r = view.Cell.Row;
                int c = view.Cell.Column;
                if (r >= 0 && r < gridManager.Rows && c >= 0 && c < gridManager.Columns)
                    views[r, c] = view;
            }
            for (int r = 0; r < gridManager.Rows; r++)
            for (int c = 0; c < gridManager.Columns; c++)
                if (views[r, c] == null) return false;
            return true;
        }

        private bool TryRestore(ClassicRunSnapshot snapshot)
        {
            if (!ClassicRunValidator.Validate(snapshot, out string reason))
            {
                Debug.LogError("[ClassicContinue] Invalid snapshot: " + reason);
                return false;
            }
            if (snapshot.rows != gridManager.Rows || snapshot.columns != gridManager.Columns ||
                !blockSpawner.TryResolveTrayBlockIds(snapshot.trayBlockIds,
                    out BlockDefinition[] restoredTray))
            {
                Debug.LogError("[ClassicContinue] Unknown block ID or incompatible board.");
                return false;
            }

            // Pre-flight before changing the live board or tray.
            if (!blockSpawner.CanRestoreTraySnapshot(restoredTray)) return false;
            BlockDefinition[] originalTray = blockSpawner.CaptureTraySnapshot();
            if (originalTray == null) return false;
            for (int r = 0; r < gridManager.Rows; r++)
            for (int c = 0; c < gridManager.Columns; c++)
                if (gridManager.GetCell(r, c).IsOccupied) return false;

            gridManager.ClearPreview();
            int maxBlockId = 0;
            var applied = new List<ClassicOccupiedCell>();
            foreach (ClassicOccupiedCell state in snapshot.occupiedCells)
            {
                Color color = new Color(state.red, state.green, state.blue, state.alpha);
                if (!gridManager.TryOccupyCell(state.row, state.column, state.blockId, color))
                {
                    RollBackBoard(applied, originalTray);
                    return false;
                }
                applied.Add(state);
                maxBlockId = Mathf.Max(maxBlockId, state.blockId);
            }
            if (!blockSpawner.RestoreTraySnapshot(restoredTray))
            {
                RollBackBoard(applied, originalTray);
                return false;
            }

            scoreManager.RestoreScoreState(new ScoreManager.ScoreState(
                snapshot.score, snapshot.combo, snapshot.lastEarnedPoints,
                snapshot.lastClearedLines, snapshot.multiplier));
            BlockDragHandler.EnsureNextBlockIdAtLeast(maxBlockId + 1);
            return true;
        }

        private void RollBackBoard(List<ClassicOccupiedCell> applied,
            BlockDefinition[] originalTray)
        {
            foreach (ClassicOccupiedCell cell in applied)
                gridManager.ClearCell(cell.row, cell.column);
            if (!blockSpawner.RestoreTraySnapshot(originalTray))
                Debug.LogError("[ClassicContinue] Could not restore original tray.");
        }

        private void HandleAfterPlacement(bool placed)
        {
            if (placed) SaveCommittedState();
        }

        private bool Playing => GameManager.Instance == null ||
                                GameManager.Instance.CurrentState == GameState.Playing;

        private void SaveCommittedState()
        {
            if (!ready || restoring || protectExistingSave || runClosed || !Playing ||
                blockSpawner == null || blockSpawner.IsAnyBlockDragging || !EnsureViews())
                return;

            BlockDefinition[] tray = blockSpawner.CaptureTraySnapshot();
            if (tray == null || tray.Length != 3) return;

            var cells = new List<ClassicOccupiedCell>();
            for (int r = 0; r < gridManager.Rows; r++)
            for (int c = 0; c < gridManager.Columns; c++)
            {
                GridCell cell = gridManager.GetCell(r, c);
                if (!cell.IsOccupied) continue;
                Color color = views[r, c].GetComponent<Image>().color;
                cells.Add(new ClassicOccupiedCell
                {
                    row = r,
                    column = c,
                    blockId = cell.BlockId,
                    red = color.r,
                    green = color.g,
                    blue = color.b,
                    alpha = color.a
                });
            }

            ScoreManager.ScoreState score = scoreManager.CaptureScoreState();
            var snapshot = new ClassicRunSnapshot
            {
                rows = gridManager.Rows,
                columns = gridManager.Columns,
                score = score.Score,
                combo = score.Combo,
                lastEarnedPoints = score.LastEarned,
                lastClearedLines = score.LastLines,
                multiplier = score.Multiplier,
                occupiedCells = cells.ToArray(),
                rewardedRerollsUsed = rewardedRerollsUsed,
                trayBlockIds = new string[3]
            };
            for (int i = 0; i < tray.Length; i++)
                snapshot.trayBlockIds[i] = tray[i] == null ? "" : tray[i].BlockId;

            if (!ClassicRunSaveService.TrySave(snapshot))
                Debug.LogWarning("[ClassicContinue] Latest move could not be saved.");
        }

        private void Update()
        {
            if (ready && !runClosed && !protectExistingSave &&
                GameManager.Instance != null &&
                GameManager.Instance.CurrentState == GameState.GameOver)
            {
                ClassicRewardedTools tools = GetComponent<ClassicRewardedTools>();
                if (tools == null || !tools.CanRescue) EndRun();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveCommittedState();
        }

        private void OnApplicationQuit()
        {
            SaveCommittedState();
        }

        private void EndRun()
        {
            if (runClosed) return;
            runClosed = true;
            ClassicRunSaveService.CloseRun();
            Debug.Log("[ClassicContinue] Game Over: session closed, best score retained.");
        }

        private void OnDisable()
        {
            BlockDragHandler.AfterPlacement -= HandleAfterPlacement;
            if (ready && !protectExistingSave && !runClosed &&
                GameManager.Instance != null &&
                GameManager.Instance.CurrentState == GameState.GameOver)
            {
                ClassicRewardedTools tools = GetComponent<ClassicRewardedTools>();
                if (tools == null || !tools.CanRescue) EndRun();
            }
        }
    }
}

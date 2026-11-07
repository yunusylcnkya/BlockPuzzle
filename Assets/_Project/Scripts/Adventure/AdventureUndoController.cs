using System;
using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Blocks;
using BlockPuzzle.Core;
using BlockPuzzle.Gameplay;
using BlockPuzzle.Grid;
using BlockPuzzle.SaveSystem;

namespace BlockPuzzle.Adventure
{
    /// <summary>
    /// One-step paid undo. Captures the board BEFORE a valid drop, commits only
    /// after a successful drop, and restores the board/score/progress/tray together.
    /// Attach only in AdventureGame, not ClassicGame.
    /// </summary>
    public sealed class AdventureUndoController : MonoBehaviour
    {
        [Header("Required references")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private RectTransform gridRoot;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private BlockSpawner blockSpawner;
        [SerializeField] private AdventureGameController adventureGame;

        private GridCellView[,] views;
        private TurnSnapshot pending;
        private TurnSnapshot available;
        private bool applyingUndo;

        public event Action AvailabilityChanged;

        public bool CanUndo =>
            !applyingUndo && available != null &&
            adventureGame != null && adventureGame.CanUndoMove &&
            blockSpawner != null && !blockSpawner.IsAnyBlockDragging &&
            GameManager.Instance != null &&
            GameManager.Instance.CurrentState == GameState.Playing;

        private sealed class TurnSnapshot
        {
            public CellState[,] Cells;
            public BlockDefinition[] Tray;
            public ScoreManager.ScoreState Score;
            public AdventureGameController.UndoProgressState Progress;
        }

        private struct CellState
        {
            public bool Occupied;
            public int BlockId;
            public Color Color;
        }

        private void OnEnable()
        {
            BlockDragHandler.BeforeValidPlacement += HandleBeforePlacement;
            BlockDragHandler.AfterPlacement += HandleAfterPlacement;
            if (blockSpawner != null)
                blockSpawner.RerollCompleted += InvalidateUndo;
        }

        private void OnDisable()
        {
            BlockDragHandler.BeforeValidPlacement -= HandleBeforePlacement;
            BlockDragHandler.AfterPlacement -= HandleAfterPlacement;
            if (blockSpawner != null)
                blockSpawner.RerollCompleted -= InvalidateUndo;
            pending = null;
            available = null;
        }

        private bool EnsureViews()
        {
            if (gridManager == null || gridRoot == null || scoreManager == null ||
                blockSpawner == null || adventureGame == null)
            {
                Debug.LogError("[Undo] Missing Inspector references.");
                return false;
            }

            int rows = gridManager.Rows;
            int columns = gridManager.Columns;
            if (views != null && views.GetLength(0) == rows &&
                views.GetLength(1) == columns)
                return true;

            views = new GridCellView[rows, columns];
            foreach (GridCellView view in gridRoot.GetComponentsInChildren<GridCellView>(true))
            {
                if (view.Cell == null) continue;
                int row = view.Cell.Row;
                int column = view.Cell.Column;
                if (row >= 0 && row < rows && column >= 0 && column < columns)
                    views[row, column] = view;
            }

            for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
            {
                if (views[row, col] == null || gridManager.GetCell(row, col) == null)
                {
                    Debug.LogError("[Undo] Grid cell views not initialized.");
                    views = null;
                    return false;
                }
            }
            return true;
        }

        private void HandleBeforePlacement(BlockDragHandler source)
        {
            pending = null;
            if (applyingUndo || adventureGame == null ||
                !adventureGame.CanUndoMove || !EnsureViews())
                return;

            BlockDefinition[] tray = blockSpawner.CaptureTraySnapshot();
            if (tray == null) return;

            int rows = gridManager.Rows;
            int columns = gridManager.Columns;
            CellState[,] cells = new CellState[rows, columns];
            for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
            {
                GridCell cell = gridManager.GetCell(row, col);
                // Occupied colors are stable outside row-clear animations.
                Color blockColor = views[row, col].GetComponent<Image>().color;
                cells[row, col] = new CellState
                {
                    Occupied = cell.IsOccupied,
                    BlockId = cell.BlockId,
                    Color = blockColor
                };
            }

            pending = new TurnSnapshot
            {
                Cells = cells,
                Tray = tray,
                Score = scoreManager.CaptureScoreState(),
                Progress = adventureGame.CaptureUndoProgress()
            };
        }

        private void HandleAfterPlacement(bool succeeded)
        {
            if (succeeded)
            {
                // A new successful move replaces the previous undo, even if
                // capturing the new state failed (never undo the wrong move).
                available = pending;
                AvailabilityChanged?.Invoke();
            }
            pending = null;
        }

        public bool TryPurchaseUndo(int price)
        {
            if (price <= 0 || !CanUndo || !EnsureViews() ||
                AdventureSaveService.TotalCoins < price)
                return false;

            TurnSnapshot snapshot = available;
            if (snapshot.Cells.GetLength(0) != gridManager.Rows ||
                snapshot.Cells.GetLength(1) != gridManager.Columns ||
                !blockSpawner.CanRestoreTraySnapshot(snapshot.Tray))
                return false;

            // This is a single-threaded game action. Restore known-good
            // snapshots first; charge only after the restore reports success.
            applyingUndo = true;
            try
            {
                gridManager.ClearPreview();
                for (int row = 0; row < gridManager.Rows; row++)
                for (int col = 0; col < gridManager.Columns; col++)
                    gridManager.ClearCell(row, col);

                for (int row = 0; row < gridManager.Rows; row++)
                for (int col = 0; col < gridManager.Columns; col++)
                {
                    CellState state = snapshot.Cells[row, col];
                    if (state.Occupied)
                        gridManager.TryOccupyCell(row, col, state.BlockId, state.Color);
                }

                scoreManager.RestoreScoreState(snapshot.Score);
                adventureGame.RestoreUndoProgress(snapshot.Progress);
                if (!blockSpawner.RestoreTraySnapshot(snapshot.Tray))
                {
                    Debug.LogError("[Undo] Tray restore failed; no coins charged.");
                    available = null;
                    return false;
                }

                // Re-validate the balance at payment time. Only a successful
                // purchase consumes the snapshot, so it cannot be reused.
                available = null;
                pending = null;
                if (!AdventureSaveService.TrySpendCoins(price))
                {
                    Debug.LogError("[Undo] Coin charge failed after restore.");
                    return false;
                }

                Debug.Log($"[Undo] Last move restored for {price} coins.");
                return true;
            }
            finally
            {
                applyingUndo = false;
                AvailabilityChanged?.Invoke();
            }
        }

        private void InvalidateUndo()
        {
            available = null;
            pending = null;
            AvailabilityChanged?.Invoke();
        }
    }
}

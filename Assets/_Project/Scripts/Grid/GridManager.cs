
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Blocks;

namespace BlockPuzzle.Grid
{
    public sealed class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField, Min(1)] private int rows = 8;
        [SerializeField, Min(1)] private int columns = 8;
        [SerializeField, Min(0)] private float spacing = 4f;

        [Header("References")]
        [SerializeField] private RectTransform gridRoot;
        [SerializeField] private GridCellView cellPrefab;

        private GridCell[,] cells;
        private GridCellView[,] cellViews;
        private GridLayoutGroup gridLayout;

        private readonly List<Vector2Int> previewCells = new();

        private Vector2 lastGridSize;

        // Other systems can subscribe to line clears.
        public event Action<LineClearResult> LinesCleared;
        public event Action<BlockPlacementResult> BlockPlaced;

        public int Rows => rows;
        public int Columns => columns;

        public float CellStep =>
            gridLayout == null
                ? 0f
                : gridLayout.cellSize.x + gridLayout.spacing.x;

        private void Awake()
        {
            if (gridRoot != null)
                gridLayout = gridRoot.GetComponent<GridLayoutGroup>();
        }

        private void Start()
        {
            GenerateGrid();
        }

        private void LateUpdate()
        {
            if (gridRoot == null || gridLayout == null)
                return;

            if ((gridRoot.rect.size - lastGridSize)
                .sqrMagnitude > 0.01f)
            {
                ConfigureGridLayout();
            }
        }

        private void ConfigureGridLayout()
        {
            float availableWidth =
                gridRoot.rect.width -
                gridLayout.padding.horizontal;

            float availableHeight =
                gridRoot.rect.height -
                gridLayout.padding.vertical;

            float cellWidth =
                (availableWidth - (columns - 1) * spacing)
                / columns;

            float cellHeight =
                (availableHeight - (rows - 1) * spacing)
                / rows;

            float cellSize = Mathf.Max(
                1f,
                Mathf.Min(cellWidth, cellHeight)
            );

            gridLayout.cellSize =
                new Vector2(cellSize, cellSize);

            gridLayout.spacing =
                new Vector2(spacing, spacing);

            gridLayout.startCorner =
                GridLayoutGroup.Corner.UpperLeft;

            gridLayout.startAxis =
                GridLayoutGroup.Axis.Horizontal;

            gridLayout.childAlignment =
                TextAnchor.MiddleCenter;

            gridLayout.constraint =
                GridLayoutGroup.Constraint.FixedColumnCount;

            gridLayout.constraintCount = columns;

            lastGridSize = gridRoot.rect.size;

            LayoutRebuilder.ForceRebuildLayoutImmediate(
                gridRoot
            );
        }

        public void GenerateGrid()
        {
            if (gridRoot == null ||
                cellPrefab == null ||
                gridLayout == null)
            {
                Debug.LogError(
                    "[GridManager] Missing references."
                );
                return;
            }

            if (cells != null)
                return;

            ConfigureGridLayout();

            cells = new GridCell[rows, columns];
            cellViews = new GridCellView[rows, columns];

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    GridCell cell = new GridCell(row, column);

                    GridCellView view = Instantiate(
                        cellPrefab, gridRoot
                    );

                    view.name = $"Cell_{row}_{column}";
                    view.Initialize(cell);

                    cells[row, column] = cell;
                    cellViews[row, column] = view;
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(
                gridRoot
            );

            Debug.Log(
                $"[GridManager] Generated {rows}x{columns} grid."
            );
        }

        public bool IsInsideGrid(int row, int column)
        {
            return row >= 0 && row < rows &&
                   column >= 0 && column < columns;
        }

        public GridCell GetCell(int row, int column)
        {
            if (cells == null || !IsInsideGrid(row, column))
                return null;

            return cells[row, column];
        }

        public bool TryOccupyCell(
            int row, int column, int blockId)
        {
            GridCell cell = GetCell(row, column);

            if (cell == null || !cell.TryOccupy(blockId))
                return false;

            cellViews[row, column].Refresh();
            return true;
        }

        public bool ClearCell(int row, int column)
        {
            GridCell cell = GetCell(row, column);

            if (cell == null)
                return false;

            cell.Clear();
            cellViews[row, column].Refresh();

            return true;
        }

        public bool TryGetPlacementFromScreenPosition(
            BlockDefinition definition,
            Vector2 screenPosition,
            Camera uiCamera,
            out int bottomRow,
            out int leftColumn)
        {
            bottomRow = -1;
            leftColumn = -1;

            if (cells == null ||
                definition == null ||
                !definition.IsValid())
            {
                return false;
            }

            if (!RectTransformUtility.RectangleContainsScreenPoint(
                gridRoot, screenPosition, uiCamera))
            {
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gridRoot,
                screenPosition,
                uiCamera,
                out Vector2 localPoint))
            {
                return false;
            }

            Vector3 firstCellWorld =
                cellViews[0, 0].transform.position;

            Vector3 firstCellLocal =
                gridRoot.InverseTransformPoint(firstCellWorld);

            float step = CellStep;

            if (step <= 0f)
                return false;

            float centerColumn =
                (localPoint.x - firstCellLocal.x) / step;

            float centerRow =
                (firstCellLocal.y - localPoint.y) / step;

            leftColumn = Mathf.RoundToInt(
                centerColumn -
                (definition.Width - 1) * 0.5f
            );

            bottomRow = Mathf.RoundToInt(
                centerRow +
                (definition.Height - 1) * 0.5f
            );

            return true;
        }

        public Vector3 GetPlacementCenterWorld(
            BlockDefinition definition,
            int bottomRow,
            int leftColumn)
        {
            Vector3 firstCellLocal =
                gridRoot.InverseTransformPoint(
                    cellViews[0, 0].transform.position
                );

            float step = CellStep;

            float centerColumn =
                leftColumn +
                (definition.Width - 1) * 0.5f;

            float centerRow =
                bottomRow -
                (definition.Height - 1) * 0.5f;

            Vector3 localPosition = new Vector3(
                firstCellLocal.x + centerColumn * step,
                firstCellLocal.y - centerRow * step,
                0f
            );

            return gridRoot.TransformPoint(localPosition);
        }

        public bool CanPlaceBlock(
            BlockDefinition definition,
            int bottomRow,
            int leftColumn)
        {
            if (definition == null || !definition.IsValid())
                return false;

            foreach (Vector2Int offset in definition.Cells)
            {
                int row = bottomRow - offset.y;
                int column = leftColumn + offset.x;

                GridCell cell = GetCell(row, column);

                if (cell == null || cell.IsOccupied)
                    return false;
            }

            return true;
        }


        public bool TryPlaceBlock(
            BlockDefinition definition,
            int bottomRow,
            int leftColumn,
            int blockId)
        {
            if (!CanPlaceBlock(
                definition, bottomRow, leftColumn))
            {
                return false;
            }

            ClearPreview();

            foreach (Vector2Int offset in definition.Cells)
            {
                int row = bottomRow - offset.y;
                int column = leftColumn + offset.x;

                TryOccupyCell(row, column, blockId);
            }

            LineClearResult lineResult = ResolveCompletedLines();

            if (lineResult.HasClearedLines)
            {
                Debug.Log(
                    "[GridManager] Lines cleared: " +
                    $"Rows={lineResult.ClearedRowCount}, " +
                    $"Columns={lineResult.ClearedColumnCount}, " +
                    $"Cells={lineResult.ClearedCellCount}"
                );
            }

            BlockPlacementResult placementResult =
                new BlockPlacementResult(
                    definition.Cells.Count,
                    blockId,
                    lineResult
                );

            BlockPlaced?.Invoke(placementResult);

            return true;
        }


        private LineClearResult ResolveCompletedLines()
        {
            List<int> completedRows = new();
            List<int> completedColumns = new();

            // STEP 1: Scan every row.
            for (int row = 0; row < rows; row++)
            {
                bool complete = true;

                for (int column = 0; column < columns; column++)
                {
                    if (!cells[row, column].IsOccupied)
                    {
                        complete = false;
                        break;
                    }
                }

                if (complete)
                    completedRows.Add(row);
            }

            // STEP 2: Scan every column before clearing.
            for (int column = 0; column < columns; column++)
            {
                bool complete = true;

                for (int row = 0; row < rows; row++)
                {
                    if (!cells[row, column].IsOccupied)
                    {
                        complete = false;
                        break;
                    }
                }

                if (complete)
                    completedColumns.Add(column);
            }

            // STEP 3: Build a unique collection.
            HashSet<Vector2Int> cellsToClear = new();

            foreach (int row in completedRows)
            {
                for (int column = 0; column < columns; column++)
                {
                    cellsToClear.Add(new Vector2Int(row, column));
                }
            }

            foreach (int column in completedColumns)
            {
                for (int row = 0; row < rows; row++)
                {
                    cellsToClear.Add(new Vector2Int(row, column));
                }
            }

            // STEP 4: Clear every selected cell once.
            List<Vector2Int> cleared = new(cellsToClear);

            cleared.Sort((a, b) =>
            {
                int rowComparison = a.x.CompareTo(b.x);

                return rowComparison != 0
                    ? rowComparison
                    : a.y.CompareTo(b.y);
            });

            foreach (Vector2Int position in cleared)
            {
                ClearCell(position.x, position.y);
            }

            LineClearResult result = new LineClearResult(
                completedRows.Count,
                completedColumns.Count,
                cleared
            );

            // STEP 5: Notify future score and FX systems.
            if (result.HasClearedLines)
                LinesCleared?.Invoke(result);

            return result;
        }

        public void ShowPlacementPreview(
            BlockDefinition definition,
            int bottomRow,
            int leftColumn)
        {
            ClearPreview();

            if (definition == null || !definition.IsValid())
                return;

            bool valid = CanPlaceBlock(
                definition, bottomRow, leftColumn
            );

            foreach (Vector2Int offset in definition.Cells)
            {
                int row = bottomRow - offset.y;
                int column = leftColumn + offset.x;

                if (!IsInsideGrid(row, column))
                    continue;

                cellViews[row, column].ShowPreview(valid);

                previewCells.Add(
                    new Vector2Int(row, column)
                );
            }
        }

        public void ClearPreview()
        {
            foreach (Vector2Int position in previewCells)
            {
                if (IsInsideGrid(position.x, position.y))
                {
                    cellViews[position.x, position.y].Refresh();
                }
            }

            previewCells.Clear();
        }

        // Editor-only manual test. Not used in normal gameplay.
        [ContextMenu("DEBUG/Test Row And Column Clear")]
        private void DebugTestRowAndColumnClear()
        {
            if (!Application.isPlaying || cells == null)
            {
                Debug.LogWarning(
                    "[GridManager] Run this test in Play mode."
                );
                return;
            }

            // Start with an empty board.
            ClearPreview();

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    ClearCell(row, column);
                }
            }

            // Fill row 3 and column 4 for an 8x8 grid.
            int testRow = Mathf.Min(3, rows - 1);
            int testColumn = Mathf.Min(4, columns - 1);

            for (int column = 0; column < columns; column++)
                TryOccupyCell(testRow, column, 9001);

            for (int row = 0; row < rows; row++)
                TryOccupyCell(row, testColumn, 9001);

            LineClearResult result = ResolveCompletedLines();

            Debug.Log(
                "[GridManager] DEBUG Result: " +
                $"Rows={result.ClearedRowCount}, " +
                $"Columns={result.ClearedColumnCount}, " +
                $"Cells={result.ClearedCellCount}"
            );
        }
    }
}

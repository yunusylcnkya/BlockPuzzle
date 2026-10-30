
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

        [Header("References")]
        [SerializeField] private RectTransform gridRoot;
        [SerializeField] private GridCellView cellPrefab;

        private GridCell[,] cells;
        private GridCellView[,] cellViews;

        private GridLayoutGroup gridLayout;

        private readonly List<Vector2Int> previewCells = new();

        public int Rows => rows;
        public int Columns => columns;

        private void Awake()
        {
            if (gridRoot != null)
                gridLayout = gridRoot.GetComponent<GridLayoutGroup>();
        }

        private void Start()
        {
            GenerateGrid();
        }

        public void GenerateGrid()
        {
            if (gridRoot == null || cellPrefab == null || gridLayout == null)
            {
                Debug.LogError("[GridManager] Missing references.");
                return;
            }

            if (cells != null)
                return;

            cells = new GridCell[rows, columns];
            cellViews = new GridCellView[rows, columns];

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    GridCell cell = new GridCell(row, column);
                    GridCellView view = Instantiate(cellPrefab, gridRoot);

                    view.name = $"Cell_{row}_{column}";
                    view.Initialize(cell);

                    cells[row, column] = cell;
                    cellViews[row, column] = view;
                }
            }

            Debug.Log($"[GridManager] Generated {rows}x{columns} grid.");
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

        public bool TryOccupyCell(int row, int column, int blockId)
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

        public bool TryGetCellFromScreenPosition(
            Vector2 screenPosition,
            Camera uiCamera,
            out int row,
            out int column)
        {
            row = -1;
            column = -1;

            if (gridRoot == null || gridLayout == null)
                return false;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gridRoot, screenPosition, uiCamera, out Vector2 local))
                return false;

            Rect rect = gridRoot.rect;

            float x = local.x - rect.xMin;
            float y = rect.yMax - local.y;

            float stepX = gridLayout.cellSize.x + gridLayout.spacing.x;
            float stepY = gridLayout.cellSize.y + gridLayout.spacing.y;

            if (stepX <= 0 || stepY <= 0)
                return false;

            float contentWidth =
                columns * gridLayout.cellSize.x +
                (columns - 1) * gridLayout.spacing.x;

            float contentHeight =
                rows * gridLayout.cellSize.y +
                (rows - 1) * gridLayout.spacing.y;

            float availableWidth = rect.width -
                gridLayout.padding.left - gridLayout.padding.right;

            float availableHeight = rect.height -
                gridLayout.padding.top - gridLayout.padding.bottom;

            float startX = gridLayout.padding.left +
                (availableWidth - contentWidth) * 0.5f;

            float startY = gridLayout.padding.top +
                (availableHeight - contentHeight) * 0.5f;

            float gridX = x - startX;
            float gridY = y - startY;

            if (gridX < 0 || gridY < 0 ||
                gridX >= contentWidth || gridY >= contentHeight)
                return false;

            column = Mathf.FloorToInt(gridX / stepX);
            row = Mathf.FloorToInt(gridY / stepY);

            if (!IsInsideGrid(row, column))
                return false;

            float withinX = gridX - column * stepX;
            float withinY = gridY - row * stepY;

            return withinX < gridLayout.cellSize.x &&
                   withinY < gridLayout.cellSize.y;
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
            if (!CanPlaceBlock(definition, bottomRow, leftColumn))
                return false;

            ClearPreview();

            foreach (Vector2Int offset in definition.Cells)
            {
                int row = bottomRow - offset.y;
                int column = leftColumn + offset.x;

                TryOccupyCell(row, column, blockId);
            }

            return true;
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
                definition, bottomRow, leftColumn);

            foreach (Vector2Int offset in definition.Cells)
            {
                int row = bottomRow - offset.y;
                int column = leftColumn + offset.x;

                if (!IsInsideGrid(row, column))
                    continue;

                cellViews[row, column].ShowPreview(valid);
                previewCells.Add(new Vector2Int(row, column));
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
    }
}

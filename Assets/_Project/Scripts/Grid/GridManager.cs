
using UnityEngine;
using UnityEngine.UI;

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

        public int Rows => rows;
        public int Columns => columns;

        private void Start()
        {
            GenerateGrid();

        }

        public void GenerateGrid()
        {
            if (gridRoot == null || cellPrefab == null)
            {
                Debug.LogError("[GridManager] Missing references.");
                return;
            }

            if (cells != null)
            {
                Debug.LogWarning("[GridManager] Grid already generated.");
                return;
            }

            cells = new GridCell[rows, columns];
            cellViews = new GridCellView[rows, columns];

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    GridCell cell = new GridCell(row, column);

                    GridCellView view = Instantiate(
                        cellPrefab,
                        gridRoot
                    );

                    view.name = $"Cell_{row}_{column}";
                    view.Initialize(cell);

                    cells[row, column] = cell;
                    cellViews[row, column] = view;
                }
            }

            Debug.Log(
                $"[GridManager] Generated {rows}x{columns} grid."
            );
        }

        public bool IsInsideGrid(int row, int column)
        {
            return row >= 0 &&
                   row < rows &&
                   column >= 0 &&
                   column < columns;
        }

        public GridCell GetCell(int row, int column)
        {
            if (cells == null ||
                !IsInsideGrid(row, column))
            {
                return null;
            }

            return cells[row, column];
        }

        public bool TryOccupyCell(
            int row,
            int column,
            int blockId)
        {
            GridCell cell = GetCell(row, column);

            if (cell == null)
                return false;

            bool success = cell.TryOccupy(blockId);

            if (success)
            {
                cellViews[row, column].Refresh();
            }

            return success;
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
    }
}

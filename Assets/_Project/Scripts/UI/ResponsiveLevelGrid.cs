
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class ResponsiveLevelGrid : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform viewport;

        [Header("Responsive Settings")]
        [SerializeField, Range(1, 6)]
        private int maximumColumns = 4;

        [SerializeField, Min(80f)]
        private float minimumCellWidth = 190f;

        [SerializeField, Range(0.7f, 1.5f)]
        private float cellHeightRatio = 1.05f;

        private GridLayoutGroup grid;
        private float lastViewportWidth = -1f;

        private void Awake()
        {
            grid = GetComponent<GridLayoutGroup>();
        }

        private void OnEnable()
        {
            lastViewportWidth = -1f;
        }

        private void LateUpdate()
        {
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            if (viewport == null || grid == null)
                return;

            float width = viewport.rect.width;

            if (width <= 0f)
                return;

            if (Mathf.Abs(width - lastViewportWidth) < 0.5f)
                return;

            float availableWidth =
                width -
                grid.padding.left -
                grid.padding.right;

            float spacing = grid.spacing.x;

            if (availableWidth <= 0f)
                return;

            int columns = Mathf.FloorToInt(
                (availableWidth + spacing) /
                (minimumCellWidth + spacing)
            );

            columns = Mathf.Clamp(
                columns,
                1,
                maximumColumns
            );

            float cellWidth =
                (availableWidth -
                 spacing * (columns - 1)) /
                columns;

            cellWidth = Mathf.Max(1f, cellWidth);

            float cellHeight =
                cellWidth * cellHeightRatio;

            grid.constraint =
                GridLayoutGroup.Constraint.FixedColumnCount;

            grid.constraintCount = columns;

            grid.cellSize = new Vector2(
                cellWidth,
                cellHeight
            );

            lastViewportWidth = width;

            LayoutRebuilder.MarkLayoutForRebuild(
                (RectTransform)transform
            );

            Debug.Log(
                $"[Adventure UI] Grid updated: " +
                $"{columns} columns, " +
                $"Cell: {cellWidth:F1} x {cellHeight:F1}"
            );
        }
    }
}

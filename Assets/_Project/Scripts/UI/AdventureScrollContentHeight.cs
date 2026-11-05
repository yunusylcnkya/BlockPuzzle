
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UI
{
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class AdventureScrollContentHeight : MonoBehaviour
    {
        [SerializeField] private RectTransform viewport;

        private RectTransform content;
        private GridLayoutGroup grid;

        private int lastChildCount = -1;
        private int lastColumns = -1;
        private float lastCellHeight = -1f;
        private float lastSpacing = -1f;
        private float lastViewportHeight = -1f;

        private void Awake()
        {
            content = GetComponent<RectTransform>();
            grid = GetComponent<GridLayoutGroup>();
        }

        private void OnEnable()
        {
            lastChildCount = -1;
        }

        private void LateUpdate()
        {
            if (viewport == null)
                return;

            int count = 0;

            foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf)
                    count++;
            }

            int columns = Mathf.Max(
                1,
                grid.constraintCount
            );

            float cellHeight = grid.cellSize.y;
            float spacing = grid.spacing.y;
            float viewportHeight = viewport.rect.height;

            if (count == lastChildCount &&
                columns == lastColumns &&
                Mathf.Approximately(cellHeight, lastCellHeight) &&
                Mathf.Approximately(spacing, lastSpacing) &&
                Mathf.Approximately(viewportHeight, lastViewportHeight))
            {
                return;
            }

            int rows = Mathf.CeilToInt(
                count / (float)columns
            );

            float height =
                grid.padding.top +
                grid.padding.bottom +
                rows * cellHeight +
                Mathf.Max(0, rows - 1) * spacing;

            height = Mathf.Max(
                height,
                viewportHeight
            );

            content.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                height
            );

            lastChildCount = count;
            lastColumns = columns;
            lastCellHeight = cellHeight;
            lastSpacing = spacing;
            lastViewportHeight = viewportHeight;

            Debug.Log(
                $"[AdventureScroll] {count} levels, " +
                $"{rows} rows, Content height: {height:F0}"
            );
        }
    }
}

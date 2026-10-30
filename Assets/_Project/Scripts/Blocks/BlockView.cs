
using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzle.Blocks
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class BlockView : MonoBehaviour
    {
        [SerializeField] private BlockCellView cellPrefab;
        [SerializeField] private float cellSize = 62f;
        [SerializeField] private float spacing = 4f;

        private readonly List<BlockCellView> spawnedCells = new();

        private RectTransform rectTransform;
        private Vector2 originalPosition;

        public BlockDefinition Definition { get; private set; }
        public RectTransform RectTransform => rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void Initialize(BlockDefinition definition)
        {
            if (definition == null || !definition.IsValid() ||
                cellPrefab == null)
            {
                Debug.LogError("[BlockView] Invalid setup.");
                return;
            }

            ClearVisuals();
            Definition = definition;

            float step = cellSize + spacing;
            float width = definition.Width * step - spacing;
            float height = definition.Height * step - spacing;

            foreach (Vector2Int coordinate in definition.Cells)
            {
                BlockCellView cell = Instantiate(cellPrefab, transform);

                RectTransform cellRect =
                    cell.GetComponent<RectTransform>();

                cellRect.anchorMin = Vector2.one * 0.5f;
                cellRect.anchorMax = Vector2.one * 0.5f;
                cellRect.pivot = Vector2.one * 0.5f;
                cellRect.sizeDelta = new Vector2(cellSize, cellSize);

                float x = coordinate.x * step -
                    width * 0.5f + cellSize * 0.5f;

                float y = coordinate.y * step -
                    height * 0.5f + cellSize * 0.5f;

                cellRect.anchoredPosition = new Vector2(x, y);
                cell.SetColor(definition.BlockColor);

                spawnedCells.Add(cell);
            }

            originalPosition = rectTransform.anchoredPosition;
        }

        public void SetPosition(Vector2 position)
        {
            rectTransform.anchoredPosition = position;
        }

        public void ResetPosition()
        {
            rectTransform.anchoredPosition = originalPosition;
        }

        private void ClearVisuals()
        {
            foreach (BlockCellView cell in spawnedCells)
            {
                if (cell != null)
                {
                    cell.gameObject.SetActive(false);
                    Destroy(cell.gameObject);
                }
            }

            spawnedCells.Clear();
        }
    }
}

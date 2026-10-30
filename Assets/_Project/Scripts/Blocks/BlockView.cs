
using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzle.Blocks
{
    public sealed class BlockView : MonoBehaviour
    {
        [SerializeField] private BlockCellView cellPrefab;
        [SerializeField] private float cellSize = 62f;
        [SerializeField] private float spacing = 4f;

        private readonly List<BlockCellView> spawnedCells = new();

        public BlockDefinition Definition { get; private set; }

        public void Initialize(BlockDefinition definition)
        {
            if (definition == null || !definition.IsValid())
            {
                Debug.LogError("[BlockView] Invalid block definition.");
                return;
            }

            if (cellPrefab == null)
            {
                Debug.LogError("[BlockView] Cell prefab missing.");
                return;
            }

            ClearVisuals();

            Definition = definition;

            float step = cellSize + spacing;

            float width = definition.Width * step - spacing;
            float height = definition.Height * step - spacing;

            foreach (Vector2Int coordinate in definition.Cells)
            {
                BlockCellView cell = Instantiate(
                    cellPrefab,
                    transform
                );

                RectTransform rect =
                    cell.GetComponent<RectTransform>();

                rect.anchorMin = Vector2.one * 0.5f;
                rect.anchorMax = Vector2.one * 0.5f;
                rect.pivot = Vector2.one * 0.5f;

                rect.sizeDelta = new Vector2(cellSize, cellSize);

                float x = coordinate.x * step
                    - width * 0.5f + cellSize * 0.5f;

                float y = coordinate.y * step
                    - height * 0.5f + cellSize * 0.5f;

                rect.anchoredPosition = new Vector2(x, y);

                cell.SetColor(definition.BlockColor);

                spawnedCells.Add(cell);
            }
        }

        private void ClearVisuals()
        {
            foreach (BlockCellView cell in spawnedCells)
            {
                if (cell == null)
                    continue;

                cell.gameObject.SetActive(false);
                Destroy(cell.gameObject);
            }

            spawnedCells.Clear();
        }
    }
}

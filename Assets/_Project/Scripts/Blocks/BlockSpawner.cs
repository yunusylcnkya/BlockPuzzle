
using System;
using System.Collections.Generic;
using UnityEngine;
using BlockPuzzle.Grid;

namespace BlockPuzzle.Blocks
{
    public sealed class BlockSpawner : MonoBehaviour
    {
        [Header("Block Library")]
        [SerializeField]
        private List<BlockDefinition> blockLibrary = new();

        [Header("References")]
        [SerializeField] private BlockView blockViewPrefab;

        [SerializeField]
        private RectTransform[] slots =
            new RectTransform[3];

        [SerializeField] private RectTransform dragLayer;
        [SerializeField] private GridManager gridManager;

        private readonly List<BlockView> activeBlocks = new();

        public IReadOnlyList<BlockView> ActiveBlocks => activeBlocks;

        public event Action TrayChanged;

        private void Start()
        {
            SpawnThreeBlocks();
        }

        public void SpawnThreeBlocks()
        {
            if (blockViewPrefab == null ||
                gridManager == null ||
                dragLayer == null ||
                slots == null ||
                slots.Length != 3 ||
                blockLibrary == null)
            {
                Debug.LogError(
                    "[BlockSpawner] Missing references."
                );
                return;
            }

            foreach (RectTransform slot in slots)
            {
                if (slot == null)
                {
                    Debug.LogError(
                        "[BlockSpawner] Missing slot."
                    );
                    return;
                }
            }

            List<BlockDefinition> validBlocks = new();

            foreach (BlockDefinition definition in blockLibrary)
            {
                if (definition != null && definition.IsValid())
                    validBlocks.Add(definition);
            }

            if (validBlocks.Count == 0)
            {
                Debug.LogError(
                    "[BlockSpawner] No valid blocks."
                );
                return;
            }

            ClearActiveBlocks();

            for (int i = 0; i < 3; i++)
            {
                BlockDefinition selected =
                    validBlocks[
                        UnityEngine.Random.Range(
                            0, validBlocks.Count
                        )
                    ];

                BlockView block = Instantiate(
                    blockViewPrefab,
                    slots[i]
                );

                RectTransform rect =
                    block.GetComponent<RectTransform>();

                rect.anchorMin = Vector2.one * 0.5f;
                rect.anchorMax = Vector2.one * 0.5f;
                rect.pivot = Vector2.one * 0.5f;
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;

                block.Initialize(selected);

                BlockDragHandler drag =
                    block.GetComponent<BlockDragHandler>();

                if (drag != null)
                {
                    drag.Initialize(
                        gridManager,
                        this,
                        dragLayer
                    );
                }
                else
                {
                    Debug.LogError(
                        "[BlockSpawner] BlockDragHandler missing."
                    );
                }

                activeBlocks.Add(block);
            }

            Debug.Log(
                "[BlockSpawner] Three blocks spawned."
            );

            TrayChanged?.Invoke();
        }

        public void NotifyBlockUsed(BlockView block)
        {
            if (block == null)
                return;

            if (!activeBlocks.Remove(block))
                return;

            if (activeBlocks.Count == 0)
            {
                // Notify only after the next group exists.
                SpawnThreeBlocks();
            }
            else
            {
                TrayChanged?.Invoke();
            }
        }

        private void ClearActiveBlocks()
        {
            foreach (BlockView block in activeBlocks)
            {
                if (block != null)
                {
                    block.gameObject.SetActive(false);
                    Destroy(block.gameObject);
                }
            }

            activeBlocks.Clear();
        }
    }
}

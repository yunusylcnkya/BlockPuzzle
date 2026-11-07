using System;
using System.Collections.Generic;
using UnityEngine;
using BlockPuzzle.Grid;

namespace BlockPuzzle.Blocks
{
    public sealed class BlockSpawner : MonoBehaviour
    {
        [Header("Block Library")]
        [SerializeField] private List<BlockDefinition> blockLibrary = new();

        [Header("References")]
        [SerializeField] private BlockView blockViewPrefab;
        [SerializeField] private RectTransform[] slots = new RectTransform[3];
        [SerializeField] private RectTransform dragLayer;
        [SerializeField] private GridManager gridManager;

        private readonly List<BlockView> activeBlocks = new();
        public IReadOnlyList<BlockView> ActiveBlocks => activeBlocks;
        public event Action TrayChanged;
        public event Action RerollCompleted;

        private void Start()
        {
            SpawnThreeBlocks();
        }

        public void SpawnThreeBlocks()
        {
            if (blockViewPrefab == null || gridManager == null || dragLayer == null ||
                slots == null || slots.Length != 3 || blockLibrary == null)
            {
                Debug.LogError("[BlockSpawner] Missing references.");
                return;
            }

            foreach (RectTransform slot in slots)
            {
                if (slot == null)
                {
                    Debug.LogError("[BlockSpawner] Missing slot.");
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
                Debug.LogError("[BlockSpawner] No valid blocks.");
                return;
            }

            ClearActiveBlocks();

            for (int i = 0; i < 3; i++)
            {
                BlockDefinition selected = validBlocks[
                    UnityEngine.Random.Range(0, validBlocks.Count)];
                BlockView block = Instantiate(blockViewPrefab, slots[i]);
                RectTransform rect = block.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.one * 0.5f;
                rect.anchorMax = Vector2.one * 0.5f;
                rect.pivot = Vector2.one * 0.5f;
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
                block.Initialize(selected);

                BlockDragHandler drag = block.GetComponent<BlockDragHandler>();
                if (drag != null)
                    drag.Initialize(gridManager, this, dragLayer);
                else
                    Debug.LogError("[BlockSpawner] BlockDragHandler missing.");

                activeBlocks.Add(block);
            }

            Debug.Log("[BlockSpawner] Three blocks spawned.");
            TrayChanged?.Invoke();
        }

        public void NotifyBlockUsed(BlockView block)
        {
            if (block == null || !activeBlocks.Remove(block))
                return;

            if (activeBlocks.Count == 0)
                SpawnThreeBlocks();
            else
                TrayChanged?.Invoke();
        }

        /// <summary>
        /// Determines whether the remaining tray blocks can be exchanged.
        /// Only offers replacements that fit somewhere on the current board.
        /// A block being dragged is never replaced.
        /// </summary>
        public bool CanRerollRemainingBlocks()
        {
            if (gridManager == null || gridManager.GetCell(0, 0) == null ||
                activeBlocks.Count == 0 || slots == null || slots.Length != 3)
                return false;

            foreach (BlockView block in activeBlocks)
            {
                if (block == null || block.Definition == null ||
                    !block.gameObject.activeInHierarchy || !IsInTraySlot(block))
                    return false;
            }

            List<BlockDefinition> choices = GetPlaceableDefinitions();
            if (choices.Count == 0)
                return false;

            // Do not charge the player for a reroll that cannot change anything.
            if (choices.Count == 1)
            {
                foreach (BlockView block in activeBlocks)
                {
                    if (block.Definition != choices[0])
                        return true;
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// Selects valid replacement shapes, then calls payment once.
        /// No blocks are altered when the validation or payment fails.
        /// A successful reroll does not consume a gameplay move.
        /// </summary>
        public bool TryRerollRemainingBlocks(Func<bool> tryCommitPayment)
        {
            if (tryCommitPayment == null || !CanRerollRemainingBlocks())
                return false;

            List<BlockDefinition> choices = GetPlaceableDefinitions();
            List<BlockDefinition> replacements = new(activeBlocks.Count);

            foreach (BlockView block in activeBlocks)
            {
                // Prefer a different shape from the one being replaced.
                List<BlockDefinition> different = new();
                foreach (BlockDefinition choice in choices)
                {
                    if (choice != block.Definition)
                        different.Add(choice);
                }

                List<BlockDefinition> pool = different.Count > 0 ? different : choices;
                replacements.Add(pool[UnityEngine.Random.Range(0, pool.Count)]);
            }

            // Validate first, spend second, apply third. No automatic refunds
            // are needed for ordinary validation failures.
            if (!tryCommitPayment())
                return false;

            for (int i = 0; i < activeBlocks.Count; i++)
                activeBlocks[i].Initialize(replacements[i]);

            Debug.Log($"[BlockSpawner] Replaced {replacements.Count} remaining tray blocks.");
            // Paid reroll invalidates a previous move's Undo snapshot.
            RerollCompleted?.Invoke();
            TrayChanged?.Invoke();
            return true;
        }

        // Used by the Adventure Undo controller. A dragged block is temporarily
        // parented to DragLayer, so use BlockDragHandler.SourceTraySlot to find
        // which of the three slots it originally occupied.
        public BlockDefinition[] CaptureTraySnapshot()
        {
            if (!HasValidSpawnReferences()) return null;
            BlockDefinition[] snapshot = new BlockDefinition[slots.Length];

            foreach (BlockView block in activeBlocks)
            {
                if (block == null || block.Definition == null) return null;
                BlockDragHandler drag = block.GetComponent<BlockDragHandler>();
                Transform originalSlot = drag != null ? drag.SourceTraySlot : block.transform.parent;
                int slotIndex = Array.FindIndex(slots, slot => slot == originalSlot);
                if (slotIndex < 0 || snapshot[slotIndex] != null) return null;
                snapshot[slotIndex] = block.Definition;
            }
            return snapshot;
        }

        public bool IsAnyBlockDragging
        {
            get
            {
                foreach (BlockView block in activeBlocks)
                {
                    if (block == null) continue;
                    BlockDragHandler drag = block.GetComponent<BlockDragHandler>();
                    if (drag != null && drag.IsDragging) return true;
                }
                return false;
            }
        }

        private bool HasValidSpawnReferences()
        {
            if (slots == null || slots.Length != 3 ||
                blockViewPrefab == null || gridManager == null || dragLayer == null)
                return false;
            foreach (RectTransform slot in slots)
                if (slot == null) return false;
            return true;
        }

        public bool CanRestoreTraySnapshot(BlockDefinition[] snapshot)
        {
            if (!HasValidSpawnReferences() || snapshot == null ||
                snapshot.Length != slots.Length || IsAnyBlockDragging)
                return false;
            foreach (BlockDefinition definition in snapshot)
                if (definition != null && !definition.IsValid()) return false;
            return true;
        }

        public bool RestoreTraySnapshot(BlockDefinition[] snapshot)
        {
            if (!CanRestoreTraySnapshot(snapshot)) return false;

            ClearActiveBlocks();
            for (int i = 0; i < slots.Length; i++)
            {
                BlockDefinition definition = snapshot[i];
                if (definition == null) continue;

                BlockView block = Instantiate(blockViewPrefab, slots[i]);
                RectTransform rect = block.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.one * 0.5f;
                rect.anchorMax = Vector2.one * 0.5f;
                rect.pivot = Vector2.one * 0.5f;
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
                block.Initialize(definition);

                BlockDragHandler drag = block.GetComponent<BlockDragHandler>();
                if (drag != null)
                    drag.Initialize(gridManager, this, dragLayer);
                else
                    Debug.LogError("[BlockSpawner] BlockDragHandler missing after Undo.");

                activeBlocks.Add(block);
            }
            TrayChanged?.Invoke();
            return true;
        }

        private bool IsInTraySlot(BlockView block)
        {
            Transform parent = block.transform.parent;
            foreach (RectTransform slot in slots)
            {
                if (slot != null && parent == slot)
                    return true;
            }
            return false;
        }

        private List<BlockDefinition> GetPlaceableDefinitions()
        {
            List<BlockDefinition> choices = new();
            if (blockLibrary == null || gridManager == null)
                return choices;

            foreach (BlockDefinition definition in blockLibrary)
            {
                if (definition == null || !definition.IsValid() ||
                    choices.Contains(definition))
                    continue;

                if (gridManager.HasAnyValidPlacement(definition))
                    choices.Add(definition);
            }
            return choices;
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

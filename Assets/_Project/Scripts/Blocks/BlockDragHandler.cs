using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BlockPuzzle.Grid;

namespace BlockPuzzle.Blocks
{
    [RequireComponent(typeof(BlockView))]
    [RequireComponent(typeof(Image))]
    public sealed class BlockDragHandler : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("Drag Settings")]
        [SerializeField, Range(0f, 0.2f)]
        private float fingerOffsetRatio = 0.075f;
        [SerializeField, Range(0.2f, 1f)]
        private float dragAlpha = 0.70f;

        private static int nextBlockId = 1;

        // Optional hooks. No Undo listener exists in ClassicGame, so Classic
        // keeps its previous behavior without any new Inspector references.
        public static event Action<BlockDragHandler> BeforeValidPlacement;
        public static event Action<bool> AfterPlacement;

        public Transform SourceTraySlot => originalParent != null
            ? originalParent : transform.parent;
        public bool IsDragging => isDragging;

        private BlockView blockView;
        private GridManager gridManager;
        private BlockSpawner blockSpawner;
        private RectTransform rect;
        private RectTransform dragLayer;
        private Transform originalParent;
        private int originalSiblingIndex;
        private CanvasGroup canvasGroup;
        private bool isDragging;
        private bool hasTarget;
        private int targetRow;
        private int targetColumn;

        private void Awake()
        {
            blockView = GetComponent<BlockView>();
            rect = GetComponent<RectTransform>();
            Image image = GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Initialize(
            GridManager grid, BlockSpawner spawner, RectTransform layer)
        {
            gridManager = grid;
            blockSpawner = spawner;
            dragLayer = layer;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (gridManager == null || blockSpawner == null ||
                dragLayer == null || blockView.Definition == null)
            {
                Debug.LogWarning("[BlockDrag] Missing references.");
                return;
            }

            isDragging = true;
            hasTarget = false;
            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();
            transform.SetParent(dragLayer, false);
            transform.SetAsLastSibling();

            float scale = gridManager.CellStep /
                Mathf.Max(1f, blockView.CellStep);
            rect.localScale = Vector3.one * scale;
            canvasGroup.alpha = dragAlpha;
            canvasGroup.blocksRaycasts = false;
            UpdateDragPosition(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            UpdateDragPosition(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            UpdateDragPosition(eventData);
            isDragging = false;
            gridManager.ClearPreview();

            bool placed = false;
            bool attemptWasValid = hasTarget && gridManager.CanPlaceBlock(
                blockView.Definition, targetRow, targetColumn);

            if (attemptWasValid)
            {
                // Snapshot BEFORE TryPlaceBlock mutates the board and scores.
                BeforeValidPlacement?.Invoke(this);
                placed = gridManager.TryPlaceBlock(
                    blockView.Definition, targetRow, targetColumn, nextBlockId);

                if (placed) nextBlockId++;
            }

            if (placed)
            {
                blockSpawner.NotifyBlockUsed(blockView);
                // Commit AFTER the tray auto-refills, if applicable.
                AfterPlacement?.Invoke(true);
                Destroy(gameObject);
            }
            else
            {
                if (attemptWasValid) AfterPlacement?.Invoke(false);

                transform.SetParent(originalParent, false);
                transform.SetSiblingIndex(originalSiblingIndex);
                rect.localScale = Vector3.one;
                blockView.ResetPosition();
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            hasTarget = false;
        }

        private void UpdateDragPosition(PointerEventData eventData)
        {
            Camera uiCamera = eventData.pressEventCamera;
            float offsetPixels = Screen.height * fingerOffsetRatio;
            Vector2 targetScreenPoint = eventData.position + Vector2.up * offsetPixels;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragLayer, targetScreenPoint, uiCamera, out Vector2 fingerLocal))
                rect.anchoredPosition = fingerLocal;

            gridManager.ClearPreview();
            hasTarget = false;

            if (!gridManager.TryGetPlacementFromScreenPosition(
                blockView.Definition, targetScreenPoint, uiCamera,
                out int row, out int column))
                return;

            targetRow = row;
            targetColumn = column;
            hasTarget = true;
            gridManager.ShowPlacementPreview(
                blockView.Definition, targetRow, targetColumn);

            Vector3 worldCenter = gridManager.GetPlacementCenterWorld(
                blockView.Definition, targetRow, targetColumn);
            Vector3 localCenter = dragLayer.InverseTransformPoint(worldCenter);
            rect.anchoredPosition = new Vector2(localCenter.x, localCenter.y);
        }
    }
}

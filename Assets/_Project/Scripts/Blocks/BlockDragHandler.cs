
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
        [SerializeField] private float fingerOffset = 140f;

        private BlockView blockView;
        private GridManager gridManager;
        private BlockSpawner blockSpawner;

        private RectTransform rect;
        private RectTransform dragLayer;
        private Transform originalParent;
        private int originalSiblingIndex;
        private static int nextBlockId = 1;
        private bool isDragging;
        private bool hasTarget;
        private int targetRow;
        private int targetColumn;

        private void Awake()
        {
            blockView = GetComponent<BlockView>();
            rect = GetComponent<RectTransform>();

            Image image = GetComponent<Image>();
            image.color = new Color(1, 1, 1, 0);
            image.raycastTarget = true;
        }

        public void Initialize(
            GridManager grid,
            BlockSpawner spawner,
            RectTransform layer)
        {
            gridManager = grid;
            blockSpawner = spawner;
            dragLayer = layer;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (gridManager == null || dragLayer == null ||
                blockView.Definition == null)
                return;

            isDragging = true;
            originalParent = transform.parent;
            originalSiblingIndex = transform.GetSiblingIndex();

            transform.SetParent(dragLayer, true);
            transform.SetAsLastSibling();

            UpdateDragPosition(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;

            UpdateDragPosition(eventData);
        }


        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;

            isDragging = false;

            gridManager.ClearPreview();

            bool placed = false;

            if (hasTarget)
            {
                placed = gridManager.TryPlaceBlock(
                    blockView.Definition,
                    targetRow,
                    targetColumn,
                    nextBlockId
                );

                if (placed)
                {
                    nextBlockId++;
                }
            }

            if (placed)
            {
                blockSpawner.NotifyBlockUsed(blockView);
                Destroy(gameObject);
            }
            else
            {
                transform.SetParent(originalParent, false);
                transform.SetSiblingIndex(originalSiblingIndex);
                blockView.ResetPosition();
            }

            hasTarget = false;
        }


        private void UpdateDragPosition(PointerEventData eventData)
        {
            Camera camera = eventData.pressEventCamera;

            Vector2 screenPoint = eventData.position +
                Vector2.up * fingerOffset;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragLayer,
                screenPoint,
                camera,
                out Vector2 localPoint))
            {
                rect.anchoredPosition = localPoint;
            }

            gridManager.ClearPreview();
            hasTarget = false;

            if (gridManager.TryGetCellFromScreenPosition(
                screenPoint,
                camera,
                out int row,
                out int column))
            {
                targetRow = row;
                targetColumn = column;
                hasTarget = true;

                gridManager.ShowPlacementPreview(
                    blockView.Definition,
                    targetRow,
                    targetColumn
                );
            }
        }
    }
}

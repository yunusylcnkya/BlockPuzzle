
using UnityEngine;

namespace BlockPuzzle.Grid
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class ResponsiveBoardLayout : MonoBehaviour
    {
        [Header("Responsive Settings")]
        [SerializeField, Range(0.5f, 1f)]
        private float widthRatio = 0.88f;

        [SerializeField, Range(0.2f, 0.8f)]
        private float maxHeightRatio = 0.50f;

        private RectTransform boardRect;
        private RectTransform canvasRect;

        private Vector2 lastCanvasSize;

        private void Awake()
        {
            boardRect = GetComponent<RectTransform>();

            Canvas canvas = GetComponentInParent<Canvas>();

            if (canvas == null)
            {
                Debug.LogError("[BoardLayout] Canvas missing.");
                enabled = false;
                return;
            }

            canvasRect = canvas.rootCanvas
                .GetComponent<RectTransform>();

            ApplyLayout();
        }

        private void LateUpdate()
        {
            if (canvasRect == null)
                return;

            Vector2 size = canvasRect.rect.size;

            if ((size - lastCanvasSize).sqrMagnitude > 0.01f)
                ApplyLayout();
        }

        private void ApplyLayout()
        {
            Vector2 size = canvasRect.rect.size;

            float boardSize = Mathf.Min(
                size.x * widthRatio,
                size.y * maxHeightRatio
            );

            boardRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                boardSize
            );

            boardRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                boardSize
            );

            lastCanvasSize = size;
        }
    }
}


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
        private RectTransform parentRect;

        private Vector2 lastParentSize;

        private void Awake()
        {
            boardRect = GetComponent<RectTransform>();
            parentRect = transform.parent as RectTransform;

            if (parentRect == null)
            {
                Debug.LogError(
                    "[ResponsiveBoardLayout] Parent RectTransform missing."
                );

                enabled = false;
            }
        }

        private void Start()
        {
            ApplyLayout();
        }

        private void LateUpdate()
        {
            if (parentRect == null)
                return;

            Vector2 currentSize = parentRect.rect.size;

            if ((currentSize - lastParentSize).sqrMagnitude > 0.01f)
                ApplyLayout();
        }

        private void ApplyLayout()
        {
            if (parentRect == null)
                return;

            Vector2 size = parentRect.rect.size;

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

            lastParentSize = size;
        }
    }
}


using UnityEngine;

namespace BlockPuzzle.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaController : MonoBehaviour
    {
        private RectTransform safeAreaRect;

        private Rect previousSafeArea;
        private Vector2Int previousScreenSize;

        private void Awake()
        {
            safeAreaRect = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            Vector2Int currentScreenSize = new Vector2Int(
                Screen.width,
                Screen.height
            );

            if (Screen.safeArea != previousSafeArea ||
                currentScreenSize != previousScreenSize)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
                return;

            Rect safeArea = Screen.safeArea;

            Vector2 anchorMin = new Vector2(
                safeArea.xMin / Screen.width,
                safeArea.yMin / Screen.height
            );

            Vector2 anchorMax = new Vector2(
                safeArea.xMax / Screen.width,
                safeArea.yMax / Screen.height
            );

            safeAreaRect.anchorMin = anchorMin;
            safeAreaRect.anchorMax = anchorMax;

            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;

            previousSafeArea = safeArea;

            previousScreenSize = new Vector2Int(
                Screen.width,
                Screen.height
            );
        }
    }
}

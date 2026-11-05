
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Grid;

namespace BlockPuzzle.UI
{
    public sealed class PlacementFXController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private RectTransform fxLayer;
        [SerializeField] private Sprite particleSprite;

        [Header("Particle Settings")]
        [SerializeField, Range(2, 12)]
        private int particlesPerBurst = 6;

        [SerializeField, Range(1, 64)]
        private int maxParticlesPerMove = 24;

        [SerializeField, Min(0.05f)]
        private float duration = 0.40f;

        [SerializeField, Min(0f)]
        private float travelDistance = 85f;

        [SerializeField, Min(1f)]
        private float particleSize = 16f;

        [SerializeField]
        private Color particleColor =
            new Color32(255, 213, 107, 255);
        [SerializeField] private RectTransform boardContainer;
        private Canvas rootCanvas;

        private void Awake()
        {
            if (fxLayer != null)
                rootCanvas = fxLayer.GetComponentInParent<Canvas>();
        }

        private void OnEnable()
        {
            if (gridManager != null)
                gridManager.BlockPlaced += OnBlockPlaced;
        }

        private void OnDisable()
        {
            if (gridManager != null)
                gridManager.BlockPlaced -= OnBlockPlaced;

            StopAllCoroutines();

            if (fxLayer == null)
                return;

            for (int i = fxLayer.childCount - 1; i >= 0; i--)
            {
                Destroy(fxLayer.GetChild(i).gameObject);
            }
        }

        private void OnBlockPlaced(BlockPlacementResult result)
        {
            if (result == null || gridManager == null ||
                fxLayer == null)
                return;

            if (boardContainer == null)
                return;

            Vector3 worldPosition =
                boardContainer.TransformPoint(
                    boardContainer.rect.center
                );

            Vector2 localPosition;

            Camera uiCamera = null;

            if (rootCanvas != null &&
                rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = rootCanvas.worldCamera;
            }

            Vector2 screenPosition =
                RectTransformUtility.WorldToScreenPoint(
                    uiCamera,
                    worldPosition
                );

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                fxLayer,
                screenPosition,
                uiCamera,
                out localPosition))
            {
                return;
            }

            int count = Mathf.Min(
                particlesPerBurst,
                maxParticlesPerMove
            );

            // Stronger feedback for multiple cleared lines.
            if (result.LineClear != null &&
                result.LineClear.HasClearedLines)
            {
                count = Mathf.Min(
                    maxParticlesPerMove,
                    particlesPerBurst *
                    Mathf.Max(1, result.LineClear.TotalLines)
                );
            }

            for (int i = 0; i < count; i++)
            {
                SpawnParticle(localPosition, i, count);
            }
        }

        private void SpawnParticle(
            Vector2 center,
            int index,
            int total)
        {
            GameObject go = new GameObject(
                "FX_Particle",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            RectTransform rect =
                go.GetComponent<RectTransform>();

            rect.SetParent(fxLayer, false);

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            rect.sizeDelta = Vector2.one * particleSize;
            rect.anchoredPosition = center;

            Image image = go.GetComponent<Image>();

            image.sprite = particleSprite;
            image.color = particleColor;
            image.raycastTarget = false;

            float angle =
                index * Mathf.PI * 2f / Mathf.Max(1, total);

            Vector2 direction = new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            );

            StartCoroutine(
                AnimateParticle(rect, image, center, direction)
            );
        }

        private IEnumerator AnimateParticle(
            RectTransform rect,
            Image image,
            Vector2 start,
            Vector2 direction)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (rect == null || image == null)
                    yield break;

                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(elapsed / duration);

                float eased = 1f -
                    Mathf.Pow(1f - t, 2f);

                rect.anchoredPosition =
                    start + direction * travelDistance * eased;

                rect.localScale =
                    Vector3.one * (1f - t * 0.65f);

                Color color = particleColor;
                color.a = 1f - t;

                image.color = color;

                yield return null;
            }

            if (rect != null)
                Destroy(rect.gameObject);
        }
    }
}

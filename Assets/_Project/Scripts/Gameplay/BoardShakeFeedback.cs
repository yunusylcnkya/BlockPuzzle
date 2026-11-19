using System.Collections;
using UnityEngine;
using BlockPuzzle.Grid;

namespace BlockPuzzle.Gameplay
{
    /// <summary>
    /// Optional, low-amplitude shake of the board UI, not the whole camera.
    /// Does not alter grid hit testing, saved board data or gameplay events.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class BoardShakeFeedback : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private bool enableShake = true;
        [SerializeField, Range(0f, 8f)] private float pixelsPerLine = 2.5f;
        [SerializeField, Range(0.04f, 0.25f)] private float duration = 0.13f;

        private RectTransform rect;
        private Vector2 originalPosition;
        private Coroutine running;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            originalPosition = rect.anchoredPosition;
        }

        private void OnEnable()
        {
            if (gridManager != null) gridManager.LinesCleared += OnLinesCleared;
        }

        private void OnDisable()
        {
            if (gridManager != null) gridManager.LinesCleared -= OnLinesCleared;
            if (running != null) StopCoroutine(running);
            running = null;
            if (rect != null) rect.anchoredPosition = originalPosition;
        }

        private void OnLinesCleared(LineClearResult result)
        {
            if (!enableShake || result == null || result.TotalLines < 1) return;
            if (running != null)
            {
                StopCoroutine(running);
                rect.anchoredPosition = originalPosition;
            }
            originalPosition = rect.anchoredPosition;
            running = StartCoroutine(Shake(Mathf.Min(6f,
                pixelsPerLine * result.TotalLines)));
        }

        private IEnumerator Shake(float amplitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float fade = (1f - t) * (1f - t);
                // Deterministic offset avoids allocating per-cell particles.
                Vector2 offset = new Vector2(
                    Mathf.Sin(t * 67f), Mathf.Cos(t * 59f)) * amplitude * fade;
                rect.anchoredPosition = originalPosition + offset;
                yield return null;
            }
            rect.anchoredPosition = originalPosition;
            running = null;
        }
    }
}

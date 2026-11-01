
using System.Collections;
using UnityEngine;

namespace BlockPuzzle.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class GameOverPanelAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform resultCard;

        [Header("Animation")]
        [SerializeField, Min(0.01f)]
        private float duration = 0.40f;

        [SerializeField, Range(0.5f, 1f)]
        private float initialScale = 0.82f;

        private CanvasGroup canvasGroup;
        private Vector3 originalScale;
        private Coroutine openingRoutine;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            if (resultCard != null)
                originalScale = resultCard.localScale;
        }

        private void OnEnable()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (resultCard != null &&
                originalScale == Vector3.zero)
            {
                originalScale = resultCard.localScale;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;

            if (resultCard != null)
            {
                resultCard.localScale =
                    originalScale * initialScale;
            }

            openingRoutine = StartCoroutine(
                AnimateOpen()
            );
        }

        private void OnDisable()
        {
            if (openingRoutine != null)
            {
                StopCoroutine(openingRoutine);
                openingRoutine = null;
            }

            if (resultCard != null)
                resultCard.localScale = originalScale;
        }

        private IEnumerator AnimateOpen()
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / duration
                );

                float eased = 1f -
                    Mathf.Pow(1f - t, 3f);

                canvasGroup.alpha = eased;

                if (resultCard != null)
                {
                    float scale = Mathf.Lerp(
                        initialScale,
                        1f,
                        eased
                    );

                    resultCard.localScale =
                        originalScale * scale;
                }

                yield return null;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;

            if (resultCard != null)
                resultCard.localScale = originalScale;

            openingRoutine = null;
        }
    }
}

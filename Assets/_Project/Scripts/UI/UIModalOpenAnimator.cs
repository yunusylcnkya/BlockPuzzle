using System.Collections;
using UnityEngine;

namespace BlockPuzzle.UI
{
    /// <summary>
    /// Lightweight opening animation for a modal controlled with SetActive.
    /// Do not attach to ResultPanel or GameOverPanel: they already use GameOverPanelAnimator.
    /// Attach this component to the full-screen overlay, not its card.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UIModalOpenAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform card;

        [Header("Opening Animation")]
        [SerializeField, Min(0.01f)] private float duration = 0.22f;
        [SerializeField, Range(0.75f, 1f)] private float initialScale = 0.94f;

        private CanvasGroup canvasGroup;
        private Vector3 baseScale = Vector3.one;
        private Coroutine openingRoutine;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (card != null)
                baseScale = card.localScale;
        }

        private void OnEnable()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (openingRoutine != null)
            {
                StopCoroutine(openingRoutine);
                openingRoutine = null;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            // Block touches on the board while the modal is opening.
            canvasGroup.blocksRaycasts = true;

            if (card != null)
                card.localScale = baseScale * initialScale;

            openingRoutine = StartCoroutine(AnimateOpen());
        }

        private IEnumerator AnimateOpen()
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                canvasGroup.alpha = eased;
                if (card != null)
                    card.localScale = Vector3.LerpUnclamped(baseScale * initialScale, baseScale, eased);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            if (card != null)
                card.localScale = baseScale;
            openingRoutine = null;
        }

        private void OnDisable()
        {
            if (openingRoutine != null)
            {
                StopCoroutine(openingRoutine);
                openingRoutine = null;
            }
            if (card != null)
                card.localScale = baseScale;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UI
{
    /// <summary>
    /// Cosmetic-only star reveal. Attach ONLY to the StarRatingLarge instance
    /// under AdventureGame/ResultPanel/ResultCard; never to the shared prefab.
    /// Does not award stars or change saved progress.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIStarRating))]
    public sealed class ResultStarRevealAnimator : MonoBehaviour
    {
        [Header("References - left to right")]
        [SerializeField] private Image[] stars = new Image[3];

        [Header("Animation")]
        [SerializeField, Min(0.01f)] private float starDuration = 0.18f;
        [SerializeField, Min(0f)] private float delayBetweenStars = 0.10f;
        [SerializeField, Range(1f, 1.5f)] private float overshootScale = 1.13f;

        private UIStarRating rating;
        private Vector3[] originalScales;
        private Coroutine routine;

        private void Awake()
        {
            rating = GetComponent<UIStarRating>();
            originalScales = new Vector3[stars.Length];
            for (int i = 0; i < stars.Length; i++)
                originalScales[i] = stars[i] != null ? stars[i].rectTransform.localScale : Vector3.one;
        }

        private void OnEnable()
        {
            if (routine != null)
                StopCoroutine(routine);
            // Prevent a one-frame full-size flash before the star count is updated.
            if (stars != null && originalScales != null)
            {
                for (int i = 0; i < stars.Length && i < originalScales.Length; i++)
                    if (stars[i] != null)
                        stars[i].rectTransform.localScale = originalScales[i] * 0.7f;
            }
            routine = StartCoroutine(RevealAfterResultUpdate());
        }

        private IEnumerator RevealAfterResultUpdate()
        {
            // AdventureGameController calls SetStars immediately AFTER it activates ResultPanel.
            // One frame later we know the real completion star count.
            yield return null;
            if (rating == null || !isActiveAndEnabled)
            {
                routine = null;
                yield break;
            }

            int filledCount = Mathf.Clamp(rating.CurrentStars, 0, Mathf.Min(3, stars.Length));
            // Unfilled stars remain at their normal scale; only earned stars pop.
            for (int i = filledCount; i < stars.Length && i < originalScales.Length; i++)
                if (stars[i] != null)
                    stars[i].rectTransform.localScale = originalScales[i];
            for (int i = 0; i < filledCount; i++)
            {
                Image star = stars[i];
                if (star == null) continue;
                RectTransform rect = star.rectTransform;
                Vector3 baseline = originalScales[i];
                float elapsed = 0f;
                while (elapsed < starDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / starDuration);
                    // Start at 70%, expand slightly, then settle at 100%.
                    float scale = t < 0.7f
                        ? Mathf.Lerp(0.7f, overshootScale, t / 0.7f)
                        : Mathf.Lerp(overshootScale, 1f, (t - 0.7f) / 0.3f);
                    rect.localScale = baseline * scale;
                    yield return null;
                }
                rect.localScale = baseline;
                if (delayBetweenStars > 0f)
                    yield return new WaitForSecondsRealtime(delayBetweenStars);
            }
            routine = null;
        }

        private void OnDisable()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
            if (stars == null || originalScales == null) return;
            for (int i = 0; i < stars.Length && i < originalScales.Length; i++)
            {
                if (stars[i] != null)
                    stars[i].rectTransform.localScale = originalScales[i];
            }
        }
    }
}

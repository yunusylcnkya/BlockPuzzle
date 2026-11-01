
using System.Collections;
using TMPro;
using UnityEngine;
using BlockPuzzle.Gameplay;

namespace BlockPuzzle.UI
{
    public sealed class ScoreUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text bestScoreText;
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private TMP_Text earnedPointsText;

        [Header("Score Animation")]
        [SerializeField, Min(1f)]
        private float scorePunchScale = 1.15f;

        [SerializeField, Min(0.01f)]
        private float scorePunchDuration = 0.20f;

        [Header("Earned Points Animation")]
        [SerializeField, Min(0.01f)]
        private float earnedDuration = 0.85f;

        [SerializeField]
        private float earnedRise = 50f;

        [Header("Combo Animation")]
        [SerializeField, Min(1f)]
        private float comboPunchScale = 1.20f;

        [SerializeField, Min(0.01f)]
        private float comboPunchDuration = 0.35f;

        private Vector3 scoreBaseScale;
        private Vector3 comboBaseScale;
        private Vector2 earnedBasePosition;
        private Color earnedBaseColor;

        private Coroutine scoreRoutine;
        private Coroutine earnedRoutine;
        private Coroutine comboRoutine;

        private bool initialized;

        private void Awake()
        {
            CacheInitialValues();
        }

        private void CacheInitialValues()
        {
            if (scoreText != null)
                scoreBaseScale =
                    scoreText.rectTransform.localScale;

            if (comboText != null)
                comboBaseScale =
                    comboText.rectTransform.localScale;

            if (earnedPointsText != null)
            {
                earnedBasePosition =
                    earnedPointsText.rectTransform.anchoredPosition;

                earnedBaseColor = earnedPointsText.color;
                earnedBaseColor.a = 1f;
            }

            initialized = true;
        }

        private void OnEnable()
        {
            if (!initialized)
                CacheInitialValues();

            if (scoreManager != null)
                scoreManager.ScoreChanged += Refresh;

            // Initial display should not animate.
            UpdateTexts(false);
        }

        private void OnDisable()
        {
            if (scoreManager != null)
                scoreManager.ScoreChanged -= Refresh;

            StopAnimations();
        }

        public void Refresh()
        {
            UpdateTexts(true);
        }

        private void UpdateTexts(bool animate)
        {
            if (scoreManager == null)
                return;

            if (scoreText != null)
            {
                scoreText.text =
                    scoreManager.CurrentScore.ToString("N0");
            }

            if (bestScoreText != null)
            {
                bestScoreText.text =
                    $"BEST {scoreManager.BestScore:N0}";
            }

            if (comboText != null)
            {
                comboText.text = scoreManager.Combo > 1
                    ? $"COMBO x{scoreManager.CurrentMultiplier:0.##}"
                    : string.Empty;
            }

            if (!animate)
            {
                if (earnedPointsText != null)
                    earnedPointsText.text = string.Empty;

                return;
            }

            if (scoreRoutine != null)
                StopCoroutine(scoreRoutine);

            if (scoreText != null)
                scoreRoutine = StartCoroutine(
                    PunchScale(
                        scoreText.rectTransform,
                        scoreBaseScale,
                        scorePunchScale,
                        scorePunchDuration
                    )
                );

            if (earnedRoutine != null)
                StopCoroutine(earnedRoutine);

            if (earnedPointsText != null &&
                scoreManager.LastEarnedPoints > 0)
            {
                earnedPointsText.text =
                    $"+{scoreManager.LastEarnedPoints:N0}";

                earnedRoutine = StartCoroutine(
                    AnimateEarnedPoints()
                );
            }

            if (comboRoutine != null)
                StopCoroutine(comboRoutine);

            if (comboText != null)
            {
                comboText.rectTransform.localScale =
                    comboBaseScale;

                if (scoreManager.LastClearedLines > 0 &&
                    scoreManager.Combo > 1)
                {
                    comboRoutine = StartCoroutine(
                        PunchScale(
                            comboText.rectTransform,
                            comboBaseScale,
                            comboPunchScale,
                            comboPunchDuration
                        )
                    );
                }
            }
        }

        private IEnumerator PunchScale(
            RectTransform target,
            Vector3 baseScale,
            float maximumScale,
            float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / duration
                );

                float punch = Mathf.Sin(t * Mathf.PI);

                target.localScale = baseScale *
                    (1f + (maximumScale - 1f) * punch);

                yield return null;
            }

            target.localScale = baseScale;
        }

        private IEnumerator AnimateEarnedPoints()
        {
            RectTransform target =
                earnedPointsText.rectTransform;

            target.anchoredPosition = earnedBasePosition;
            earnedPointsText.color = earnedBaseColor;

            float elapsed = 0f;

            while (elapsed < earnedDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / earnedDuration
                );

                float eased = 1f -
                    Mathf.Pow(1f - t, 2f);

                target.anchoredPosition =
                    earnedBasePosition +
                    Vector2.up * earnedRise * eased;

                Color color = earnedBaseColor;
                color.a = 1f - t;

                earnedPointsText.color = color;

                yield return null;
            }

            target.anchoredPosition = earnedBasePosition;
            earnedPointsText.color = earnedBaseColor;
            earnedPointsText.text = string.Empty;
        }

        private void StopAnimations()
        {
            if (scoreRoutine != null)
            {
                StopCoroutine(scoreRoutine);
                scoreRoutine = null;
            }

            if (earnedRoutine != null)
            {
                StopCoroutine(earnedRoutine);
                earnedRoutine = null;
            }

            if (comboRoutine != null)
            {
                StopCoroutine(comboRoutine);
                comboRoutine = null;
            }

            if (scoreText != null)
                scoreText.rectTransform.localScale =
                    scoreBaseScale;

            if (comboText != null)
                comboText.rectTransform.localScale =
                    comboBaseScale;

            if (earnedPointsText != null)
            {
                earnedPointsText.rectTransform.anchoredPosition =
                    earnedBasePosition;

                earnedPointsText.color = earnedBaseColor;
                earnedPointsText.text = string.Empty;
            }
        }
    }
}

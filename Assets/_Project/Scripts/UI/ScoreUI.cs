
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

        private void OnEnable()
        {
            if (scoreManager != null)
                scoreManager.ScoreChanged += Refresh;

            Refresh();
        }

        private void OnDisable()
        {
            if (scoreManager != null)
                scoreManager.ScoreChanged -= Refresh;
        }

        public void Refresh()
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

            if (earnedPointsText != null)
            {
                earnedPointsText.text =
                    scoreManager.LastEarnedPoints > 0
                        ? $"+{scoreManager.LastEarnedPoints:N0}"
                        : string.Empty;
            }
        }
    }
}

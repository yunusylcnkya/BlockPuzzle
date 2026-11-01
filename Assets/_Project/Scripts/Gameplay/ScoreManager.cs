
using System;
using UnityEngine;
using BlockPuzzle.Grid;

namespace BlockPuzzle.Gameplay
{
    public sealed class ScoreManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private ScoreRules scoreRules;

        [Header("Save Settings")]
        [SerializeField]
        private string bestScoreKey = "BlockPuzzle.Classic.BestScore";

        public int CurrentScore { get; private set; }
        public int BestScore { get; private set; }
        public int Combo { get; private set; }
        public int LastEarnedPoints { get; private set; }
        public int LastClearedLines { get; private set; }
        public float CurrentMultiplier { get; private set; } = 1f;

        public event Action ScoreChanged;

        private void Awake()
        {
            BestScore = PlayerPrefs.GetInt(bestScoreKey, 0);
        }

        private void OnEnable()
        {
            if (gridManager != null)
                gridManager.BlockPlaced += HandleBlockPlaced;
        }

        private void OnDisable()
        {
            if (gridManager != null)
                gridManager.BlockPlaced -= HandleBlockPlaced;
        }

        private void Start()
        {
            if (gridManager == null || scoreRules == null)
            {
                Debug.LogError(
                    "[ScoreManager] Missing references."
                );
                enabled = false;
                return;
            }

            ScoreChanged?.Invoke();
        }

        private void HandleBlockPlaced(
            BlockPlacementResult result)
        {
            if (result == null || scoreRules == null)
                return;

            int lines = result.LineClear?.TotalLines ?? 0;

            if (lines > 0)
                Combo++;
            else
                Combo = 0;

            CurrentMultiplier =
                scoreRules.GetComboMultiplier(Combo);

            int placementPoints =
                result.PlacedCellCount *
                scoreRules.pointsPerCell;

            int linePoints =
                lines * scoreRules.pointsPerLine;

            int multiLineBonus =
                scoreRules.CalculateMultipleLineBonus(lines);

            int clearPoints = Mathf.RoundToInt(
                (linePoints + multiLineBonus) *
                CurrentMultiplier
            );

            LastEarnedPoints =
                placementPoints + clearPoints;

            LastClearedLines = lines;

            CurrentScore += LastEarnedPoints;

            if (CurrentScore > BestScore)
            {
                BestScore = CurrentScore;

                PlayerPrefs.SetInt(
                    bestScoreKey,
                    BestScore
                );

                PlayerPrefs.Save();
            }

            Debug.Log(
                $"[ScoreManager] +{LastEarnedPoints} " +
                $"Score={CurrentScore} " +
                $"Lines={lines} " +
                $"Combo={Combo} " +
                $"Multiplier={CurrentMultiplier:0.##}"
            );

            ScoreChanged?.Invoke();
        }

        public void ResetCurrentRun()
        {
            CurrentScore = 0;
            Combo = 0;
            LastEarnedPoints = 0;
            LastClearedLines = 0;
            CurrentMultiplier = 1f;

            ScoreChanged?.Invoke();
        }
    }
}

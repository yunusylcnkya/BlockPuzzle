
using UnityEngine;

namespace BlockPuzzle.Gameplay
{
    [CreateAssetMenu(
        fileName = "ClassicScoreRules",
        menuName = "Block Puzzle/Score Rules"
    )]
    public sealed class ScoreRules : ScriptableObject
    {
        [Header("Base Points")]
        [Min(0)] public int pointsPerCell = 10;
        [Min(0)] public int pointsPerLine = 100;

        [Header("Multiple Lines")]
        [Min(0)] public int multiLineBonusStep = 50;

        [Header("Combo")]
        [Min(0f)] public float comboMultiplierStep = 0.25f;
        [Min(1f)] public float maxComboMultiplier = 3f;

        public int CalculateMultipleLineBonus(int lines)
        {
            if (lines < 2)
                return 0;

            return multiLineBonusStep * lines * (lines - 1) / 2;
        }

        public float GetComboMultiplier(int combo)
        {
            if (combo <= 1)
                return 1f;

            return Mathf.Min(
                maxComboMultiplier,
                1f + (combo - 1) * comboMultiplierStep
            );
        }
    }
}

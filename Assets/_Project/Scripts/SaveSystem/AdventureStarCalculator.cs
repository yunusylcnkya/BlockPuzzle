
using UnityEngine;

namespace BlockPuzzle.SaveSystem
{
    public static class AdventureStarCalculator
    {
        public static int CalculateStars(
            int movesUsed,
            int moveLimit)
        {
            if (moveLimit <= 0 ||
                movesUsed <= 0 ||
                movesUsed > moveLimit)
            {
                return 0;
            }

            float ratio = (float)movesUsed / moveLimit;

            if (ratio <= 0.50f)
                return 3;

            if (ratio <= 0.75f)
                return 2;

            return 1;
        }

        public static int CalculateFirstReward(int stars)
        {
            return Mathf.Clamp(stars, 0, 3) * 25;
        }
    }
}

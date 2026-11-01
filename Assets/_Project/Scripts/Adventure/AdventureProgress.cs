
using UnityEngine;

namespace BlockPuzzle.Adventure
{
    public static class AdventureProgress
    {
        private const string HighestUnlockedKey =
            "BlockPuzzle.Adventure.HighestUnlockedLevel";

        public static int HighestUnlockedLevel =>
            Mathf.Max(
                1,
                PlayerPrefs.GetInt(HighestUnlockedKey, 1)
            );

        public static bool IsUnlocked(int levelId)
        {
            return levelId >= 1 &&
                   levelId <= HighestUnlockedLevel;
        }

        // Reserved for v0.9 when a level is completed.
        public static void UnlockNextLevel(int completedLevelId)
        {
            if (completedLevelId < 1)
                return;

            int nextLevel = completedLevelId + 1;

            if (nextLevel <= HighestUnlockedLevel)
                return;

            PlayerPrefs.SetInt(
                HighestUnlockedKey,
                nextLevel
            );

            PlayerPrefs.Save();
        }
    }
}

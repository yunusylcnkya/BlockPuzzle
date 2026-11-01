
using BlockPuzzle.SaveSystem;

namespace BlockPuzzle.Adventure
{
    public static class AdventureProgress
    {
        public static int HighestUnlockedLevel =>
            AdventureSaveService.HighestUnlockedLevel;

        public static bool IsUnlocked(int levelId)
        {
            return AdventureSaveService.IsUnlocked(levelId);
        }

        // Legacy API retained for compatibility.
        // New level completion must use
        // AdventureSaveService.CompleteLevel instead.
        [System.Obsolete(
            "Use AdventureSaveService.CompleteLevel after success."
        )]
        public static void UnlockNextLevel(int completedLevelId)
        {
            UnityEngine.Debug.LogWarning(
                "[AdventureProgress] Direct unlock is disabled. " +
                "Complete the level through AdventureSaveService."
            );
        }
    }
}

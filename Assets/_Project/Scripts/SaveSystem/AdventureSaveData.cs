
using System;
using System.Collections.Generic;

namespace BlockPuzzle.SaveSystem
{
    [Serializable]
    public sealed class AdventureLevelRecord
    {
        public int levelId;
        public bool completed;
        public int bestStars;
        public int bestScore;
        public int bestMoves;
        public bool rewardClaimed;
    }

    [Serializable]
    public sealed class AdventureSaveData
    {
        public int saveVersion = 1;
        public int highestUnlockedLevel = 1;
        public int totalCoins = 0;

        public List<AdventureLevelRecord> levels =
            new List<AdventureLevelRecord>();
    }

    public readonly struct AdventureCompletionResult
    {
        public readonly int EarnedStars;
        public readonly int BestStars;
        public readonly int CoinsEarned;
        public readonly int TotalCoins;
        public readonly bool FirstCompletion;

        public AdventureCompletionResult(
            int earnedStars,
            int bestStars,
            int coinsEarned,
            int totalCoins,
            bool firstCompletion)
        {
            EarnedStars = earnedStars;
            BestStars = bestStars;
            CoinsEarned = coinsEarned;
            TotalCoins = totalCoins;
            FirstCompletion = firstCompletion;
        }
    }
}

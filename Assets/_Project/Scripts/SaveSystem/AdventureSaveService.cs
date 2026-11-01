
using System;
using UnityEngine;

namespace BlockPuzzle.SaveSystem
{
    public static class AdventureSaveService
    {
        private const string SaveKey =
            "BlockPuzzle.Adventure.SaveData.V1";

        private const string LegacyUnlockKey =
            "BlockPuzzle.Adventure.HighestUnlockedLevel";

        private static AdventureSaveData cachedData;

        public static event Action DataChanged;

        public static int HighestUnlockedLevel
        {
            get
            {
                EnsureLoaded();
                return cachedData.highestUnlockedLevel;
            }
        }

        public static int TotalCoins
        {
            get
            {
                EnsureLoaded();
                return cachedData.totalCoins;
            }
        }

        private static void EnsureLoaded()
        {
            if (cachedData != null)
                return;

            string json = PlayerPrefs.GetString(SaveKey, "");

            if (string.IsNullOrEmpty(json))
            {
                cachedData = new AdventureSaveData();
            }
            else
            {
                try
                {
                    cachedData =
                        JsonUtility.FromJson<AdventureSaveData>(json);
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        "[SaveSystem] Failed to read save: " +
                        exception.Message
                    );

                    cachedData = new AdventureSaveData();
                }
            }

            if (cachedData == null)
                cachedData = new AdventureSaveData();

            if (cachedData.levels == null)
            {
                cachedData.levels =
                    new System.Collections.Generic.List<
                        AdventureLevelRecord>();
            }

            cachedData.highestUnlockedLevel =
                Mathf.Max(1, cachedData.highestUnlockedLevel);

            cachedData.totalCoins =
                Mathf.Max(0, cachedData.totalCoins);

            // Preserve progress from v0.9.
            int legacyHighest = PlayerPrefs.GetInt(
                LegacyUnlockKey,
                1
            );

            if (legacyHighest >
                cachedData.highestUnlockedLevel)
            {
                cachedData.highestUnlockedLevel =
                    legacyHighest;

                Save();
            }
        }

        public static AdventureLevelRecord GetLevelRecord(
            int levelId)
        {
            EnsureLoaded();

            foreach (AdventureLevelRecord record
                     in cachedData.levels)
            {
                if (record != null &&
                    record.levelId == levelId)
                {
                    // Return a copy so UI cannot mutate save state.
                    return CopyRecord(record);
                }
            }

            return new AdventureLevelRecord
            {
                levelId = levelId,
                completed = false,
                bestStars = 0,
                bestScore = 0,
                bestMoves = 0,
                rewardClaimed = false
            };
        }

        public static bool IsUnlocked(int levelId)
        {
            EnsureLoaded();

            return levelId >= 1 &&
                   levelId <= cachedData.highestUnlockedLevel;
        }

        public static AdventureCompletionResult CompleteLevel(
            int levelId,
            int score,
            int movesUsed,
            int moveLimit)
        {
            EnsureLoaded();

            if (levelId < 1 || !IsUnlocked(levelId))
            {
                Debug.LogWarning(
                    "[SaveSystem] Invalid or locked level."
                );

                return new AdventureCompletionResult(
                    0, 0, 0, TotalCoins, false
                );
            }

            int earnedStars =
                AdventureStarCalculator.CalculateStars(
                    movesUsed,
                    moveLimit
                );

            if (earnedStars <= 0)
            {
                Debug.LogWarning(
                    "[SaveSystem] Invalid completion metrics."
                );

                return new AdventureCompletionResult(
                    0, 0, 0, TotalCoins, false
                );
            }

            AdventureLevelRecord record =
                FindOrCreateRecord(levelId);

            bool firstCompletion = !record.completed;

            record.completed = true;
            record.bestStars = Mathf.Max(
                record.bestStars,
                earnedStars
            );

            record.bestScore = Mathf.Max(
                record.bestScore,
                Mathf.Max(0, score)
            );

            if (record.bestMoves <= 0)
            {
                record.bestMoves = movesUsed;
            }
            else
            {
                record.bestMoves = Mathf.Min(
                    record.bestMoves,
                    movesUsed
                );
            }

            int coinsEarned = 0;

            if (!record.rewardClaimed)
            {
                coinsEarned =
                    AdventureStarCalculator.CalculateFirstReward(
                        earnedStars
                    );

                cachedData.totalCoins += coinsEarned;
                record.rewardClaimed = true;
            }

            cachedData.highestUnlockedLevel =
                Mathf.Max(
                    cachedData.highestUnlockedLevel,
                    levelId + 1
                );

            Save();

            return new AdventureCompletionResult(
                earnedStars,
                record.bestStars,
                coinsEarned,
                cachedData.totalCoins,
                firstCompletion
            );
        }

        private static AdventureLevelRecord FindOrCreateRecord(
            int levelId)
        {
            foreach (AdventureLevelRecord record
                     in cachedData.levels)
            {
                if (record != null &&
                    record.levelId == levelId)
                    return record;
            }

            AdventureLevelRecord created =
                new AdventureLevelRecord
                {
                    levelId = levelId
                };

            cachedData.levels.Add(created);

            return created;
        }

        private static AdventureLevelRecord CopyRecord(
            AdventureLevelRecord source)
        {
            return new AdventureLevelRecord
            {
                levelId = source.levelId,
                completed = source.completed,
                bestStars = source.bestStars,
                bestScore = source.bestScore,
                bestMoves = source.bestMoves,
                rewardClaimed = source.rewardClaimed
            };
        }

        private static void Save()
        {
            cachedData.saveVersion = 1;

            string json =
                JsonUtility.ToJson(cachedData);

            PlayerPrefs.SetString(SaveKey, json);

            // Keep the old key synchronized for compatibility.
            PlayerPrefs.SetInt(
                LegacyUnlockKey,
                cachedData.highestUnlockedLevel
            );

            PlayerPrefs.Save();

            DataChanged?.Invoke();

            Debug.Log("[SaveSystem] Progress saved.");
        }

        public static void ReloadFromDisk()
        {
            cachedData = null;
            EnsureLoaded();
            DataChanged?.Invoke();
        }
    }
}

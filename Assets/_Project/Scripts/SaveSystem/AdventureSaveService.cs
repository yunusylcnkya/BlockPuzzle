using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BlockPuzzle.SaveSystem
{
    /// <summary>
    /// Adventure progress and soft-currency wallet.
    /// V1 PlayerPrefs key is unchanged for existing installs. A second copy
    /// is stored under persistentDataPath, independently of PlayerPrefs.
    /// </summary>
    public static class AdventureSaveService
    {
        private const string SaveKey = "BlockPuzzle.Adventure.SaveData.V1";
        private const string LegacyUnlockKey = "BlockPuzzle.Adventure.HighestUnlockedLevel";
        private const string BackupFileName = "BlockPuzzle_Adventure_Save_V1.backup.json";

        private static AdventureSaveData cachedData;
        private static bool recoveryBlocked;

        public static event Action DataChanged;

        public static int HighestUnlockedLevel
        {
            get { EnsureLoaded(); return cachedData.highestUnlockedLevel; }
        }

        public static int TotalCoins
        {
            get { EnsureLoaded(); return cachedData.totalCoins; }
        }

        /// <summary>
        /// True only when existing save data was detected but neither the
        /// original JSON nor its independent file backup was valid.
        /// Purchases and progress writes are then blocked rather than silently
        /// overwriting a potentially recoverable save.
        /// </summary>
        public static bool RecoveryBlocked
        {
            get { EnsureLoaded(); return recoveryBlocked; }
        }

        public static bool TrySpendCoins(int amount)
        {
            EnsureLoaded();
            if (recoveryBlocked)
            {
                Debug.LogError("[SaveSystem] Spending blocked: save recovery required.");
                return false;
            }
            if (amount <= 0)
            {
                Debug.LogWarning("[Wallet] Spend amount must be positive.");
                return false;
            }
            if (cachedData.totalCoins < amount)
            {
                Debug.Log($"[Wallet] Not enough coins. Have {cachedData.totalCoins}, need {amount}.");
                return false;
            }

            cachedData.totalCoins -= amount;
            Save();
            Debug.Log($"[Wallet] Spent {amount} coins; remaining {cachedData.totalCoins}.");
            return true;
        }

        public static void AddRewardedCoins(int amount)
        {
            EnsureLoaded();
            if (recoveryBlocked)
            {
                Debug.LogError("[SaveSystem] Reward not saved: save recovery required.");
                return;
            }
            if (amount <= 0)
            {
                Debug.LogWarning("[SaveSystem] Invalid rewarded coin amount.");
                return;
            }
            if (cachedData.totalCoins > int.MaxValue - amount)
            {
                Debug.LogError("[SaveSystem] Coin balance limit reached.");
                return;
            }

            cachedData.totalCoins += amount;
            Save();
            Debug.Log($"[SaveSystem] Rewarded coins saved. Amount: +{amount}, Total: {cachedData.totalCoins}");
        }

        private static string BackupPath => Path.Combine(
            Application.persistentDataPath, BackupFileName);

        private static bool TryReadSave(string json, out AdventureSaveData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(json))
                return false;

            // JsonUtility may accept objects with missing fields. Such a
            // partially-written object must not be treated as a fresh save.
            if (!json.Contains("\"saveVersion\"") ||
                !json.Contains("\"highestUnlockedLevel\"") ||
                !json.Contains("\"totalCoins\"") ||
                !json.Contains("\"levels\""))
                return false;

            try
            {
                AdventureSaveData parsed =
                    JsonUtility.FromJson<AdventureSaveData>(json);
                if (parsed == null || parsed.saveVersion != 1 ||
                    parsed.highestUnlockedLevel < 1 || parsed.totalCoins < 0 ||
                    parsed.levels == null)
                    return false;

                HashSet<int> ids = new HashSet<int>();
                foreach (AdventureLevelRecord record in parsed.levels)
                {
                    if (record == null || record.levelId < 1 ||
                        !ids.Add(record.levelId) ||
                        record.bestStars < 0 || record.bestStars > 3 ||
                        record.bestScore < 0 || record.bestMoves < 0)
                        return false;
                }

                data = parsed;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SaveSystem] Invalid save JSON: " + exception.Message);
                return false;
            }
        }

        private static bool TryReadFileBackup(out AdventureSaveData backup,
            out string backupJson)
        {
            backup = null;
            backupJson = null;
            string path = BackupPath;

            // The .tmp copy may survive an interrupted replacement.
            string[] candidates = { path, path + ".tmp" };
            foreach (string candidate in candidates)
            {
                try
                {
                    if (!File.Exists(candidate))
                        continue;
                    string json = File.ReadAllText(candidate);
                    if (!TryReadSave(json, out AdventureSaveData parsed))
                        continue;
                    backup = parsed;
                    backupJson = json;
                    return true;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[SaveSystem] Backup read failed: " +
                        exception.Message);
                }
            }
            return false;
        }

        private static void WriteFileBackup(string json)
        {
            try
            {
                string path = BackupPath;
                string temp = path + ".tmp";
                File.WriteAllText(temp, json);

                // A complete temp copy is written before replacing the last
                // backup. If replacement is interrupted, the temp is recoverable.
                if (File.Exists(path))
                    File.Delete(path);
                File.Move(temp, path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SaveSystem] Backup write failed: " +
                    exception.Message);
            }
        }

        private static void EnsureLoaded()
        {
            if (cachedData != null)
                return;

            recoveryBlocked = false;
            bool hasPrimary = PlayerPrefs.HasKey(SaveKey);
            string primaryJson = PlayerPrefs.GetString(SaveKey, "");
            bool primaryValid = TryReadSave(primaryJson, out AdventureSaveData primary);
            bool backupValid = TryReadFileBackup(out AdventureSaveData backup,
                out string backupJson);

            if (primaryValid)
            {
                cachedData = primary;
                if (!backupValid || backupJson != primaryJson)
                    WriteFileBackup(primaryJson);
            }
            else if (backupValid)
            {
                cachedData = backup;
                Debug.LogWarning("[SaveSystem] Primary save invalid or missing. " +
                    "Restoring from independent backup.");
                PlayerPrefs.SetString(SaveKey, backupJson);
                PlayerPrefs.SetInt(LegacyUnlockKey, cachedData.highestUnlockedLevel);
                PlayerPrefs.Save();
            }
            else if (!hasPrimary &&
                !File.Exists(BackupPath) && !File.Exists(BackupPath + ".tmp"))
            {
                // Truly new install (or old installation with only legacy key).
                cachedData = new AdventureSaveData();
            }
            else
            {
                // Do not replace both failed copies with an empty save.
                cachedData = new AdventureSaveData();
                recoveryBlocked = true;
                Debug.LogError("[SaveSystem] CRITICAL: No valid Adventure " +
                    "save or file backup. Writes are blocked to protect data.");
                return;
            }

            int legacyHighest = PlayerPrefs.GetInt(LegacyUnlockKey, 1);
            if (legacyHighest > cachedData.highestUnlockedLevel)
            {
                cachedData.highestUnlockedLevel = legacyHighest;
                Save();
            }
        }

        public static AdventureLevelRecord GetLevelRecord(int levelId)
        {
            EnsureLoaded();
            foreach (AdventureLevelRecord record in cachedData.levels)
            {
                if (record != null && record.levelId == levelId)
                    return CopyRecord(record);
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
            return levelId >= 1 && levelId <= cachedData.highestUnlockedLevel;
        }

        public static AdventureCompletionResult CompleteLevel(
            int levelId, int score, int movesUsed, int moveLimit)
        {
            EnsureLoaded();
            if (recoveryBlocked || levelId < 1 || !IsUnlocked(levelId))
            {
                Debug.LogWarning("[SaveSystem] Completion blocked: invalid save or locked level.");
                return new AdventureCompletionResult(0, 0, 0, TotalCoins, false);
            }

            int earnedStars = AdventureStarCalculator.CalculateStars(movesUsed, moveLimit);
            if (earnedStars <= 0)
            {
                Debug.LogWarning("[SaveSystem] Invalid completion metrics.");
                return new AdventureCompletionResult(0, 0, 0, TotalCoins, false);
            }

            AdventureLevelRecord record = FindOrCreateRecord(levelId);
            bool firstCompletion = !record.completed;
            record.completed = true;
            record.bestStars = Mathf.Max(record.bestStars, earnedStars);
            record.bestScore = Mathf.Max(record.bestScore, Mathf.Max(0, score));
            if (record.bestMoves <= 0)
                record.bestMoves = movesUsed;
            else
                record.bestMoves = Mathf.Min(record.bestMoves, movesUsed);

            int coinsEarned = 0;
            if (!record.rewardClaimed)
            {
                int reward = AdventureStarCalculator.CalculateFirstReward(earnedStars);
                if (cachedData.totalCoins > int.MaxValue - reward)
                {
                    Debug.LogError("[SaveSystem] Coin balance limit; reward remains unclaimed.");
                }
                else
                {
                    coinsEarned = reward;
                    cachedData.totalCoins += reward;
                    record.rewardClaimed = true;
                }
            }

            cachedData.highestUnlockedLevel = Mathf.Max(
                cachedData.highestUnlockedLevel, levelId + 1);
            Save();
            return new AdventureCompletionResult(
                earnedStars, record.bestStars, coinsEarned,
                cachedData.totalCoins, firstCompletion);
        }

        private static AdventureLevelRecord FindOrCreateRecord(int levelId)
        {
            foreach (AdventureLevelRecord record in cachedData.levels)
                if (record != null && record.levelId == levelId)
                    return record;

            AdventureLevelRecord created = new AdventureLevelRecord { levelId = levelId };
            cachedData.levels.Add(created);
            return created;
        }

        private static AdventureLevelRecord CopyRecord(AdventureLevelRecord source)
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
            if (recoveryBlocked)
            {
                Debug.LogError("[SaveSystem] Save blocked: recovery required.");
                return;
            }
            cachedData.saveVersion = 1;
            string json = JsonUtility.ToJson(cachedData);

            // Legacy key and original JSON key remain unchanged.
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.SetInt(LegacyUnlockKey, cachedData.highestUnlockedLevel);
            PlayerPrefs.Save();

            // Independently stored JSON protects against PlayerPrefs file loss
            // or corruption. Backup failures are logged, never hidden.
            WriteFileBackup(json);

            DataChanged?.Invoke();
            Debug.Log("[SaveSystem] Progress saved (V1 + file backup).");
        }

        public static void ReloadFromDisk()
        {
            cachedData = null;
            recoveryBlocked = false;
            EnsureLoaded();
            DataChanged?.Invoke();
        }
    }
}

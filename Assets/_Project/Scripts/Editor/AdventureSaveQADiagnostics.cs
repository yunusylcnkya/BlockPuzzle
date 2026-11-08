#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using BlockPuzzle.SaveSystem;

namespace BlockPuzzle.EditorTools
{
    /// <summary>
    /// Read-only inspection of Editor-side save data.
    /// This file belongs under Scripts/Editor and is excluded from Android builds.
    /// It never writes or deletes PlayerPrefs or backup files.
    /// </summary>
    public static class AdventureSaveQADiagnostics
    {
        private const string PrimaryKey = "BlockPuzzle.Adventure.SaveData.V1";
        private const string BackupFile = "BlockPuzzle_Adventure_Save_V1.backup.json";

        [MenuItem("Tools/Block Puzzle/QA/Inspect Adventure Save (Read Only)")]
        public static void Inspect()
        {
            bool primaryExists = PlayerPrefs.HasKey(PrimaryKey);
            string primary = PlayerPrefs.GetString(PrimaryKey, "");
            string backupPath = Path.Combine(Application.persistentDataPath, BackupFile);
            string backup = "";
            bool backupExists = File.Exists(backupPath);

            if (backupExists)
            {
                try { backup = File.ReadAllText(backupPath); }
                catch (Exception e)
                {
                    Debug.LogWarning("[QA Save] Unable to read backup: " + e.Message);
                }
            }

            PrintSnapshot("PlayerPrefs primary", primaryExists, primary);
            PrintSnapshot("Independent JSON backup", backupExists, backup);
            Debug.Log("[QA Save] The diagnostic is READ ONLY. " +
                      "Editor data is independent of the Android app data.");
        }

        private static void PrintSnapshot(string label, bool exists, string json)
        {
            if (!exists || string.IsNullOrEmpty(json))
            {
                Debug.Log($"[QA Save] {label}: absent or empty.");
                return;
            }

            try
            {
                AdventureSaveData data = JsonUtility.FromJson<AdventureSaveData>(json);
                bool valid = data != null && data.saveVersion == 1 &&
                    data.highestUnlockedLevel >= 1 && data.totalCoins >= 0 &&
                    data.levels != null &&
                    json.Contains("\"saveVersion\"") &&
                    json.Contains("\"highestUnlockedLevel\"") &&
                    json.Contains("\"totalCoins\"") &&
                    json.Contains("\"levels\"");
                if (!valid)
                {
                    Debug.LogWarning($"[QA Save] {label}: INVALID FORMAT.");
                    return;
                }

                Debug.Log($"[QA Save] {label}: valid V1; " +
                          $"unlocked={data.highestUnlockedLevel}, " +
                          $"coins={data.totalCoins}, " +
                          $"records={data.levels.Count}, jsonChars={json.Length}.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[QA Save] {label}: invalid JSON. {e.Message}");
            }
        }
    }
}
#endif

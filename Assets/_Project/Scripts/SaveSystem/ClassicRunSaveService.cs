using System;
using System.IO;
using System.Text;
using UnityEngine;
using BlockPuzzle.ClassicPersistence;

namespace BlockPuzzle.SaveSystem
{
    /// <summary>
    /// Classic run only. Independent from Adventure.SaveData.V1 and Classic BestScore.
    /// PlayerPrefs + rotating on-disk copies, SHA-256 integrity, and a close marker.
    /// Saves after a committed move; never overwrites unrecognized/corrupt saves.
    /// </summary>
    public static class ClassicRunSaveService
    {
        private const string PrimaryKey = "BlockPuzzle.Classic.Run.V2";
        private const string ClosedKey = "BlockPuzzle.Classic.Run.V2.Closed";
        private const string SessionKey = "BlockPuzzle.Classic.Run.V2.Session";
        private const string FileName = "BlockPuzzle_Classic_Run_V2.backup.json";

        [Serializable]
        private sealed class Envelope
        {
            public int envelopeVersion = 1;
            public string payloadJson;
            public string checksum;
        }

        private static bool recoveryBlocked;
        public static bool RecoveryBlocked => recoveryBlocked;
        private static string BackupPath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool HasResumableRun => TryLoad(out _);

        public static bool TryLoad(out ClassicRunSnapshot snapshot)
        {
            snapshot = null;
            recoveryBlocked = false;

            // A completed/discarded run must never reappear from an old backup,
            // even if deleting the backup file failed or the app crashed.
            if (PlayerPrefs.GetInt(ClosedKey, 0) == 1)
                return false;

            string path = BackupPath;
            bool anyPresent = PlayerPrefs.HasKey(PrimaryKey) ||
                              File.Exists(path) || File.Exists(path + ".tmp") ||
                              File.Exists(path + ".prev");
            if (!anyPresent) return false;

            string primary = PlayerPrefs.GetString(PrimaryKey, "");
            string expectedSession = PlayerPrefs.GetString(SessionKey, "");
            ClassicRunSnapshot best = null;
            string bestEnvelope = null;
            string bestSource = null;

            Consider(primary, "PlayerPrefs", expectedSession, ref best, ref bestEnvelope, ref bestSource);
            Consider(ReadFile(path), "backup", expectedSession, ref best, ref bestEnvelope, ref bestSource);
            Consider(ReadFile(path + ".tmp"), "temporary", expectedSession, ref best, ref bestEnvelope, ref bestSource);
            Consider(ReadFile(path + ".prev"), "previous", expectedSession, ref best, ref bestEnvelope, ref bestSource);

            if (best == null)
            {
                recoveryBlocked = true;
                Debug.LogError("[ClassicSave] No valid V2 copy; writes blocked. " +
                               "Start New Game explicitly to discard the damaged session.");
                return false;
            }

            snapshot = best;
            if (primary != bestEnvelope)
            {
                // Repair a missing/stale PlayerPrefs copy from the newest valid copy.
                try
                {
                    PlayerPrefs.SetString(PrimaryKey, bestEnvelope);
                    if (string.IsNullOrEmpty(expectedSession))
                        PlayerPrefs.SetString(SessionKey, best.sessionId);
                    PlayerPrefs.Save();
                    Debug.LogWarning("[ClassicSave] Recovered latest run from " + bestSource);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[ClassicSave] Could not repair PlayerPrefs: " + e.Message);
                }
            }
            return true;
        }

        public static bool TrySave(ClassicRunSnapshot snapshot)
        {
            // Session ID separates an explicit New Game from stale backup files.
            string session = PlayerPrefs.GetString(SessionKey, "");
            if (string.IsNullOrEmpty(session)) session = Guid.NewGuid().ToString("N");
            if (snapshot != null) snapshot.sessionId = session;
            if (!ClassicRunValidator.Validate(snapshot, out string reason))
            {
                Debug.LogError("[ClassicSave] Invalid snapshot: " + reason);
                return false;
            }

            // Check existing copies before writing, and never overwrite corrupt data.
            ClassicRunSnapshot existing;
            bool hasExisting = TryLoad(out existing);
            if (recoveryBlocked) return false;

            snapshot.revision = hasExisting ? existing.revision + 1 : 1;
            if (!ClassicRunValidator.Validate(snapshot, out reason))
            {
                Debug.LogError("[ClassicSave] Invalid revision: " + reason);
                return false;
            }

            string payload = JsonUtility.ToJson(snapshot);
            var envelope = new Envelope
            {
                payloadJson = payload,
                checksum = ClassicSaveIntegrity.Hash(payload)
            };
            string encoded = JsonUtility.ToJson(envelope);

            // First secure a complete independent file, then update PlayerPrefs.
            if (!WriteBackup(encoded)) return false;
            try
            {
                PlayerPrefs.SetString(PrimaryKey, encoded);
                PlayerPrefs.SetString(SessionKey, session);
                PlayerPrefs.SetInt(ClosedKey, 0);
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[ClassicSave] PlayerPrefs write failed: " + e.Message);
                // File backup remains valid and can be recovered on next launch.
                return false;
            }
        }

        /// <summary>
        /// Explicit New Game / Game Over only. Tombstone is stored BEFORE file
        /// deletion so stale backups cannot resurrect a discarded run.
        /// Does NOT touch best score or any Adventure keys.
        /// </summary>
        public static void CloseRun()
        {
            try
            {
                PlayerPrefs.SetInt(ClosedKey, 1);
                PlayerPrefs.SetString(SessionKey, Guid.NewGuid().ToString("N"));
                PlayerPrefs.DeleteKey(PrimaryKey);
                PlayerPrefs.Save();
                recoveryBlocked = false;
            }
            catch (Exception e)
            {
                Debug.LogError("[ClassicSave] Could not close run: " + e.Message);
                return;
            }

            string path = BackupPath;
            RemoveFile(path);
            RemoveFile(path + ".tmp");
            RemoveFile(path + ".prev");
        }

        private static void Consider(string encoded, string source, string expectedSession,
            ref ClassicRunSnapshot best, ref string bestEnvelope, ref string bestSource)
        {
            if (!TryDecode(encoded, out ClassicRunSnapshot candidate)) return;
            if (!string.IsNullOrEmpty(expectedSession) &&
                !string.Equals(candidate.sessionId, expectedSession, StringComparison.Ordinal))
                return;
            if (best != null && best.revision >= candidate.revision) return;
            best = candidate;
            bestEnvelope = encoded;
            bestSource = source;
        }

        private static bool TryDecode(string encoded, out ClassicRunSnapshot snapshot)
        {
            snapshot = null;
            if (string.IsNullOrWhiteSpace(encoded) ||
                !encoded.Contains("\"payloadJson\"") ||
                !encoded.Contains("\"checksum\"")) return false;
            try
            {
                Envelope envelope = JsonUtility.FromJson<Envelope>(encoded);
                if (envelope == null || envelope.envelopeVersion != 1 ||
                    !ClassicSaveIntegrity.Matches(envelope.payloadJson, envelope.checksum))
                    return false;
                if (!envelope.payloadJson.Contains("\"saveVersion\"") ||
                    !envelope.payloadJson.Contains("\"trayBlockIds\"") ||
                    !envelope.payloadJson.Contains("\"occupiedCells\""))
                    return false;
                ClassicRunSnapshot candidate =
                    JsonUtility.FromJson<ClassicRunSnapshot>(envelope.payloadJson);
                if (!ClassicRunValidator.Validate(candidate, out _)) return false;
                snapshot = candidate;
                return true;
            }
            catch (Exception) { return false; }
        }

        private static string ReadFile(string path)
        {
            try { return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null; }
            catch (Exception e)
            {
                Debug.LogWarning("[ClassicSave] Read failed: " + e.Message);
                return null;
            }
        }

        private static bool WriteBackup(string encoded)
        {
            string path = BackupPath;
            string temp = path + ".tmp";
            string previous = path + ".prev";
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(temp, encoded, new UTF8Encoding(false));
                if (File.Exists(path))
                {
                    File.Copy(path, previous, true);
                    File.Delete(path);
                }
                File.Move(temp, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[ClassicSave] Backup write failed: " + e.Message);
                // Keep previous / temp copies for recovery; do not overwrite PlayerPrefs.
                return false;
            }
        }

        private static void RemoveFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception e)
            {
                Debug.LogWarning("[ClassicSave] Delete failed (tombstone still safe): " + e.Message);
            }
        }
    }
}

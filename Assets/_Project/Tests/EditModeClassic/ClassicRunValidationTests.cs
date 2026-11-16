using System;
using NUnit.Framework;
using UnityEngine;
using BlockPuzzle.ClassicPersistence;

namespace BlockPuzzle.Tests.EditModeClassic
{
    public sealed class ClassicRunValidationTests
    {
        private static ClassicRunSnapshot Sample()
        {
            return new ClassicRunSnapshot
            {
                sessionId = "dev3-test-session",
                revision = 1,
                rows = 8,
                columns = 8,
                score = 120,
                combo = 2,
                lastEarnedPoints = 10,
                lastClearedLines = 1,
                multiplier = 1.25f,
                trayBlockIds = new[] { "I2", "", "Single" },
                occupiedCells = new[]
                {
                    new ClassicOccupiedCell
                    {
                        row = 2, column = 4, blockId = 5,
                        red = 0.3f, green = 0.7f, blue = 1f, alpha = 1f
                    }
                }
            };
        }

        [Test] public void ValidSnapshot_Passes()
        {
            Assert.IsTrue(ClassicRunValidator.Validate(Sample(), out _));
        }

        [Test] public void NullSnapshot_Rejects()
        {
            Assert.IsFalse(ClassicRunValidator.Validate(null, out _));
        }

        [Test] public void WrongSaveVersion_Rejects()
        {
            var s = Sample(); s.saveVersion = 1;
            Assert.IsFalse(ClassicRunValidator.Validate(s, out _));
        }

        [Test] public void MissingSession_Rejects()
        {
            var s = Sample(); s.sessionId = "";
            Assert.IsFalse(ClassicRunValidator.Validate(s, out _));
        }

        [Test] public void WrongGrid_Rejects()
        {
            var s = Sample(); s.columns = 9;
            Assert.IsFalse(ClassicRunValidator.Validate(s, out _));
        }

        [Test] public void InvalidRevision_Rejects()
        {
            var s = Sample(); s.revision = 0;
            Assert.IsFalse(ClassicRunValidator.Validate(s, out _));
        }

        [Test] public void WrongTraySize_Rejects()
        {
            var s = Sample(); s.trayBlockIds = new[] { "I2", "I3" };
            Assert.IsFalse(ClassicRunValidator.Validate(s, out _));
        }

        [Test] public void EmptyTray_Rejects()
        {
            var s = Sample(); s.trayBlockIds = new[] { "", "", "" };
            Assert.IsFalse(ClassicRunValidator.Validate(s, out _));
        }

        [Test] public void DuplicateBoardPosition_Rejects()
        {
            var s = Sample();
            s.occupiedCells = new[] { s.occupiedCells[0], s.occupiedCells[0] };
            Assert.IsFalse(ClassicRunValidator.Validate(s, out _));
        }

        [Test] public void OutOfGridPosition_Rejects()
        {
            var s = Sample(); s.occupiedCells[0].row = 8;
            Assert.IsFalse(ClassicRunValidator.Validate(s, out _));
        }

        [Test] public void NonPositiveBlockId_Rejects()
        {
            var s = Sample(); s.occupiedCells[0].blockId = 0;
            Assert.IsFalse(ClassicRunValidator.Validate(s, out _));
        }

        [Test] public void InvalidColor_Rejects()
        {
            var s = Sample(); s.occupiedCells[0].red = float.NaN;
            Assert.IsFalse(ClassicRunValidator.Validate(s, out _));
        }

        [Test] public void InvalidScore_Rejects()
        {
            var s = Sample(); s.score = -4;
            Assert.IsFalse(ClassicRunValidator.Validate(s, out _));
        }

        [Test] public void InvalidMultiplier_Rejects()
        {
            var s = Sample(); s.multiplier = float.PositiveInfinity;
            Assert.IsFalse(ClassicRunValidator.Validate(s, out _));
        }

        [Test] public void Sha256_DetectsTampering()
        {
            const string payload = "classic-run-json";
            string hash = ClassicSaveIntegrity.Hash(payload);
            Assert.IsTrue(ClassicSaveIntegrity.Matches(payload, hash));
            Assert.IsFalse(ClassicSaveIntegrity.Matches(payload + "!", hash));
        }

        [Test] public void Sha256_IsDeterministic()
        {
            Assert.AreEqual(ClassicSaveIntegrity.Hash("same"),
                ClassicSaveIntegrity.Hash("same"));
        }

        [Test] public void JsonUtility_RoundTripRetainsBoardTrayAndCombo()
        {
            ClassicRunSnapshot original = Sample();
            string json = JsonUtility.ToJson(original);
            ClassicRunSnapshot restored = JsonUtility.FromJson<ClassicRunSnapshot>(json);
            Assert.IsTrue(ClassicRunValidator.Validate(restored, out _));
            Assert.AreEqual(original.sessionId, restored.sessionId);
            Assert.AreEqual(original.score, restored.score);
            Assert.AreEqual(original.combo, restored.combo);
            Assert.AreEqual(original.trayBlockIds[2], restored.trayBlockIds[2]);
            Assert.AreEqual(original.occupiedCells[0].blockId, restored.occupiedCells[0].blockId);
        }
    }
}

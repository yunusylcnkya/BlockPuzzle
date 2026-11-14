using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BlockPuzzle.Selection;

namespace BlockPuzzle.Tests.EditMode
{
    public sealed class SmartBlockSelectorTests
    {
        private static readonly string[] Library =
        {
            "Single", "I2", "I3", "L4", "Plus5"
        };

        private static int Size(string block)
        {
            switch (block)
            {
                case "Single": return 1;
                case "I2": return 2;
                case "I3": return 3;
                case "L4": return 4;
                case "Plus5": return 5;
                default: return 1;
            }
        }

        private static IReadOnlyList<string> Pick(
            int seed, SmartDifficulty difficulty,
            Func<string, bool> canPlace = null,
            bool assist = false,
            int traySize = 3)
        {
            return SmartBlockSelector.Select(
                Library, traySize, new Random(seed), Size,
                canPlace, difficulty, assist);
        }

        [Test]
        public void ReturnsThreeCandidates()
        {
            Assert.That(Pick(10, SmartDifficulty.Medium).Count, Is.EqualTo(3));
        }

        [Test]
        public void AvoidsDuplicatesWithLargeLibrary()
        {
            var result = Pick(11, SmartDifficulty.Medium);
            Assert.That(result.Distinct().Count(), Is.EqualTo(3));
        }

        [Test]
        public void RepeatsOnlyWhenNecessary()
        {
            var result = SmartBlockSelector.Select(
                new[] { "Single" }, 3, new Random(12), Size,
                _ => true, SmartDifficulty.Easy, true);
            Assert.That(result, Is.EquivalentTo(new[] { "Single", "Single", "Single" }));
        }

        [Test]
        public void AssistanceIncludesAFittingShapeWhenPossible()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var result = Pick(seed, SmartDifficulty.Easy,
                    block => block == "Single", true);
                Assert.That(result, Does.Contain("Single"));
            }
        }

        [Test]
        public void NoPlaceableCandidateDoesNotThrow()
        {
            var result = Pick(13, SmartDifficulty.Easy, _ => false, true);
            Assert.That(result.Count, Is.EqualTo(3));
        }

        [Test]
        public void DeterministicWithTheSameSeed()
        {
            var first = Pick(2026, SmartDifficulty.Hard);
            var second = Pick(2026, SmartDifficulty.Hard);
            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void EasyFavoursSmallShapesMoreThanExpert()
        {
            int easySmall = 0;
            int expertSmall = 0;
            var easyRandom = new Random(91);
            var expertRandom = new Random(91);
            for (int i = 0; i < 1000; i++)
            {
                var easy = SmartBlockSelector.Select(Library, 1,
                    easyRandom, Size, null, SmartDifficulty.Easy, false);
                var expert = SmartBlockSelector.Select(Library, 1,
                    expertRandom, Size, null, SmartDifficulty.Expert, false);
                if (Size(easy[0]) <= 2) easySmall++;
                if (Size(expert[0]) <= 2) expertSmall++;
            }
            Assert.That(easySmall, Is.GreaterThan(expertSmall));
        }

        [Test]
        public void NullAndDuplicateCandidatesAreIgnored()
        {
            var result = SmartBlockSelector.Select(
                new string[] { "Single", null, "Single", "I2" },
                2, new Random(22), Size, _ => true,
                SmartDifficulty.Medium, false);
            Assert.That(result.Distinct().Count(), Is.EqualTo(2));
            Assert.That(result, Does.Not.Contain(null));
        }

        [Test]
        public void InvalidArgumentsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() =>
                SmartBlockSelector.Select<string>(null, 3, new Random(1),
                    Size, null, SmartDifficulty.Easy, false));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SmartBlockSelector.Select(Library, 0, new Random(1),
                    Size, null, SmartDifficulty.Easy, false));
            Assert.Throws<ArgumentNullException>(() =>
                SmartBlockSelector.Select(Library, 3, new Random(1),
                    Size, null, SmartDifficulty.Easy, true));
        }

        [Test]
        public void OriginalLibraryOrderIsUnchanged()
        {
            var before = Library.ToArray();
            Pick(15, SmartDifficulty.Easy);
            CollectionAssert.AreEqual(before, Library);
        }
    }
}

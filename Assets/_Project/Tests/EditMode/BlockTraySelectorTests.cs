using System;
using System.Linq;
using NUnit.Framework;
using BlockPuzzle.Selection;

namespace BlockPuzzle.Tests.EditMode
{
    public sealed class BlockTraySelectorTests
    {
        private static readonly string[] Library =
        {
            "Single", "I2", "I3", "L4", "Square2"
        };

        [Test]
        public void Select_ReturnsRequestedNumberOfBlocks()
        {
            var selected = BlockTraySelector.Select(Library, 3, new Random(10));
            Assert.That(selected.Count, Is.EqualTo(3));
        }

        [Test]
        public void Select_UsesDistinctCandidatesWhenPossible()
        {
            var selected = BlockTraySelector.Select(Library, 3, new Random(11));
            Assert.That(selected.Distinct().Count(), Is.EqualTo(3));
        }

        [Test]
        public void Select_RepeatsOnlyIfLibraryIsTooSmall()
        {
            var selected = BlockTraySelector.Select(
                new[] { "Single" }, 3, new Random(12));

            Assert.That(selected.Count, Is.EqualTo(3));
            Assert.That(selected.All(x => x == "Single"), Is.True);
        }

        [Test]
        public void Select_GuaranteesPlaceableWhenEnabledAndAvailable()
        {
            var selected = BlockTraySelector.Select(
                Library, 3, new Random(13),
                canPlace: block => block == "Single",
                guaranteePlaceable: true);

            Assert.That(selected, Does.Contain("Single"));
        }

        [Test]
        public void Select_DoesNotCrashWhenNoCandidateFits()
        {
            var selected = BlockTraySelector.Select(
                Library, 3, new Random(14),
                canPlace: _ => false,
                guaranteePlaceable: true);

            Assert.That(selected.Count, Is.EqualTo(3));
        }

        [Test]
        public void Select_SameSeedProducesSameSequence()
        {
            var first = BlockTraySelector.Select(Library, 3, new Random(2026));
            var second = BlockTraySelector.Select(Library, 3, new Random(2026));
            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void Select_WithoutGuaranteeDoesNotInvokePlacementCheck()
        {
            var selected = BlockTraySelector.Select(
                Library, 3, new Random(15),
                canPlace: _ => throw new InvalidOperationException(
                    "Should not check board in legacy-compatible mode."));

            Assert.That(selected.Count, Is.EqualTo(3));
        }

        [Test]
        public void Select_IgnoresNullAndDuplicateEntries()
        {
            var selected = BlockTraySelector.Select(
                new string[] { "I2", null, "I2", "Single" },
                2, new Random(16));
            Assert.That(selected.Distinct().Count(), Is.EqualTo(2));
            Assert.That(selected, Does.Not.Contain(null));
        }

        [Test]
        public void Select_RejectsEmptyOrNullCandidates()
        {
            Assert.Throws<ArgumentException>(() =>
                BlockTraySelector.Select(Array.Empty<string>(), 3, new Random(17)));
            Assert.Throws<ArgumentNullException>(() =>
                BlockTraySelector.Select<string>(null, 3, new Random(17)));
        }

        [Test]
        public void Select_RejectsInvalidArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BlockTraySelector.Select(Library, 0, new Random(18)));
            Assert.Throws<ArgumentNullException>(() =>
                BlockTraySelector.Select(Library, 3, null));
            Assert.Throws<ArgumentNullException>(() =>
                BlockTraySelector.Select(Library, 3, new Random(18),
                    guaranteePlaceable: true));
        }
    }
}

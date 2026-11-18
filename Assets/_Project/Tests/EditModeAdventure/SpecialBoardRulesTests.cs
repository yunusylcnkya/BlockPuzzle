using System;
using System.Collections.Generic;
using NUnit.Framework;
using BlockPuzzle.AdventureRules;

namespace BlockPuzzle.Tests.EditModeAdventure
{
    public sealed class SpecialBoardRulesTests
    {
        private static bool Validate(params SpecialCellSpec[] entries) =>
            SpecialBoardRules.TryValidate(8, 8, entries, out _);

        [Test]
        public void EmptyLayoutIsValid() =>
            Assert.That(Validate(), Is.True);

        [Test]
        public void MixedLayoutIsValid() =>
            Assert.That(Validate(new SpecialCellSpec(0, 1, 1),
                new SpecialCellSpec(6, 7, 2)), Is.True);

        [Test]
        public void NullLayoutIsRejected() =>
            Assert.That(SpecialBoardRules.TryValidate(8, 8, null, out _), Is.False);

        [Test]
        public void ZeroRowsAreRejected() =>
            Assert.That(SpecialBoardRules.TryValidate(0, 8,
                Array.Empty<SpecialCellSpec>(), out _), Is.False);

        [Test]
        public void ZeroColumnsAreRejected() =>
            Assert.That(SpecialBoardRules.TryValidate(8, 0,
                Array.Empty<SpecialCellSpec>(), out _), Is.False);

        [Test]
        public void NegativeRowIsRejected() =>
            Assert.That(Validate(new SpecialCellSpec(-1, 2, 1)), Is.False);

        [Test]
        public void OutsideColumnIsRejected() =>
            Assert.That(Validate(new SpecialCellSpec(2, 8, 1)), Is.False);

        [Test]
        public void OutsideRowIsRejected() =>
            Assert.That(Validate(new SpecialCellSpec(8, 2, 2)), Is.False);

        [Test]
        public void DuplicatePositionIsRejected() =>
            Assert.That(Validate(new SpecialCellSpec(1, 3, 1),
                new SpecialCellSpec(1, 3, 2)), Is.False);

        [Test]
        public void NoneAsSpecialKindIsRejected() =>
            Assert.That(Validate(new SpecialCellSpec(1, 3, 0)), Is.False);

        [Test]
        public void UnknownSpecialKindIsRejected() =>
            Assert.That(Validate(new SpecialCellSpec(1, 3, 3)), Is.False);

        [Test]
        public void FullyLockedRowIsRejected()
        {
            var entries = new List<SpecialCellSpec>();
            for (int col = 0; col < 8; col++)
                entries.Add(new SpecialCellSpec(2, col, 1));
            Assert.That(SpecialBoardRules.TryValidate(8, 8, entries, out _), Is.False);
        }

        [Test]
        public void FullyLockedColumnIsRejected()
        {
            var entries = new List<SpecialCellSpec>();
            for (int row = 0; row < 8; row++)
                entries.Add(new SpecialCellSpec(row, 4, 1));
            Assert.That(SpecialBoardRules.TryValidate(8, 8, entries, out _), Is.False);
        }

        [Test]
        public void NearlyFullLockedRowCanBeValid()
        {
            var entries = new List<SpecialCellSpec>();
            for (int col = 0; col < 7; col++)
                entries.Add(new SpecialCellSpec(2, col, 1));
            Assert.That(SpecialBoardRules.TryValidate(8, 8, entries, out _), Is.True);
        }

        [Test]
        public void LockedCellsArePermanentAndObstaclesAreBreakable()
        {
            Assert.That(SpecialBoardRules.IsPermanent(1), Is.True);
            Assert.That(SpecialBoardRules.IsPermanent(2), Is.False);
            Assert.That(SpecialBoardRules.IsBreakable(2), Is.True);
            Assert.That(SpecialBoardRules.IsBreakable(1), Is.False);
        }

        [Test]
        public void PlacementIsAllowedOnlyOnEmptyNormalCell()
        {
            Assert.That(SpecialBoardRules.CanPlace(false, 0), Is.True);
            Assert.That(SpecialBoardRules.CanPlace(true, 0), Is.False);
            Assert.That(SpecialBoardRules.CanPlace(false, 1), Is.False);
            Assert.That(SpecialBoardRules.CanPlace(false, 2), Is.False);
        }
    }
}

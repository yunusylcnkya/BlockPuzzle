using NUnit.Framework;
using BlockPuzzle.ClassicPersistence;

namespace BlockPuzzle.Tests.EditModeClassic
{
    public sealed class ClassicRewardedValidationTests
    {
        private static ClassicRunSnapshot Valid()
        {
            return new ClassicRunSnapshot
            {
                sessionId = "rewarded-test-session",
                revision = 1,
                rows = 8,
                columns = 8,
                trayBlockIds = new [] { "Single", "", "" },
                occupiedCells = new ClassicOccupiedCell[0]
            };
        }

        [Test]
        public void NoReshuffles_IsValid()
        {
            var run = Valid();
            run.rewardedRerollsUsed = 0;
            Assert.IsTrue(ClassicRunValidator.Validate(run, out _));
        }

        [Test]
        public void FiveReshuffles_IsValid()
        {
            var run = Valid();
            run.rewardedRerollsUsed = 5;
            Assert.IsTrue(ClassicRunValidator.Validate(run, out _));
        }

        [Test]
        public void NegativeReshuffleCount_IsInvalid()
        {
            var run = Valid();
            run.rewardedRerollsUsed = -1;
            Assert.IsFalse(ClassicRunValidator.Validate(run, out _));
        }

        [Test]
        public void MoreThanFiveReshuffles_IsInvalid()
        {
            var run = Valid();
            run.rewardedRerollsUsed = 6;
            Assert.IsFalse(ClassicRunValidator.Validate(run, out _));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BlockPuzzle.AdventureRules;

namespace BlockPuzzle.Tests.EditModeAdventure
{
    public sealed class AdventureDev5PackTests
    {
        private static IReadOnlyList<AdventureDev5Blueprint> Levels => AdventureDev5Pack.Levels;

        [Test] public void PackContainsExactly20Levels() =>
            Assert.That(Levels.Count, Is.EqualTo(20));

        [Test] public void IdsAre101Through120() =>
            CollectionAssert.AreEqual(Enumerable.Range(101, 20), Levels.Select(x => x.Id));

        [Test] public void IdsAreUnique() =>
            Assert.That(Levels.Select(x => x.Id).Distinct().Count(), Is.EqualTo(20));

        [Test] public void ValidationPasses() =>
            Assert.That(AdventureDev5Pack.TryValidate(out _), Is.True);

        [Test] public void MissingIdsReturnNull()
        {
            Assert.That(AdventureDev5Pack.Find(100), Is.Null);
            Assert.That(AdventureDev5Pack.Find(121), Is.Null);
        }

        [Test] public void EveryIdCanBeFound()
        {
            foreach (AdventureDev5Blueprint level in Levels)
                Assert.That(AdventureDev5Pack.Find(level.Id), Is.SameAs(level));
        }

        [Test] public void EveryLevelHasSpecialTiles()
        {
            Assert.That(Levels.All(x => x.Cells != null && x.Cells.Count > 0), Is.True);
        }

        [Test] public void AllBoardLayoutsPassDev4Rules()
        {
            foreach (AdventureDev5Blueprint level in Levels)
                Assert.That(SpecialBoardRules.TryValidate(8, 8, level.Cells, out _),
                    Is.True, "Level " + level.Id);
        }

        [Test] public void AllObstacleTargetsAreWithinInitialObstacleCounts()
        {
            foreach (AdventureDev5Blueprint level in Levels.Where(x => x.Objective == 3))
            {
                int obstacles = level.Cells.Count(x => x.Kind == 2);
                Assert.That(level.Target, Is.LessThanOrEqualTo(obstacles),
                    "Level " + level.Id);
            }
        }

        [Test] public void AllDifficultyTiersAreRepresented() =>
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 },
                Levels.Select(x => x.Difficulty).Distinct());

        [Test] public void AllSixObjectivesAreRepresented() =>
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3, 4, 5 },
                Levels.Select(x => x.Objective).Distinct());

        [Test] public void LevelsHaveAtLeast48InitiallyEmptyCells() =>
            Assert.That(Levels.All(x => 64 - x.Cells.Count >= 48), Is.True);

        [Test] public void MoveBudgetsAreNonzeroAndBounded() =>
            Assert.That(Levels.All(x => x.Moves >= 20 && x.Moves <= 40), Is.True);

        [Test] public void PlaceBlocksTargetsFitWithinMoveBudgets()
        {
            foreach (AdventureDev5Blueprint level in Levels.Where(x => x.Objective == 2))
                Assert.That(level.Target, Is.LessThanOrEqualTo(level.Moves));
        }

        [Test] public void LayoutsDoNotContainDuplicateCoordinates()
        {
            foreach (AdventureDev5Blueprint level in Levels)
                Assert.That(level.Cells.Select(x => x.Row * 8 + x.Column).Distinct().Count(),
                    Is.EqualTo(level.Cells.Count), "Level " + level.Id);
        }

        [Test] public void PackDefinitionsAreStableAcrossAccess()
        {
            AdventureDev5Blueprint first = AdventureDev5Pack.Find(101);
            Assert.That(first.Title, Is.EqualTo("FIRST CRACK"));
            Assert.That(first.Cells.Count, Is.EqualTo(2));
        }
    }
}

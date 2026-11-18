using System;
using System.Collections.Generic;

namespace BlockPuzzle.AdventureRules
{
    /// <summary>
    /// Editor-authored definitions for levels 101-120.  No UnityEngine
    /// references: usable by EditMode tests without loading scenes or saves.
    /// 0..3 = existing AdventureDifficulty values; 0..5 =
    /// existing AdventureObjective values (do not reorder those enums).
    /// </summary>
    public sealed class AdventureDev5Blueprint
    {
        public int Id { get; }
        public string Title { get; }
        public int Difficulty { get; }
        public int Objective { get; }
        public int Target { get; }
        public int Moves { get; }
        public IReadOnlyList<SpecialCellSpec> Cells { get; }

        internal AdventureDev5Blueprint(int id, string title,
            int difficulty, int objective, int target, int moves,
            params SpecialCellSpec[] cells)
        {
            Id = id;
            Title = title;
            Difficulty = difficulty;
            Objective = objective;
            Target = target;
            Moves = moves;
            Cells = Array.AsReadOnly((SpecialCellSpec[])cells.Clone());
        }
    }

    public static class AdventureDev5Pack
    {
        public const int FirstLevel = 101;
        public const int LastLevel = 120;
        public const int LevelCount = 20;
        public const int BoardSize = 8;

        // AdventureObjective: 0=ReachScore, 1=ClearLines, 2=PlaceBlocks,
        // 3=ClearObstacles, 4=ClearRows, 5=ClearColumns.
        public static IReadOnlyList<AdventureDev5Blueprint> Levels { get; } =
            Array.AsReadOnly(new AdventureDev5Blueprint[]
            {
                new AdventureDev5Blueprint(101, "FIRST CRACK", 0, 3, 1, 24,
                    new SpecialCellSpec(0, 2, 2),
                    new SpecialCellSpec(0, 5, 2)),
                new AdventureDev5Blueprint(102, "COLUMN GATE", 0, 5, 1, 26,
                    new SpecialCellSpec(0, 1, 1),
                    new SpecialCellSpec(7, 6, 1),
                    new SpecialCellSpec(2, 3, 2)),
                new AdventureDev5Blueprint(103, "REINFORCED ROW", 0, 4, 1, 26,
                    new SpecialCellSpec(3, 2, 2),
                    new SpecialCellSpec(3, 5, 2),
                    new SpecialCellSpec(6, 1, 1)),
                new AdventureDev5Blueprint(104, "SAFE PASSAGE", 0, 2, 8, 21,
                    new SpecialCellSpec(1, 2, 1),
                    new SpecialCellSpec(6, 6, 1),
                    new SpecialCellSpec(4, 4, 2)),
                new AdventureDev5Blueprint(105, "DOUBLE BREAK", 1, 3, 2, 27,
                    new SpecialCellSpec(0, 1, 2),
                    new SpecialCellSpec(0, 5, 2),
                    new SpecialCellSpec(5, 3, 2),
                    new SpecialCellSpec(7, 7, 1)),
                new AdventureDev5Blueprint(106, "NARROW AISLE", 1, 5, 1, 29,
                    new SpecialCellSpec(0, 0, 1),
                    new SpecialCellSpec(3, 3, 1),
                    new SpecialCellSpec(7, 7, 1),
                    new SpecialCellSpec(2, 4, 2)),
                new AdventureDev5Blueprint(107, "TWO LINES", 1, 1, 2, 29,
                    new SpecialCellSpec(2, 3, 2),
                    new SpecialCellSpec(5, 3, 2),
                    new SpecialCellSpec(7, 1, 1),
                    new SpecialCellSpec(0, 6, 1)),
                new AdventureDev5Blueprint(108, "OPENING SCORE", 1, 0, 700, 27,
                    new SpecialCellSpec(0, 1, 2),
                    new SpecialCellSpec(4, 5, 2),
                    new SpecialCellSpec(6, 6, 1)),
                new AdventureDev5Blueprint(109, "CROSSROADS", 1, 4, 2, 32,
                    new SpecialCellSpec(1, 3, 2),
                    new SpecialCellSpec(1, 5, 2),
                    new SpecialCellSpec(6, 2, 2),
                    new SpecialCellSpec(4, 7, 1)),
                new AdventureDev5Blueprint(110, "BREAK THE WALL", 1, 3, 2, 32,
                    new SpecialCellSpec(5, 1, 2),
                    new SpecialCellSpec(5, 4, 2),
                    new SpecialCellSpec(2, 6, 1),
                    new SpecialCellSpec(7, 6, 1)),
                new AdventureDev5Blueprint(111, "PATCHWORK", 2, 2, 9, 29,
                    new SpecialCellSpec(1, 1, 1),
                    new SpecialCellSpec(6, 2, 1),
                    new SpecialCellSpec(3, 6, 1),
                    new SpecialCellSpec(4, 4, 2)),
                new AdventureDev5Blueprint(112, "FORKED PATH", 2, 5, 2, 34,
                    new SpecialCellSpec(2, 1, 2),
                    new SpecialCellSpec(4, 1, 2),
                    new SpecialCellSpec(0, 4, 1),
                    new SpecialCellSpec(7, 5, 1)),
                new AdventureDev5Blueprint(113, "RISING PRESSURE", 2, 3, 3, 33,
                    new SpecialCellSpec(0, 1, 2),
                    new SpecialCellSpec(0, 4, 2),
                    new SpecialCellSpec(0, 7, 2),
                    new SpecialCellSpec(5, 5, 1),
                    new SpecialCellSpec(6, 2, 1)),
                new AdventureDev5Blueprint(114, "STEP BY STEP", 2, 4, 2, 34,
                    new SpecialCellSpec(3, 1, 2),
                    new SpecialCellSpec(3, 6, 2),
                    new SpecialCellSpec(1, 2, 1),
                    new SpecialCellSpec(6, 4, 1)),
                new AdventureDev5Blueprint(115, "HARD RESET", 2, 1, 3, 36,
                    new SpecialCellSpec(1, 0, 1),
                    new SpecialCellSpec(6, 7, 1),
                    new SpecialCellSpec(4, 2, 2),
                    new SpecialCellSpec(4, 5, 2)),
                new AdventureDev5Blueprint(116, "SCORE UNDER FIRE", 2, 0, 1050, 36,
                    new SpecialCellSpec(1, 1, 1),
                    new SpecialCellSpec(2, 6, 1),
                    new SpecialCellSpec(6, 4, 1),
                    new SpecialCellSpec(0, 3, 2),
                    new SpecialCellSpec(5, 3, 2)),
                new AdventureDev5Blueprint(117, "BREAK THREE", 3, 3, 3, 36,
                    new SpecialCellSpec(7, 1, 2),
                    new SpecialCellSpec(7, 3, 2),
                    new SpecialCellSpec(7, 6, 2),
                    new SpecialCellSpec(2, 2, 1),
                    new SpecialCellSpec(5, 5, 1)),
                new AdventureDev5Blueprint(118, "CROWDED BOARD", 3, 2, 10, 30,
                    new SpecialCellSpec(0, 0, 1),
                    new SpecialCellSpec(0, 7, 1),
                    new SpecialCellSpec(7, 0, 1),
                    new SpecialCellSpec(7, 7, 1),
                    new SpecialCellSpec(3, 3, 2),
                    new SpecialCellSpec(4, 4, 2)),
                new AdventureDev5Blueprint(119, "COLUMN SPRINT", 3, 5, 2, 37,
                    new SpecialCellSpec(3, 1, 1),
                    new SpecialCellSpec(3, 6, 1),
                    new SpecialCellSpec(1, 4, 2),
                    new SpecialCellSpec(6, 4, 2)),
                new AdventureDev5Blueprint(120, "MASTER BREAKER", 3, 3, 4, 40,
                    new SpecialCellSpec(1, 1, 2),
                    new SpecialCellSpec(1, 3, 2),
                    new SpecialCellSpec(1, 5, 2),
                    new SpecialCellSpec(1, 7, 2),
                    new SpecialCellSpec(6, 0, 1),
                    new SpecialCellSpec(6, 7, 1))
            });

        public static AdventureDev5Blueprint Find(int id)
        {
            foreach (AdventureDev5Blueprint level in Levels)
                if (level.Id == id) return level;
            return null;
        }

        public static bool TryValidate(out string reason)
        {
            if (Levels.Count != LevelCount)
            {
                reason = "Expected exactly 20 levels.";
                return false;
            }

            var usedIds = new HashSet<int>();
            for (int index = 0; index < Levels.Count; index++)
            {
                AdventureDev5Blueprint level = Levels[index];
                if (level == null || level.Id != FirstLevel + index ||
                    !usedIds.Add(level.Id))
                {
                    reason = "Level IDs must be unique and consecutively ordered 101..120.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(level.Title) || level.Title.Length > 64 ||
                    level.Difficulty < 0 || level.Difficulty > 3 ||
                    level.Objective < 0 || level.Objective > 5 ||
                    level.Target <= 0 || level.Moves <= 0 || level.Moves > 60)
                {
                    reason = "Invalid metadata at level " + level.Id;
                    return false;
                }
                string boardError = "special cell list is empty";
                if (level.Cells == null || level.Cells.Count == 0 ||
                    !SpecialBoardRules.TryValidate(BoardSize, BoardSize, level.Cells,
                        out boardError))
                {
                    reason = "Invalid board at level " + level.Id + ": " + boardError;
                    return false;
                }

                int breakables = 0;
                int locked = 0;
                foreach (SpecialCellSpec cell in level.Cells)
                {
                    if (cell.Kind == SpecialBoardRules.BreakableObstacle) breakables++;
                    if (cell.Kind == SpecialBoardRules.Locked) locked++;
                }
                if (level.Objective == 3 &&
                    (breakables == 0 || level.Target > breakables))
                {
                    reason = "Impossible obstacle target at level " + level.Id;
                    return false;
                }
                if (locked + breakables > 12 || 64 - locked - breakables < 48)
                {
                    reason = "Board too congested at level " + level.Id;
                    return false;
                }
                if (level.Objective == 2 && level.Target > level.Moves)
                {
                    reason = "PlaceBlocks target exceeds move limit at level " + level.Id;
                    return false;
                }
                if ((level.Objective == 4 || level.Objective == 5 ||
                    level.Objective == 1) && level.Target > 5)
                {
                    reason = "Line-clear target outside playtest limit at level " + level.Id;
                    return false;
                }
            }
            reason = null;
            return true;
        }
    }
}

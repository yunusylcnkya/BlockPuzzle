using System;
using System.Collections.Generic;

namespace BlockPuzzle.AdventureRules
{
    // Pure C# data: 1=Locked, 2=BreakableObstacle.
    // Deliberately does not reference UnityEngine.
    public readonly struct SpecialCellSpec
    {
        public readonly int Row;
        public readonly int Column;
        public readonly int Kind;

        public SpecialCellSpec(int row, int column, int kind)
        {
            Row = row;
            Column = column;
            Kind = kind;
        }
    }

    public static class SpecialBoardRules
    {
        public const int Locked = 1;
        public const int BreakableObstacle = 2;

        public static bool IsPermanent(int kind) => kind == Locked;
        public static bool IsBreakable(int kind) => kind == BreakableObstacle;
        public static bool CanPlace(bool occupied, int kind) =>
            !occupied && kind == 0;

        /// <summary>
        /// Validates a single level's initial layout before touching the grid.
        /// Full rows/columns of permanent locks are rejected because they
        /// could trigger repeated line clears without free player cells.
        /// </summary>
        public static bool TryValidate(
            int rows,
            int columns,
            IReadOnlyList<SpecialCellSpec> cells,
            out string reason)
        {
            if (rows <= 0 || columns <= 0)
            {
                reason = "Invalid board size.";
                return false;
            }

            if (cells == null)
            {
                reason = "Special cell collection is null.";
                return false;
            }

            var positions = new HashSet<long>();
            var lockedRows = new int[rows];
            var lockedColumns = new int[columns];

            for (int i = 0; i < cells.Count; i++)
            {
                SpecialCellSpec spec = cells[i];
                if (spec.Row < 0 || spec.Row >= rows ||
                    spec.Column < 0 || spec.Column >= columns)
                {
                    reason = "Special cell is outside the board at index " + i + ".";
                    return false;
                }

                if (spec.Kind != Locked && spec.Kind != BreakableObstacle)
                {
                    reason = "Invalid special cell kind at index " + i + ".";
                    return false;
                }

                long key = ((long)spec.Row << 32) | (uint)spec.Column;
                if (!positions.Add(key))
                {
                    reason = "Duplicate special cell at (" +
                        spec.Row + "," + spec.Column + ").";
                    return false;
                }

                if (spec.Kind == Locked)
                {
                    lockedRows[spec.Row]++;
                    lockedColumns[spec.Column]++;
                }
            }

            for (int row = 0; row < rows; row++)
            {
                if (lockedRows[row] == columns)
                {
                    reason = "An entire row cannot consist of permanent locks.";
                    return false;
                }
            }

            for (int column = 0; column < columns; column++)
            {
                if (lockedColumns[column] == rows)
                {
                    reason = "An entire column cannot consist of permanent locks.";
                    return false;
                }
            }

            reason = null;
            return true;
        }
    }
}

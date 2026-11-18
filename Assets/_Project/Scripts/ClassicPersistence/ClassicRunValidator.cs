using System;
using System.Collections.Generic;

namespace BlockPuzzle.ClassicPersistence
{
    public static class ClassicRunValidator
    {
        public static bool Validate(ClassicRunSnapshot run, out string reason)
        {
            reason = string.Empty;
            if (run == null) return Fail("Snapshot missing", out reason);
            if (run.saveVersion != 2) return Fail("Unsupported save version", out reason);
            if (string.IsNullOrWhiteSpace(run.sessionId) || run.sessionId.Length > 64)
                return Fail("Invalid Classic session", out reason);
            if (run.revision < 1 || run.revision == int.MaxValue)
                return Fail("Invalid revision", out reason);
            // Classic is currently an 8x8 game; do not restore onto a different grid.
            if (run.rows != 8 || run.columns != 8)
                return Fail("Unexpected Classic grid dimensions", out reason);
            if (run.score < 0 || run.combo < 0 || run.lastEarnedPoints < 0 ||
                run.lastClearedLines < 0 || run.lastClearedLines > run.rows + run.columns)
                return Fail("Invalid score state", out reason);
            if (!Finite(run.multiplier) || run.multiplier < 1f || run.multiplier > 100f)
                return Fail("Invalid score multiplier", out reason);
            if (run.rewardedRerollsUsed < 0 || run.rewardedRerollsUsed > 5)
                return Fail("Invalid rewarded shuffle usage", out reason);
            if (run.trayBlockIds == null || run.trayBlockIds.Length != 3)
                return Fail("Invalid tray slot count", out reason);
            int present = 0;
            foreach (string id in run.trayBlockIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (string.IsNullOrWhiteSpace(id) || id.Length > 64)
                    return Fail("Invalid block ID", out reason);
                present++;
            }
            if (present == 0) return Fail("Empty tray", out reason);
            if (run.occupiedCells == null || run.occupiedCells.Length > 64)
                return Fail("Invalid board cell list", out reason);
            var positions = new HashSet<int>();
            foreach (ClassicOccupiedCell cell in run.occupiedCells)
            {
                if (cell == null || cell.row < 0 || cell.row >= run.rows ||
                    cell.column < 0 || cell.column >= run.columns ||
                    cell.blockId < 1 || cell.blockId >= int.MaxValue - 1)
                    return Fail("Invalid occupied cell", out reason);
                if (!positions.Add(cell.row * run.columns + cell.column))
                    return Fail("Duplicate occupied cell", out reason);
                if (!ColorComponent(cell.red) || !ColorComponent(cell.green) ||
                    !ColorComponent(cell.blue) || !ColorComponent(cell.alpha))
                    return Fail("Invalid cell color", out reason);
            }
            return true;
        }

        private static bool ColorComponent(float x) => Finite(x) && x >= 0f && x <= 1f;
        private static bool Finite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
        private static bool Fail(string message, out string reason)
        {
            reason = message;
            return false;
        }
    }
}

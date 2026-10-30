
using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzle.Grid
{
    public sealed class LineClearResult
    {
        public int ClearedRowCount { get; }
        public int ClearedColumnCount { get; }

        public int TotalLines =>
            ClearedRowCount + ClearedColumnCount;

        public int ClearedCellCount =>
            ClearedCells.Count;

        public IReadOnlyList<Vector2Int> ClearedCells { get; }

        public bool HasClearedLines => TotalLines > 0;

        public LineClearResult(
            int clearedRowCount,
            int clearedColumnCount,
            List<Vector2Int> clearedCells)
        {
            ClearedRowCount = clearedRowCount;
            ClearedColumnCount = clearedColumnCount;

            ClearedCells = clearedCells.AsReadOnly();
        }
    }
}

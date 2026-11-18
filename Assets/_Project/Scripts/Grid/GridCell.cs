
namespace BlockPuzzle.Grid
{
    // Appended mechanics; no change to existing normal-cell behavior.
    public enum SpecialCellKind
    {
        None = 0,
        Locked = 1,
        BreakableObstacle = 2
    }

    public sealed class GridCell
    {
        public int Row { get; }
        public int Column { get; }

        public bool IsOccupied { get; private set; }

        public int BlockId { get; private set; }
        public SpecialCellKind SpecialKind { get; private set; }

        public GridCell(int row, int column)
        {
            Row = row;
            Column = column;

            IsOccupied = false;
            BlockId = -1;
        }

        public bool TryOccupy(int blockId)
        {
            if (IsOccupied || SpecialKind != SpecialCellKind.None)
                return false;

            IsOccupied = true;
            BlockId = blockId;

            return true;
        }

        public bool TrySetSpecial(SpecialCellKind kind)
        {
            if (IsOccupied || SpecialKind != SpecialCellKind.None ||
                (kind != SpecialCellKind.Locked &&
                 kind != SpecialCellKind.BreakableObstacle))
                return false;

            SpecialKind = kind;
            IsOccupied = true;
            BlockId = kind == SpecialCellKind.Locked ? -3 : -4;
            return true;
        }

        public void Clear()
        {
            IsOccupied = false;
            BlockId = -1;
            SpecialKind = SpecialCellKind.None;
        }
    }
}

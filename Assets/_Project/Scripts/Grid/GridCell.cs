
namespace BlockPuzzle.Grid
{
    public sealed class GridCell
    {
        public int Row { get; }
        public int Column { get; }

        public bool IsOccupied { get; private set; }

        public int BlockId { get; private set; }

        public GridCell(int row, int column)
        {
            Row = row;
            Column = column;

            IsOccupied = false;
            BlockId = -1;
        }

        public bool TryOccupy(int blockId)
        {
            if (IsOccupied)
                return false;

            IsOccupied = true;
            BlockId = blockId;

            return true;
        }

        public void Clear()
        {
            IsOccupied = false;
            BlockId = -1;
        }
    }
}


namespace BlockPuzzle.Grid
{
    public sealed class BlockPlacementResult
    {
        public int PlacedCellCount { get; }
        public int BlockId { get; }
        public LineClearResult LineClear { get; }

        public BlockPlacementResult(
            int placedCellCount,
            int blockId,
            LineClearResult lineClear)
        {
            PlacedCellCount = placedCellCount;
            BlockId = blockId;
            LineClear = lineClear;
        }
    }
}

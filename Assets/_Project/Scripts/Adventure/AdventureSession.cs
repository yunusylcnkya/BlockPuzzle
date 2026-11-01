
namespace BlockPuzzle.Adventure
{
    public static class AdventureSession
    {
        public static int SelectedLevelId { get; private set; }

        public static void SelectLevel(int levelId)
        {
            SelectedLevelId = levelId;
        }

        public static void Clear()
        {
            SelectedLevelId = 0;
        }
    }
}

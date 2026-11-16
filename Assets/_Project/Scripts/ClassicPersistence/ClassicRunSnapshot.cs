using System;

namespace BlockPuzzle.ClassicPersistence
{
    // The Classic run is intentionally separate from AdventureSaveData V1.
    [Serializable]
    public sealed class ClassicOccupiedCell
    {
        public int row;
        public int column;
        public int blockId;
        public float red;
        public float green;
        public float blue;
        public float alpha = 1f;
    }

    [Serializable]
    public sealed class ClassicRunSnapshot
    {
        public int saveVersion = 2;
        public int revision = 1;
        public string sessionId = "";
        public int rows = 8;
        public int columns = 8;

        public int score;
        public int combo;
        public int lastEarnedPoints;
        public int lastClearedLines;
        public float multiplier = 1f;

        // Three slots in their original positions. Empty string = used slot.
        public string[] trayBlockIds = new string[3];
        public ClassicOccupiedCell[] occupiedCells = Array.Empty<ClassicOccupiedCell>();
    }
}

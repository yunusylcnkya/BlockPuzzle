
using System;
using System.Collections.Generic;
using UnityEngine;
using BlockPuzzle.Grid;

namespace BlockPuzzle.Adventure
{
    public enum AdventureDifficulty
    {
        Easy,
        Medium,
        Hard,
        Expert
    }

    public enum AdventureObjective
    {
        ReachScore,
        ClearLines,
        PlaceBlocks,
        ClearObstacles,
        ClearRows,
        ClearColumns
    }

    [Serializable]
    public sealed class AdventureSpecialCell
    {
        [Min(0)] public int row;
        [Min(0)] public int column;
        public SpecialCellKind kind = SpecialCellKind.Locked;
    }

    [CreateAssetMenu(
        fileName = "Level_001",
        menuName = "Block Puzzle/Adventure/Level Definition"
    )]
    public sealed class AdventureLevelDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Min(1)] private int levelId = 1;
        [SerializeField] private string levelName = "First Steps";

        [Header("Difficulty")]
        [SerializeField]
        private AdventureDifficulty difficulty =
            AdventureDifficulty.Easy;

        [Header("Objective - Used in v0.9")]
        [SerializeField]
        private AdventureObjective objective =
            AdventureObjective.ReachScore;

        [SerializeField, Min(1)] private int targetValue = 100;

        [SerializeField, Min(1)] private int moveLimit = 15;

        [Header("Special Board - v2.0.0-dev.4 (opt-in)")]
        [SerializeField] private bool enableSpecialBoard = false;
        [SerializeField] private List<AdventureSpecialCell> specialCells = new();

        public bool EnableSpecialBoard => enableSpecialBoard;
        public IReadOnlyList<AdventureSpecialCell> SpecialCells => specialCells;

        public int LevelId => levelId;
        public string LevelName => levelName;
        public AdventureDifficulty Difficulty => difficulty;
        public AdventureObjective Objective => objective;
        public int TargetValue => targetValue;
        public int MoveLimit => moveLimit;
    }
}

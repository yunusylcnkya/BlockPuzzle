
using UnityEngine;

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
        PlaceBlocks
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

        public int LevelId => levelId;
        public string LevelName => levelName;
        public AdventureDifficulty Difficulty => difficulty;
        public AdventureObjective Objective => objective;
        public int TargetValue => targetValue;
        public int MoveLimit => moveLimit;
    }
}


using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzle.Adventure
{
    [CreateAssetMenu(
        fileName = "AdventureCatalog",
        menuName = "Block Puzzle/Adventure/Level Catalog"
    )]
    public sealed class AdventureLevelCatalog : ScriptableObject
    {
        [SerializeField]
        private List<AdventureLevelDefinition> levels = new();

        public IReadOnlyList<AdventureLevelDefinition> Levels => levels;

        public AdventureLevelDefinition GetLevel(int levelId)
        {
            foreach (AdventureLevelDefinition level in levels)
            {
                if (level != null && level.LevelId == levelId)
                    return level;
            }

            return null;
        }

        public bool ContainsLevel(int levelId)
        {
            return GetLevel(levelId) != null;
        }

        private void OnValidate()
        {
            if (levels == null)
                levels = new List<AdventureLevelDefinition>();
        }
    }
}

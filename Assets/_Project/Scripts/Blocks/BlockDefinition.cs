
using System.Collections.Generic;
using UnityEngine;

namespace BlockPuzzle.Blocks
{
    [CreateAssetMenu(
        fileName = "BlockDefinition",
        menuName = "Block Puzzle/Block Definition"
    )]
    public sealed class BlockDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string blockId;

        [Header("Visual")]
        [SerializeField] private Color blockColor = Color.cyan;

        [Header("Shape")]
        [SerializeField] private List<Vector2Int> cells = new();

        public string BlockId => blockId;
        public Color BlockColor => blockColor;
        public IReadOnlyList<Vector2Int> Cells => cells;

        public int Width
        {
            get
            {
                int max = 0;

                foreach (Vector2Int cell in cells)
                {
                    max = Mathf.Max(max, cell.x);
                }

                return max + 1;
            }
        }

        public int Height
        {
            get
            {
                int max = 0;

                foreach (Vector2Int cell in cells)
                {
                    max = Mathf.Max(max, cell.y);
                }

                return max + 1;
            }
        }

        public bool IsValid()
        {
            if (cells == null || cells.Count == 0)
                return false;

            HashSet<Vector2Int> uniqueCells = new();

            foreach (Vector2Int cell in cells)
            {
                if (cell.x < 0 || cell.y < 0)
                    return false;

                if (!uniqueCells.Add(cell))
                    return false;
            }

            return true;
        }

        private void OnValidate()
        {
            if (cells == null)
                cells = new List<Vector2Int>();
        }
    }
}

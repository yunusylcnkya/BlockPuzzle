
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Grid
{
    [RequireComponent(typeof(Image))]
    public sealed class GridCellView : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField]
        private Color emptyColor =
            new Color32(53, 66, 99, 255);

        [SerializeField]
        private Color occupiedColor =
            new Color32(40, 190, 235, 255);

        private Image cellImage;
        private GridCell cell;

        public GridCell Cell => cell;

        private void Awake()
        {
            cellImage = GetComponent<Image>();
            cellImage.raycastTarget = false;
        }

        public void Initialize(GridCell gridCell)
        {
            cell = gridCell;
            Refresh();
        }

        public void Refresh()
        {
            if (cell == null)
                return;

            cellImage.color = cell.IsOccupied
                ? occupiedColor
                : emptyColor;
        }
    }
}

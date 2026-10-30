
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

        [SerializeField]
        private Color validPreviewColor =
            new Color32(70, 205, 135, 255);

        [SerializeField]
        private Color invalidPreviewColor =
            new Color32(235, 80, 90, 255);

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

            EnsureImage();

            cellImage.color = cell.IsOccupied
                ? occupiedColor
                : emptyColor;
        }

        public void ShowPreview(bool isValid)
        {
            EnsureImage();

            cellImage.color = isValid
                ? validPreviewColor
                : invalidPreviewColor;
        }

        private void EnsureImage()
        {
            if (cellImage == null)
                cellImage = GetComponent<Image>();
        }
    }
}

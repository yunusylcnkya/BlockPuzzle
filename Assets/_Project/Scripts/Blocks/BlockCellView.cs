
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Blocks
{
    [RequireComponent(typeof(Image))]
    public sealed class BlockCellView : MonoBehaviour
    {
        private Image cellImage;

        private void Awake()
        {
            cellImage = GetComponent<Image>();
            cellImage.raycastTarget = false;
        }

        public void SetColor(Color color)
        {
            if (cellImage == null)
                cellImage = GetComponent<Image>();

            cellImage.color = color;
        }
    }
}

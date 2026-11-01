
using System.Collections;
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
            new Color32(43, 67, 104, 255);

        [SerializeField]
        private Color defaultOccupiedColor =
            new Color32(72, 199, 232, 255);

        [SerializeField]
        private Color validPreviewColor =
            new Color32(82, 212, 156, 255);

        [SerializeField]
        private Color invalidPreviewColor =
            new Color32(240, 108, 121, 255);

        [Header("Clear Effect")]
        [SerializeField]
        private Color clearFlashColor =
            new Color32(255, 238, 160, 255);

        [SerializeField, Min(0.01f)]
        private float flashDuration = 0.25f;

        private Image cellImage;
        private GridCell cell;

        private Color placedBlockColor;
        private bool hasPlacedColor;

        private Coroutine flashRoutine;

        public GridCell Cell => cell;

        private void Awake()
        {
            cellImage = GetComponent<Image>();
            cellImage.raycastTarget = false;
        }

        public void Initialize(GridCell gridCell)
        {
            cell = gridCell;
            hasPlacedColor = false;

            Refresh();
        }

        public void SetOccupiedColor(Color color)
        {
            placedBlockColor = color;
            hasPlacedColor = true;

            Refresh();
        }

        public void ResetOccupiedColor()
        {
            hasPlacedColor = false;

            Refresh();
        }

        public void Refresh()
        {
            EnsureImage();

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }

            if (cell == null)
            {
                cellImage.color = emptyColor;
                return;
            }

            if (!cell.IsOccupied)
            {
                cellImage.color = emptyColor;
                return;
            }

            cellImage.color = hasPlacedColor
                ? placedBlockColor
                : defaultOccupiedColor;
        }

        public void ShowPreview(bool isValid)
        {
            EnsureImage();

            cellImage.color = isValid
                ? validPreviewColor
                : invalidPreviewColor;
        }

        public void PlayClearFlash()
        {
            EnsureImage();

            if (flashRoutine != null)
                StopCoroutine(flashRoutine);

            flashRoutine = StartCoroutine(
                AnimateClearFlash()
            );
        }

        private IEnumerator AnimateClearFlash()
        {
            float elapsed = 0f;

            while (elapsed < flashDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / flashDuration
                );

                cellImage.color = Color.Lerp(
                    clearFlashColor,
                    emptyColor,
                    t
                );

                yield return null;
            }

            cellImage.color = emptyColor;
            flashRoutine = null;
        }

        private void EnsureImage()
        {
            if (cellImage == null)
                cellImage = GetComponent<Image>();
        }
    }
}

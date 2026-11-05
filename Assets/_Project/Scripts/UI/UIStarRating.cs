
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.UI
{
    public sealed class UIStarRating : MonoBehaviour
    {
        [Header("Star Images")]
        [SerializeField] private Image[] stars =
            new Image[3];

        [Header("Star Sprites")]
        [SerializeField] private Sprite filledStarSprite;
        [SerializeField] private Sprite emptyStarSprite;

        [Header("Colors")]
        [SerializeField] private Color filledColor =
            new Color32(255, 196, 61, 255);

        [SerializeField] private Color emptyColor =
            new Color32(100, 119, 150, 255);

        [Header("Editor Preview")]
        [SerializeField, Range(0, 3)]
        private int previewStars = 0;

        private int currentStars;

        public int CurrentStars => currentStars;

        private void Awake()
        {
            currentStars = previewStars;
            RefreshVisuals();
        }

        /// <summary>
        /// Displays 0, 1, 2 or 3 stars.
        /// Does not change the player's save data.
        /// </summary>
        public void SetStars(int amount)
        {
            currentStars = Mathf.Clamp(amount, 0, 3);
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (stars == null || stars.Length != 3)
                return;

            for (int i = 0; i < stars.Length; i++)
            {
                Image image = stars[i];

                if (image == null)
                    continue;

                bool isFilled = i < currentStars;

                image.sprite = isFilled
                    ? filledStarSprite
                    : emptyStarSprite;

                image.color = isFilled
                    ? filledColor
                    : emptyColor;

                image.preserveAspect = true;
                image.raycastTarget = false;

                image.enabled = image.sprite != null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            currentStars = previewStars;
            RefreshVisuals();
        }
#endif
    }
}

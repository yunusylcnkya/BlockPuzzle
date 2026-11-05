
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.SaveSystem;
using BlockPuzzle.UI;

namespace BlockPuzzle.Adventure
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public sealed class AdventureLevelButton : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text numberText;

        // The object that contains the lock icon.
        // Kept under the old field name for Inspector compatibility.
        [SerializeField] private GameObject lockText;

        [Header("Star Rating")]
        [SerializeField] private UIStarRating starRating;

        [Header("Colors")]
        [SerializeField]
        private Color unlockedColor =
            new Color32(243, 183, 70, 255);

        [SerializeField]
        private Color completedColor =
            new Color32(73, 183, 143, 255);

        [SerializeField]
        private Color lockedColor =
            new Color32(41, 70, 111, 255);

        private Button button;
        private Image background;

        private int levelId;
        private Action<int> onLevelSelected;

        private void Awake()
        {
            button = GetComponent<Button>();
            background = GetComponent<Image>();
        }

        public void Initialize(
            AdventureLevelDefinition definition,
            bool unlocked,
            Action<int> selectionCallback)
        {
            if (definition == null)
            {
                Debug.LogWarning(
                    "[AdventureLevelButton] Definition is null."
                );
                return;
            }

            levelId = definition.LevelId;
            onLevelSelected = selectionCallback;

            if (button == null)
                button = GetComponent<Button>();

            if (background == null)
                background = GetComponent<Image>();

            AdventureLevelRecord record =
                AdventureSaveService.GetLevelRecord(levelId);

            bool completed = record.completed;
            bool isLocked = !unlocked;

            // LEVEL NUMBER
            if (numberText != null)
            {
                numberText.text = levelId.ToString();
                numberText.gameObject.SetActive(unlocked);
            }

            // LOCK ICON
            if (lockText != null)
            {
                lockText.SetActive(isLocked);
            }

            // STAR RATING
            if (starRating != null)
            {
                int bestStars = completed
                    ? Mathf.Clamp(record.bestStars, 0, 3)
                    : 0;

                starRating.SetStars(bestStars);

                // Locked levels do not display stars.
                starRating.gameObject.SetActive(unlocked);
            }
            else
            {
                Debug.LogWarning(
                    $"[AdventureLevelButton] " +
                    $"StarRating missing on Level {levelId}."
                );
            }

            // BACKGROUND COLOR
            if (background != null)
            {
                background.color = isLocked
                    ? lockedColor
                    : completed
                        ? completedColor
                        : unlockedColor;
            }

            // BUTTON STATE
            button.interactable = unlocked;

            // Prevent duplicate listeners during refreshes.
            button.onClick.RemoveListener(HandleClick);

            if (unlocked)
            {
                button.onClick.AddListener(HandleClick);
            }
        }

        private void HandleClick()
        {
            onLevelSelected?.Invoke(levelId);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }
    }
}

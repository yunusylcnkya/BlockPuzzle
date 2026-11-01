
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.SaveSystem;

namespace BlockPuzzle.Adventure
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public sealed class AdventureLevelButton : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text numberText;
        [SerializeField] private GameObject lockText;
        [SerializeField] private TMP_Text starsText;

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
                return;

            levelId = definition.LevelId;
            onLevelSelected = selectionCallback;

            if (button == null)
                button = GetComponent<Button>();

            if (background == null)
                background = GetComponent<Image>();

            AdventureLevelRecord record =
                AdventureSaveService.GetLevelRecord(levelId);

            bool completed = record.completed;

            if (numberText != null)
            {
                numberText.text = levelId.ToString();
                numberText.gameObject.SetActive(unlocked);
            }

            if (lockText != null)
                lockText.SetActive(!unlocked);

            if (starsText != null)
            {
                starsText.gameObject.SetActive(completed);

                if (completed)
                {
                    int stars = Mathf.Clamp(
                        record.bestStars, 0, 3
                    );

                    starsText.text =
                        new string('★', stars) +
                        new string('☆', 3 - stars);
                }
            }

            background.color = !unlocked
                ? lockedColor
                : completed
                    ? completedColor
                    : unlockedColor;

            button.interactable = unlocked;

            button.onClick.RemoveAllListeners();

            if (unlocked)
            {
                button.onClick.AddListener(HandleClick);
            }
        }

        private void HandleClick()
        {
            onLevelSelected?.Invoke(levelId);
        }
    }
}

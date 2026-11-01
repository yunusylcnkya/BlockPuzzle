
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockPuzzle.Adventure
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public sealed class AdventureLevelButton : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text numberText;
        [SerializeField] private GameObject lockText;

        [Header("Colors")]
        [SerializeField]
        private Color unlockedColor =
            new Color32(243, 183, 70, 255);

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

            if (numberText != null)
            {
                numberText.text = levelId.ToString();
                numberText.gameObject.SetActive(unlocked);
            }

            if (lockText != null)
                lockText.SetActive(!unlocked);

            background.color = unlocked
                ? unlockedColor
                : lockedColor;

            button.interactable = unlocked;

            button.onClick.RemoveAllListeners();

            if (unlocked)
                button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            onLevelSelected?.Invoke(levelId);
        }
    }
}

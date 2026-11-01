
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockPuzzle.Adventure
{
    public sealed class AdventureMapController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField]
        private AdventureLevelCatalog catalog;

        [Header("UI")]
        [SerializeField]
        private RectTransform contentRoot;

        [SerializeField]
        private AdventureLevelButton levelButtonPrefab;

        [SerializeField]
        private TMP_Text progressText;

        private readonly List<AdventureLevelButton> buttons = new();

        private void Start()
        {
            BuildMap();
        }

        public void BuildMap()
        {
            if (catalog == null ||
                contentRoot == null ||
                levelButtonPrefab == null)
            {
                Debug.LogError(
                    "[AdventureMap] Missing Inspector references."
                );

                return;
            }

            foreach (AdventureLevelButton button in buttons)
            {
                if (button != null)
                {
                    button.gameObject.SetActive(false);
                    Destroy(button.gameObject);
                }
            }

            buttons.Clear();

            int highestUnlocked =
                AdventureProgress.HighestUnlockedLevel;

            if (progressText != null)
            {
                progressText.text =
                    $"LEVEL {highestUnlocked} UNLOCKED";
            }

            // Explicit ordering, independent of asset list order.
            List<AdventureLevelDefinition> sortedLevels = new();

            foreach (AdventureLevelDefinition definition
                     in catalog.Levels)
            {
                if (definition != null)
                    sortedLevels.Add(definition);
            }

            sortedLevels.Sort(
                (a, b) => a.LevelId.CompareTo(b.LevelId)
            );

            HashSet<int> usedIds = new();

            foreach (AdventureLevelDefinition definition
                     in sortedLevels)
            {
                if (!usedIds.Add(definition.LevelId))
                {
                    Debug.LogWarning(
                        $"[AdventureMap] Duplicate Level Id: " +
                        definition.LevelId
                    );

                    continue;
                }

                bool unlocked =
                    AdventureProgress.IsUnlocked(
                        definition.LevelId
                    );

                AdventureLevelButton button = Instantiate(
                    levelButtonPrefab,
                    contentRoot
                );

                button.name =
                    $"LevelButton_{definition.LevelId:000}";

                button.Initialize(
                    definition,
                    unlocked,
                    SelectLevel
                );

                buttons.Add(button);
            }

            Debug.Log(
                $"[AdventureMap] Created {buttons.Count} buttons."
            );
        }

        private void SelectLevel(int levelId)
        {
            if (!AdventureProgress.IsUnlocked(levelId))
                return;

            if (catalog == null ||
                !catalog.ContainsLevel(levelId))
            {
                Debug.LogWarning(
                    $"[AdventureMap] Level {levelId} not found."
                );

                return;
            }

            AdventureSession.SelectLevel(levelId);

            if (!Application.CanStreamedLevelBeLoaded(
                "AdventureGame"))
            {
                Debug.LogError(
                    "[AdventureMap] AdventureGame scene missing."
                );

                return;
            }

            SceneManager.LoadScene("AdventureGame");
        }

        public void BackToMainMenu()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}

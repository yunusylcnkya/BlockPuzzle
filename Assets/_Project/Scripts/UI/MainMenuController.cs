
using UnityEngine;
using BlockPuzzle.Core;

namespace BlockPuzzle.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject settingsPanel;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.MainMenu);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        public void PlayClassic()
        {
            Debug.Log("[MainMenu] Classic selected.");

            LoadGameScene("ClassicGame");
        }

        public void PlayAdventure()
        {
            Debug.Log("[MainMenu] Adventure selected.");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadScene("AdventureMap");
            }
        }

        public void OpenSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
        }

        public void CloseSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        private void LoadGameScene(string sceneName)
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[MainMenu] GameManager missing.");
                return;
            }

            GameManager.Instance.LoadScene(sceneName);
        }
    }
}

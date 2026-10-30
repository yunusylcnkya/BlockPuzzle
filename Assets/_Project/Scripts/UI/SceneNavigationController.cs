
using UnityEngine;
using BlockPuzzle.Core;

namespace BlockPuzzle.UI
{
    public sealed class SceneNavigationController : MonoBehaviour
    {
        [SerializeField] private bool isGameplayScene;

        private void Start()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[Navigation] GameManager missing.");
                return;
            }

            if (isGameplayScene)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }
        }

        public void BackToMenu()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[Navigation] GameManager missing.");
                return;
            }

            GameManager.Instance.LoadScene("MainMenu");
        }
    }
}


using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockPuzzle.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        private IEnumerator Start()
        {
            InitializeGame();

            yield return null;

            SceneManager.LoadScene("MainMenu");
        }

        private void InitializeGame()
        {
            if (GameManager.Instance == null)
            {
                GameObject managerObject = new GameObject("GameManager");

                managerObject.AddComponent<GameManager>();
            }

            Debug.Log("[Bootstrap] Game initialized.");
        }
    }
}

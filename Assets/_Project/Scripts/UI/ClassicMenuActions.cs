using UnityEngine;
using UnityEngine.SceneManagement;
using BlockPuzzle.Core;
using BlockPuzzle.SaveSystem;

namespace BlockPuzzle.UI
{
    /// <summary>
    /// Optional MainMenu component for an explicit NEW GAME button.
    /// The existing MainMenuController.PlayClassic() automatically resumes
    /// because ClassicRunController runs on entering ClassicGame.
    /// </summary>
    public sealed class ClassicMenuActions : MonoBehaviour
    {
        public void StartNewClassic()
        {
            ClassicRunSaveService.CloseRun();
            OpenClassic();
        }

        public void ContinueClassic()
        {
            OpenClassic();
        }

        private static void OpenClassic()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.LoadScene("ClassicGame");
            else
                SceneManager.LoadScene("ClassicGame");
        }
    }
}

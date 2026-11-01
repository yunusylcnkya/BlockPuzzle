
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using BlockPuzzle.Blocks;
using BlockPuzzle.Core;
using BlockPuzzle.Gameplay;
using BlockPuzzle.Grid;

namespace BlockPuzzle.Classic
{
    public sealed class ClassicGameController : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private BlockSpawner blockSpawner;
        [SerializeField] private ScoreManager scoreManager;

        [Header("Game Over UI")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text bestScoreText;

        [Header("Feedback")]
        [SerializeField] private GameFeedbackController feedbackController;


        private bool isGameOver;
        private Coroutine pendingCheck;

        private void Awake()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (blockSpawner != null)
                blockSpawner.TrayChanged += HandleTrayChanged;
        }

        private void Start()
        {
            if (gridManager == null ||
                blockSpawner == null ||
                scoreManager == null ||
                gameOverPanel == null)
            {
                Debug.LogError(
                    "[ClassicGame] Missing Inspector references."
                );

                enabled = false;
                return;
            }

            isGameOver = false;

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Playing);

            RequestMoveCheck();
        }

        private void OnDisable()
        {
            if (blockSpawner != null)
                blockSpawner.TrayChanged -= HandleTrayChanged;

            if (pendingCheck != null)
            {
                StopCoroutine(pendingCheck);
                pendingCheck = null;
            }
        }

        private void HandleTrayChanged()
        {
            RequestMoveCheck();
        }

        private void RequestMoveCheck()
        {
            if (!isActiveAndEnabled || isGameOver)
                return;

            if (pendingCheck != null)
                StopCoroutine(pendingCheck);

            pendingCheck = StartCoroutine(
                CheckMovesNextFrame()
            );
        }

        private IEnumerator CheckMovesNextFrame()
        {
            // GridManager and BlockSpawner may initialize
            // in a different Start() order.
            yield return null;

            pendingCheck = null;

            if (isGameOver ||
                gridManager == null ||
                blockSpawner == null)
            {
                yield break;
            }

            if (gridManager.GetCell(0, 0) == null)
            {
                Debug.LogWarning(
                    "[ClassicGame] Grid not initialized."
                );

                yield break;
            }

            List<BlockDefinition> remaining = new();

            foreach (BlockView block in blockSpawner.ActiveBlocks)
            {
                if (block != null &&
                    block.Definition != null)
                {
                    remaining.Add(block.Definition);
                }
            }

            // Do not declare Game Over for an empty tray
            // while it is being rebuilt.
            if (remaining.Count == 0)
            {
                Debug.LogWarning(
                    "[ClassicGame] No blocks to check."
                );

                yield break;
            }

            bool hasMove =
                gridManager.HasAnyValidPlacement(remaining);

            Debug.Log(
                $"[ClassicGame] Valid move exists: {hasMove}"
            );

            if (!hasMove)
                TriggerGameOver();
        }

        private void TriggerGameOver()
        {
            if (isGameOver)
                return;

            isGameOver = true;

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.GameOver);

            if (finalScoreText != null)
            {
                finalScoreText.text =
                    scoreManager.CurrentScore.ToString("N0");
            }

            if (bestScoreText != null)
            {
                bestScoreText.text =
                    $"BEST {scoreManager.BestScore:N0}";
            }

            gameOverPanel.SetActive(true);

            if (feedbackController != null)
            {
                feedbackController.PlayGameOver();
            }
            Debug.Log(
                $"[ClassicGame] GAME OVER. Score={scoreManager.CurrentScore}"
            );
        }

        public void RestartGame()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadScene(
                    "ClassicGame"
                );
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager
                    .LoadScene("ClassicGame");
            }
        }

        public void ReturnToMainMenu()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadScene(
                    "MainMenu"
                );
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager
                    .LoadScene("MainMenu");
            }
        }

        [ContextMenu("DEBUG/Check Available Moves")]
        private void DebugCheckAvailableMoves()
        {
            if (!Application.isPlaying)
                return;

            RequestMoveCheck();
        }
    }
}

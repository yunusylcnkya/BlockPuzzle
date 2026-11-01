
using UnityEngine;
using BlockPuzzle.Grid;

namespace BlockPuzzle.Gameplay
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class GameFeedbackController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager gridManager;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip placeClip;
        [SerializeField] private AudioClip lineClearClip;
        [SerializeField] private AudioClip multiLineClip;
        [SerializeField] private AudioClip comboClip;
        [SerializeField] private AudioClip gameOverClip;

        private AudioSource audioSource;

        private bool gameEnded;
        private int consecutiveClearMoves;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            if (gridManager != null)
                gridManager.BlockPlaced += HandleBlockPlaced;
        }

        private void OnDisable()
        {
            if (gridManager != null)
                gridManager.BlockPlaced -= HandleBlockPlaced;
        }


        private void HandleBlockPlaced(BlockPlacementResult result)
        {
            if (gameEnded || result == null)
                return;

            int clearedLines =
                result.LineClear?.TotalLines ?? 0;

            if (clearedLines == 0)
            {
                consecutiveClearMoves = 0;
                PlayClip(placeClip);
            }
            else
            {
                consecutiveClearMoves++;

                if (clearedLines == 1)
                    PlayClip(lineClearClip);
                else
                    PlayClip(multiLineClip);

                if (consecutiveClearMoves >= 2)
                    PlayCombo();
            }

            TryVibrate();
        }


        public void PlayCombo()
        {
            if (gameEnded)
                return;

            PlayClip(comboClip);
        }

        public void PlayGameOver()
        {
            if (gameEnded)
                return;

            gameEnded = true;

            PlayClip(gameOverClip);
            TryVibrate();
        }

        private void PlayClip(AudioClip clip)
        {
            if (!GameFeedbackSettings.SoundEnabled)
                return;

            if (clip == null || audioSource == null)
                return;

            float volume =
                GameFeedbackSettings.SfxVolume;

            if (volume <= 0f)
                return;

            audioSource.PlayOneShot(
                clip,
                volume
            );
        }

        private void TryVibrate()
        {
            if (!GameFeedbackSettings.HapticsEnabled)
                return;

#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}

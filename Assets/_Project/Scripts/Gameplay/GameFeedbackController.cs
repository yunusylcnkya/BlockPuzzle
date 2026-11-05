
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

        [Header("SFX Balance")]
        [SerializeField, Range(0f, 1f)]
        private float placeVolume = 0.40f;

        [SerializeField, Range(0f, 1f)]
        private float lineClearVolume = 0.65f;

        [SerializeField, Range(0f, 1f)]
        private float multiLineVolume = 0.75f;

        [SerializeField, Range(0f, 1f)]
        private float comboVolume = 0.55f;

        [SerializeField, Range(0f, 1f)]
        private float gameOverVolume = 0.65f;

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

            float balance = 1f;

            if (clip == placeClip)
                balance = placeVolume;
            else if (clip == lineClearClip)
                balance = lineClearVolume;
            else if (clip == multiLineClip)
                balance = multiLineVolume;
            else if (clip == comboClip)
                balance = comboVolume;
            else if (clip == gameOverClip)
                balance = gameOverVolume;

            float finalVolume =
                GameFeedbackSettings.SfxVolume * balance;

            if (finalVolume <= 0f)
                return;

            audioSource.PlayOneShot(clip, finalVolume);
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

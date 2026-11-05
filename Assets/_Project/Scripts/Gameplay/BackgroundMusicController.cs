
using UnityEngine;

namespace BlockPuzzle.Gameplay
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class BackgroundMusicController : MonoBehaviour
    {
        public static BackgroundMusicController Instance
        {
            get;
            private set;
        }

        private const string MusicEnabledKey =
            "BlockPuzzle.Settings.MusicEnabled";

        private const string MusicVolumeKey =
            "BlockPuzzle.Settings.MusicVolume";

        [Header("Music")]
        [SerializeField] private AudioClip backgroundMusic;

        [SerializeField, Range(0f, 1f)]
        private float defaultVolume = 0.35f;

        private AudioSource source;

        public static bool MusicEnabled
        {
            get => PlayerPrefs.GetInt(
                MusicEnabledKey, 1) == 1;

            set
            {
                PlayerPrefs.SetInt(
                    MusicEnabledKey,
                    value ? 1 : 0
                );

                PlayerPrefs.Save();

                if (Instance != null)
                    Instance.RefreshSettings();
            }
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(
                MusicVolumeKey, 0.35f);

            set
            {
                PlayerPrefs.SetFloat(
                    MusicVolumeKey,
                    Mathf.Clamp01(value)
                );

                PlayerPrefs.Save();

                if (Instance != null)
                    Instance.RefreshSettings();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            source = GetComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.clip = backgroundMusic;

            if (!PlayerPrefs.HasKey(MusicVolumeKey))
                MusicVolume = defaultVolume;

            RefreshSettings();
        }

        public void RefreshSettings()
        {
            if (source == null)
                return;

            source.volume = MusicVolume;

            if (!MusicEnabled || source.clip == null)
            {
                source.Pause();
                return;
            }

            if (!source.isPlaying)
            {
                source.UnPause();

                if (!source.isPlaying)
                    source.Play();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (source == null)
                return;

            if (paused)
                source.Pause();
            else
                RefreshSettings();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}

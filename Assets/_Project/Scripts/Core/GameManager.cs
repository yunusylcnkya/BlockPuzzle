
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockPuzzle.Core
{
    public enum GameState
    {
        Boot,
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; }
            = GameState.Boot;

        [Header("Adaptive FPS")]
        [SerializeField]
        private bool enable120FPS = true;

        [SerializeField]
        private bool automaticPerformanceFallback = true;

        [SerializeField, Range(8f, 30f)]
        private float performanceCheckSeconds = 12f;

        [SerializeField, Range(70, 115)]
        private int minimumStableFPS = 96;

        private int currentTargetFPS;
        private float detectedRefreshRate;

        private float sampleTime;
        private int sampleFrames;
        private float ignoreSamplesUntil;
        private float nextRefreshCheck;

        private bool fpsInitialized;

        public int CurrentTargetFPS => currentTargetFPS;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            ConfigureFrameRate();

            Debug.Log("[GameManager] Initialized.");
        }

        private void ConfigureFrameRate()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 120;
            detectedRefreshRate = ReadRefreshRate();

            int selectedFPS = 120;

            // Mobile uses targetFrameRate instead of vSyncCount.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = selectedFPS;

            currentTargetFPS = selectedFPS;
            fpsInitialized = true;

            ResetPerformanceSamples();

            // Ignore startup and scene initialization spikes.
            ignoreSamplesUntil = Time.unscaledTime + 8f;
            nextRefreshCheck = Time.unscaledTime + 5f;

            Debug.Log(
                $"[FPS] Display: {detectedRefreshRate:F2} Hz | " +
                $"Target: {currentTargetFPS} FPS"
            );
        }

        private float ReadRefreshRate()
        {
            float refreshRate =
                (float)Screen.currentResolution
                    .refreshRateRatio.value;

            if (refreshRate <= 0f)
                return 60f;

            return refreshRate;
        }

        private int SelectFrameRate(float refreshRate)
        {
            // Common 120 Hz displays.
            if (refreshRate >= 118f &&
                refreshRate <= 122f)
            {
                return enable120FPS ? 120 : 60;
            }

            // A 90 Hz display works best at 90 rather
            // than requesting 60 and potentially getting 45.
            if (refreshRate >= 88f &&
                refreshRate <= 92f)
            {
                return 90;
            }

            // Standard 60 Hz displays.
            if (refreshRate >= 58f &&
                refreshRate <= 62f)
            {
                return 60;
            }

            // High-refresh displays, such as 144 Hz:
            // prefer approximately half the refresh rate.
            if (refreshRate > 122f)
            {
                return Mathf.CeilToInt(
                    refreshRate / 2f
                );
            }

            // Other supported display modes.
            return Mathf.Clamp(
                Mathf.RoundToInt(refreshRate),
                60,
                120
            );
        }

        private void Update()
        {
            if (!fpsInitialized)
                return;

            CheckDisplayRefreshRate();
            MonitorPerformance();
        }

        private void CheckDisplayRefreshRate()
        {
            if (Time.unscaledTime < nextRefreshCheck)
                return;

            nextRefreshCheck = Time.unscaledTime + 5f;

            float refreshRate = ReadRefreshRate();

            if (Mathf.Abs(
                refreshRate - detectedRefreshRate) >= 2f)
            {
                Debug.Log(
                    "[FPS] Display refresh rate changed."
                );

                ConfigureFrameRate();
            }
        }

        private void MonitorPerformance()
        {
            if (!automaticPerformanceFallback ||
                currentTargetFPS != 120)
            {
                return;
            }

            if (Time.unscaledTime < ignoreSamplesUntil)
                return;

            float deltaTime = Time.unscaledDeltaTime;

            // Ignore an isolated long interruption,
            // such as returning from an external screen.
            if (deltaTime <= 0f || deltaTime > 0.5f)
            {
                ResetPerformanceSamples();
                return;
            }

            sampleTime += deltaTime;
            sampleFrames++;

            if (sampleTime < performanceCheckSeconds)
                return;

            float averageFPS = sampleFrames / sampleTime;

            Debug.Log(
                $"[FPS] Average: {averageFPS:F1} | " +
                $"Target : {currentTargetFPS}"
            );

            if (averageFPS < minimumStableFPS)
            {
                currentTargetFPS = 120;
                Application.targetFrameRate = 120;

                Debug.Log(
                    "[FPS] Performance fallback: " +
                    $"120 -> 60 FPS. " +
                    $"Measured average: {averageFPS:F1}"
                );
            }

            ResetPerformanceSamples();
        }

        private void ResetPerformanceSamples()
        {
            sampleTime = 0f;
            sampleFrames = 0;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                ResetPerformanceSamples();
                return;
            }

            if (fpsInitialized)
                ConfigureFrameRate();
        }

        public void SetState(GameState newState)
        {
            if (CurrentState == newState)
                return;

            CurrentState = newState;

            Debug.Log(
                $"[GameManager] State: {CurrentState}"
            );
        }

        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError(
                    "[GameManager] Scene name is empty."
                );
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(
                sceneName))
            {
                Debug.LogError(
                    $"[GameManager] Scene not found: {sceneName}"
                );
                return;
            }

            Debug.Log(
                $"[GameManager] Loading: {sceneName}"
            );

            SceneManager.LoadScene(sceneName);
        }
    }
}

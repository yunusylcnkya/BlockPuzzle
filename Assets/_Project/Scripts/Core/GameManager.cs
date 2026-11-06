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

    /// <summary>
    /// Keeps the existing game state/scene API and manages Android frame pacing.
    /// Android first gets a short 120 FPS request so adaptive displays can switch
    /// from their 30/60 Hz startup mode. We then keep 120 only if 120 Hz was
    /// observed, otherwise use 60. During gameplay we can fall back to 60.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Boot;

        [Header("Adaptive FPS")]
        [SerializeField] private bool enable120FPS = true;
        [SerializeField] private bool automaticPerformanceFallback = true;

        [Header("Display Detection")]
        [SerializeField, Range(4f, 10f)]
        private float refreshProbeSeconds = 5f;

        [Header("Performance Monitoring")]
        [SerializeField, Range(8f, 30f)]
        private float performanceCheckSeconds = 12f;

        [SerializeField, Range(70, 115)]
        private int minimumStableFPS = 96;

        [SerializeField, Range(1, 3)]
        private int consecutiveLowSampleWindows = 2;

        private const float ProbeReadIntervalSeconds = 0.5f;
        private const float SceneWarmupSeconds = 4f;
        private const float PauseSpikeThresholdSeconds = 0.5f;

        private int currentTargetFPS;
        private float highestObservedRefreshRate;
        private bool probingRefreshRate;
        private float probeEndTime;
        private float nextProbeReadTime;
        private float ignorePerformanceUntil;
        private float sampleTime;
        private int sampleFrames;
        private int consecutiveLowSamples;
        private bool wasPaused;

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
            SceneManager.sceneLoaded += HandleSceneLoaded;

            BeginFrameRateSelection();
            Debug.Log("[GameManager] Initialized.");
        }

        private void BeginFrameRateSelection()
        {
            // Android ignores vSyncCount and uses targetFrameRate.
            // Setting it to 0 also makes Editor FPS tests easier to interpret.
            QualitySettings.vSyncCount = 0;

            ResetPerformanceSamples();
            consecutiveLowSamples = 0;
            ignorePerformanceUntil = Time.unscaledTime + SceneWarmupSeconds;
            probingRefreshRate = false;

            // Keep Editor and non-Android behavior predictable.
            if (Application.platform != RuntimePlatform.Android || !enable120FPS)
            {
                ApplyTargetFrameRate(60, "Standard mode");
                return;
            }

            // A 120 Hz Android phone can report only 30/60 Hz at startup.
            // Requesting 120 lets the system choose its higher refresh mode.
            highestObservedRefreshRate = ReadActiveRefreshRate();
            ApplyTargetFrameRate(120, "High-refresh probe started");

            probingRefreshRate = true;
            probeEndTime = Time.unscaledTime + refreshProbeSeconds;
            nextProbeReadTime = Time.unscaledTime;

            Debug.Log(
                $"[FPS] Probe: starting display {highestObservedRefreshRate:F2} Hz; " +
                $"checking high refresh for {refreshProbeSeconds:F0} seconds."
            );
        }

        private static float ReadActiveRefreshRate()
        {
            double rate = Screen.currentResolution.refreshRateRatio.value;
            return rate > 0.0 ? (float)rate : 0f;
        }

        private void Update()
        {
            if (probingRefreshRate)
            {
                UpdateRefreshProbe();
                return;
            }

            MonitorGamePerformance();
        }

        private void UpdateRefreshProbe()
        {
            float now = Time.unscaledTime;

            if (now >= nextProbeReadTime)
            {
                float currentRefresh = ReadActiveRefreshRate();
                highestObservedRefreshRate = Mathf.Max(
                    highestObservedRefreshRate, currentRefresh);

                nextProbeReadTime = now + ProbeReadIntervalSeconds;
            }

            if (now < probeEndTime)
                return;

            probingRefreshRate = false;

            // We saw the panel switch to high refresh in this app session.
            // 115 Hz accommodates 119.88/120 Hz reporting variations.
            int chosenFPS = highestObservedRefreshRate >= 115f ? 120 : 60;

            ApplyTargetFrameRate(chosenFPS, "Display probe finished");
            ResetPerformanceSamples();
            ignorePerformanceUntil = now + SceneWarmupSeconds;

            Debug.Log(
                $"[FPS] Probe result: peak display {highestObservedRefreshRate:F2} Hz " +
                $"| Target: {currentTargetFPS} FPS"
            );
        }

        private void MonitorGamePerformance()
        {
            if (!automaticPerformanceFallback ||
                currentTargetFPS != 120 ||
                CurrentState != GameState.Playing ||
                Time.unscaledTime < ignorePerformanceUntil)
            {
                // Menu screens and scene-loading periods should not trigger
                // a permanent downgrade of gameplay performance.
                ResetPerformanceSamples();
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;

            // Ignore a large one-off stall (pause, app switch, ad transition).
            if (deltaTime <= 0f || deltaTime > PauseSpikeThresholdSeconds)
            {
                ResetPerformanceSamples();
                ignorePerformanceUntil = Time.unscaledTime + 2f;
                return;
            }

            sampleTime += deltaTime;
            sampleFrames++;

            if (sampleTime < performanceCheckSeconds)
                return;

            float averageFPS = sampleFrames / sampleTime;

            Debug.Log(
                $"[FPS] Average: {averageFPS:F1} | Target: {currentTargetFPS}"
            );

            if (averageFPS < minimumStableFPS)
            {
                consecutiveLowSamples++;
            }
            else
            {
                consecutiveLowSamples = 0;
            }

            if (consecutiveLowSamples >= consecutiveLowSampleWindows)
            {
                ApplyTargetFrameRate(60,
                    $"Performance fallback; observed {averageFPS:F1} FPS");

                Debug.Log(
                    $"[FPS] Performance fallback: 120 -> 60 FPS " +
                    $"after {consecutiveLowSamples} low measurement windows."
                );

                consecutiveLowSamples = 0;
            }

            ResetPerformanceSamples();
        }

        private void ApplyTargetFrameRate(int target, string reason)
        {
            currentTargetFPS = target;
            Application.targetFrameRate = target;

            Debug.Log(
                $"[FPS] Display: {ReadActiveRefreshRate():F2} Hz | " +
                $"Target: {currentTargetFPS} FPS | {reason}"
            );
        }

        private void ResetPerformanceSamples()
        {
            sampleTime = 0f;
            sampleFrames = 0;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResetPerformanceSamples();
            consecutiveLowSamples = 0;
            ignorePerformanceUntil = Time.unscaledTime + SceneWarmupSeconds;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                wasPaused = true;
                ResetPerformanceSamples();
                return;
            }

            // Unity may send OnApplicationPause(false) at launch; don't
            // restart the initial probe unless the app was actually paused.
            if (wasPaused)
            {
                wasPaused = false;
                BeginFrameRateSelection();
            }
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }

        public void SetState(GameState newState)
        {
            if (CurrentState == newState)
                return;

            CurrentState = newState;
            ResetPerformanceSamples();
            consecutiveLowSamples = 0;
            ignorePerformanceUntil = Time.unscaledTime + SceneWarmupSeconds;

            Debug.Log($"[GameManager] State: {CurrentState}");
        }

        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[GameManager] Scene name is empty.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("[GameManager] Scene not found: " + sceneName);
                return;
            }

            Debug.Log("[GameManager] Loading: " + sceneName);
            SceneManager.LoadScene(sceneName);
        }
    }
}

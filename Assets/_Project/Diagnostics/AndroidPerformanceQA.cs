#if UNITY_ANDROID && DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using BlockPuzzle.Core;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace BlockPuzzle.Diagnostics
{
    /// <summary>
    /// Development Build only: collect a small, allocation-free frame-time sample
    /// on the actual Android device. Not compiled into a release player.
    /// No Inspector bindings and no existing gameplay class changes are needed.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class AndroidPerformanceQA : MonoBehaviour
    {
        private const float WarmupSeconds = 5f;
        private const float WindowSeconds = 60f;
        private const float UnexpectedPauseSeconds = 0.5f;
        private const int MaxFrames = 12000;
        private static AndroidPerformanceQA instance;

        private readonly float[] frameSeconds = new float[MaxFrames];
        private int frameCount;
        private int startingTargetFPS;
        private int startingGc0;
        private long startingAllocatedBytes;
        private float warmupEndTime;
        private float captureStartedAt;
        private bool recording;
        private bool paused;
        private string sceneName;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (instance != null)
                return;

            GameObject gameObject = new GameObject("[QA] Android Performance");
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<AndroidPerformanceQA>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            sceneName = SceneManager.GetActiveScene().name;
            SceneManager.activeSceneChanged += OnSceneChanged;
            BeginWarmup();
            Debug.Log("[QA-PERF] Android Development Build profiler installed. " +
                      "Samples start after a 5-second scene warmup.");
        }

        private void OnSceneChanged(Scene previous, Scene current)
        {
            sceneName = current.name;
            BeginWarmup();
        }

        private void OnApplicationPause(bool isPaused)
        {
            paused = isPaused;
            if (!paused)
                BeginWarmup();
        }

        private void BeginWarmup()
        {
            recording = false;
            frameCount = 0;
            warmupEndTime = Time.realtimeSinceStartup + WarmupSeconds;
        }

        private static int GetTargetFPS()
        {
            return GameManager.Instance != null
                ? GameManager.Instance.CurrentTargetFPS
                : Application.targetFrameRate;
        }

        private void Update()
        {
            if (paused)
                return;

            float now = Time.realtimeSinceStartup;
            if (now < warmupEndTime)
                return;

            int currentTarget = GetTargetFPS();
            if (!recording)
            {
                recording = true;
                captureStartedAt = now;
                startingTargetFPS = currentTarget;
                startingGc0 = GC.CollectionCount(0);
                startingAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong();
                return;
            }

            // A scene/game-state switch or FPS fallback invalidates a mixed sample.
            if (currentTarget != startingTargetFPS)
            {
                Debug.Log("[QA-PERF] Target FPS changed; starting a fresh window.");
                BeginWarmup();
                return;
            }

            float delta = Time.unscaledDeltaTime;
            if (delta <= 0f || delta > UnexpectedPauseSeconds)
            {
                // App switching, debugger attach, native ads or other long stalls.
                // These events should be investigated separately in Logcat.
                Debug.Log("[QA-PERF] Long stall/pause detected; window reset.");
                BeginWarmup();
                return;
            }

            if (frameCount < frameSeconds.Length)
                frameSeconds[frameCount++] = delta;

            float elapsed = now - captureStartedAt;
            if (elapsed >= WindowSeconds || frameCount == frameSeconds.Length)
            {
                Report(elapsed);
                BeginWarmup();
            }
        }

        private void Report(float elapsedSeconds)
        {
            if (frameCount < 2 || elapsedSeconds <= 0f)
                return;

            int overThirtyThreeMs = 0;
            for (int i = 0; i < frameCount; i++)
            {
                if (frameSeconds[i] > 0.033333f)
                    overThirtyThreeMs++;
            }

            Array.Sort(frameSeconds, 0, frameCount);
            float p50 = PercentileMs(0.50f);
            float p95 = PercentileMs(0.95f);
            float p99 = PercentileMs(0.99f);
            float averageFPS = frameCount / elapsedSeconds;
            int gc0 = Math.Max(0, GC.CollectionCount(0) - startingGc0);
            long allocatedBytes = Profiler.GetTotalAllocatedMemoryLong();
            float allocatedMB = allocatedBytes / (1024f * 1024f);
            float allocatedDeltaMB = (allocatedBytes - startingAllocatedBytes) / (1024f * 1024f);
            double refreshHz = Screen.currentResolution.refreshRateRatio.value;
            float longFramePercent = 100f * overThirtyThreeMs / frameCount;

            // Unity-allocated memory is not the same as Android total PSS or managed heap.
            Debug.Log(
                $"[QA-PERF] scene={sceneName} target={startingTargetFPS} " +
                $"displayHz={refreshHz:0.0} duration={elapsedSeconds:0.0}s " +
                $"avgFPS={averageFPS:0.0} frameMs_p50={p50:0.00} " +
                $"frameMs_p95={p95:0.00} frameMs_p99={p99:0.00} " +
                $"framesOver33ms={overThirtyThreeMs} ({longFramePercent:0.00}%) " +
                $"GC0={gc0} unityAllocatedMB={allocatedMB:0.0} " +
                $"allocatedDeltaMB={allocatedDeltaMB:+0.0;-0.0;0.0}");
        }

        private float PercentileMs(float fraction)
        {
            int index = Mathf.Clamp(
                Mathf.CeilToInt(fraction * frameCount) - 1, 0, frameCount - 1);
            return frameSeconds[index] * 1000f;
        }

        private void OnDestroy()
        {
            if (instance != this)
                return;

            SceneManager.activeSceneChanged -= OnSceneChanged;
            instance = null;
        }
    }
}
#endif

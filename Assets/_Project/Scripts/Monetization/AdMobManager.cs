using System;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using BlockPuzzle.SaveSystem;
using UnityEngine;

namespace BlockPuzzle.Monetization
{
    /// <summary>
    /// Persistent AdMob controller. Consent, ad caching, retries and transitions
    /// are managed here. Existing scene-facing methods and events are retained.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AdMobManager : MonoBehaviour
    {
        public static AdMobManager Instance { get; private set; }

        private const string AndroidTestInterstitialId =
            "ca-app-pub-3940256099942544/1033173712";
        private const string AndroidTestRewardedId =
            "ca-app-pub-3940256099942544/5224354917";

        private const float CacheLifetimeSeconds = 3500f; // Refresh before 1 hour.
        private const float RetryFirstSeconds = 15f;
        private const float RetryMaximumSeconds = 120f;

        [Header("Ad Configuration")]
        [SerializeField] private bool useTestAds = true;
        [SerializeField] private string androidInterstitialId = "";

        [Header("Frequency Rules")]
        [SerializeField, Min(1)] private int transitionsPerAd = 3;
        [SerializeField, Min(0f)] private float minimumSecondsBetweenAds = 120f;

        [Header("Rewarded Ads")]
        [SerializeField] private string androidRewardedId = "";
        [SerializeField, Min(1)] private int rewardedCoinAmount = 50;

        private InterstitialAd interstitialAd;
        private RewardedAd rewardedAd;
        private bool sdkInitialized;
        private bool sdkInitializing;
        private bool loadingAd;
        private bool loadingRewarded;
        private bool showingAd;
        private bool showingRewarded;
        private bool rewardGrantedForCurrentAd;
        private bool privacyOptionsOpen;
        private bool applicationPaused;
        private bool? lastConsentAllowed;
        private bool clearAdsAfterFullScreen;
        private int adGeneration;
        private float interstitialLoadedAt;
        private float rewardedLoadedAt;
        private float nextInterstitialRetryTime;
        private float nextRewardedRetryTime;
        private int interstitialFailureCount;
        private int rewardedFailureCount;
        private float nextMaintenanceTime;
        private float nextConsentRetryTime;
        private int consentFailureCount;
        private bool consentRequestInProgress;
        private float nextSdkRetryTime;
        private int sdkFailureCount;
        private int transitionCount;
        private float lastAdTime = -100000f;
        private Action pendingContinuation;
        private Action deferredRewardedContinuation;

        public bool IsSdkInitialized => sdkInitialized;
        public bool UseTestAds => useTestAds;
        public bool IsPrivacyOptionsOpen => privacyOptionsOpen;
        // The banner uses this to invalidate an ad loaded with earlier privacy choices.
        public int ConsentRevision { get; private set; }
        public bool IsFullScreenAdShowing => showingAd || showingRewarded;
        public bool IsRewardedReady =>
            CanLoadAds() && !loadingRewarded && !IsFullScreenAdShowing &&
            rewardedAd != null && rewardedAd.CanShowAd() &&
            Time.realtimeSinceStartup - rewardedLoadedAt < CacheLifetimeSeconds;
        public int RewardedCoinAmount => rewardedCoinAmount;
        public bool CanShowPrivacyOptions =>
            ConsentInformation.PrivacyOptionsRequirementStatus ==
            PrivacyOptionsRequirementStatus.Required;

        public event Action RewardedStateChanged;
        public event Action<int> RewardedCoinsGranted;

        private bool CanLoadAds()
        {
            return sdkInitialized && !privacyOptionsOpen &&
                   ConsentInformation.CanRequestAds();
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
        }

        private void Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log("[AdMob] Android startup.");
            // Prior consent may already be usable, but ALWAYS refresh UMP at launch.
            if (ConsentInformation.CanRequestAds())
                TryInitializeAds();
            RequestConsent();
#else
            Debug.Log("[AdMob] Editor mode. Real ads require Android.");
#endif
        }

        private void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Time.realtimeSinceStartup < nextMaintenanceTime)
                return;
            nextMaintenanceTime = Time.realtimeSinceStartup + 1f;

            // A failed UMP update (for example offline on first launch) must
            // be retried even when CanRequestAds() is currently false.
            if (!applicationPaused && !privacyOptionsOpen &&
                !consentRequestInProgress && nextConsentRetryTime > 0f &&
                Time.realtimeSinceStartup >= nextConsentRetryTime)
            {
                nextConsentRetryTime = 0f;
                RequestConsent();
            }

            if (applicationPaused || privacyOptionsOpen ||
                !ConsentInformation.CanRequestAds())
                return;

            if (!sdkInitialized)
            {
                if (!sdkInitializing && Time.realtimeSinceStartup >= nextSdkRetryTime)
                    TryInitializeAds();
                return;
            }

            // Never use a stale preloaded full-screen ad.
            if (!showingAd && interstitialAd != null &&
                Time.realtimeSinceStartup - interstitialLoadedAt >= CacheLifetimeSeconds)
            {
                Debug.Log("[AdMob] Interstitial cache expired; refreshing.");
                DestroyInterstitial();
            }
            if (!showingRewarded && rewardedAd != null &&
                Time.realtimeSinceStartup - rewardedLoadedAt >= CacheLifetimeSeconds)
            {
                Debug.Log("[AdMob] Rewarded cache expired; refreshing.");
                DestroyRewarded();
                RewardedStateChanged?.Invoke();
            }

            if (!showingAd && !loadingAd && interstitialAd == null &&
                Time.realtimeSinceStartup >= nextInterstitialRetryTime)
                LoadInterstitial();

            if (!showingRewarded && !loadingRewarded && rewardedAd == null &&
                Time.realtimeSinceStartup >= nextRewardedRetryTime)
                LoadRewarded();
#endif
        }

        private void ScheduleConsentRetry()
        {
            float delay = RetryDelay(consentFailureCount++);
            nextConsentRetryTime = Time.realtimeSinceStartup + delay;
            Debug.LogWarning($"[AdMob] UMP retry in {delay:0}s.");
        }

        private void RequestConsent()
        {
            if (consentRequestInProgress || privacyOptionsOpen) return;
            consentRequestInProgress = true;
            Debug.Log("[AdMob] UMP consent update started.");
            try
            {
                ConsentInformation.Update(new ConsentRequestParameters(), error =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        if (this == null) return;
                        consentRequestInProgress = false;
                        if (error != null)
                        {
                            Debug.LogWarning("[AdMob] UMP update failed: " + error.Message);
                            ScheduleConsentRetry();
                            // UMP may retain an earlier, usable choice.
                            ReconcileConsent(false);
                            return;
                        }

                        consentFailureCount = 0;
                        nextConsentRetryTime = 0f;
                        ReconcileConsent(false);
                        ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                        {
                            MobileAdsEventExecutor.ExecuteInUpdate(() =>
                            {
                                if (this == null) return;
                                if (formError != null)
                                {
                                    Debug.LogWarning("[AdMob] UMP form: " + formError.Message);
                                    ScheduleConsentRetry();
                                }
                                // Form might have updated personalisation.
                                ReconcileConsent(formError == null);
                            });
                        });
                    });
                });
            }
            catch (Exception ex)
            {
                consentRequestInProgress = false;
                Debug.LogError("[AdMob] UMP update exception: " + ex.Message);
                ScheduleConsentRetry();
                ReconcileConsent(false);
            }
        }

        /// <summary>
        /// Invalidates cached ads on consent change. In-flight callbacks use
        /// adGeneration so stale responses cannot populate a new cache.
        /// </summary>
        private void ReconcileConsent(bool refreshLoadedAds)
        {
            bool permitted = ConsentInformation.CanRequestAds();
            bool changed = !lastConsentAllowed.HasValue ||
                           lastConsentAllowed.Value != permitted;
            lastConsentAllowed = permitted;
            Debug.Log("[AdMob] Consent permits requests: " + permitted);

            if (changed || refreshLoadedAds)
            {
                ConsentRevision++;
                adGeneration++;
                loadingAd = false;
                loadingRewarded = false;
                nextInterstitialRetryTime = 0f;
                nextRewardedRetryTime = 0f;
                interstitialFailureCount = 0;
                rewardedFailureCount = 0;
                if (IsFullScreenAdShowing)
                    clearAdsAfterFullScreen = true;
                else
                {
                    DestroyInterstitial();
                    DestroyRewarded();
                }
                RewardedStateChanged?.Invoke();
                Debug.Log("[AdMob] Cached ads invalidated. Consent revision " +
                          ConsentRevision);
            }

            if (!permitted)
            {
                if (!IsFullScreenAdShowing)
                {
                    DestroyInterstitial();
                    DestroyRewarded();
                    RewardedStateChanged?.Invoke();
                }
                return;
            }

            if (!sdkInitialized) TryInitializeAds();
            else if (!IsFullScreenAdShowing)
            {
                LoadInterstitial();
                LoadRewarded();
            }
        }

        private void TryInitializeAds()
        {
            if (sdkInitialized || sdkInitializing || privacyOptionsOpen ||
                Time.realtimeSinceStartup < nextSdkRetryTime ||
                !ConsentInformation.CanRequestAds())
                return;

            sdkInitializing = true;
            MobileAds.Initialize(status =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (this == null) return;
                    sdkInitializing = false;
                    if (status == null)
                    {
                        float delay = RetryDelay(sdkFailureCount++);
                        nextSdkRetryTime = Time.realtimeSinceStartup + delay;
                        Debug.LogError("[AdMob] SDK initialization returned null; " +
                                       $"retry in {delay:0}s.");
                        return;
                    }
                    sdkInitialized = true;
                    sdkFailureCount = 0;
                    nextSdkRetryTime = 0f;
                    Debug.Log("[AdMob] SDK Initialized: True");
                    LoadInterstitial();
                    LoadRewarded();
                });
            });
        }

        private static float RetryDelay(int failures)
        {
            return Mathf.Min(RetryMaximumSeconds,
                RetryFirstSeconds * Mathf.Pow(2f, Mathf.Min(3, failures)));
        }

        private void ScheduleInterstitialRetry()
        {
            float delay = RetryDelay(interstitialFailureCount++);
            nextInterstitialRetryTime = Time.realtimeSinceStartup + delay;
            Debug.LogWarning($"[AdMob] Interstitial retry in {delay:0}s.");
        }

        private void ScheduleRewardedRetry()
        {
            float delay = RetryDelay(rewardedFailureCount++);
            nextRewardedRetryTime = Time.realtimeSinceStartup + delay;
            Debug.LogWarning($"[AdMob Rewarded] Retry in {delay:0}s.");
        }

        private string GetInterstitialId() => useTestAds
            ? AndroidTestInterstitialId : (androidInterstitialId ?? "").Trim();

        private string GetRewardedId() => useTestAds
            ? AndroidTestRewardedId : (androidRewardedId ?? "").Trim();

        private void LoadInterstitial()
        {
            if (!CanLoadAds() || loadingAd || showingAd ||
                interstitialAd != null ||
                Time.realtimeSinceStartup < nextInterstitialRetryTime)
                return;

            string unitId = GetInterstitialId();
            if (string.IsNullOrEmpty(unitId)) return;

            loadingAd = true;
            int requestGeneration = adGeneration;
            try
            {
                InterstitialAd.Load(unitId, new AdRequest(), (ad, error) =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        if (this == null || requestGeneration != adGeneration)
                        {
                            ad?.Destroy();
                            return;
                        }
                        loadingAd = false;
                        if (!CanLoadAds() || error != null || ad == null)
                        {
                            ad?.Destroy();
                            if (CanLoadAds()) ScheduleInterstitialRetry();
                            Debug.LogWarning("[AdMob] Interstitial load failed: " + error);
                            return;
                        }

                        interstitialAd = ad;
                        interstitialLoadedAt = Time.realtimeSinceStartup;
                        interstitialFailureCount = 0;
                        nextInterstitialRetryTime = 0f;
                        RegisterInterstitialEvents(ad);
                        Debug.Log("[AdMob] Interstitial loaded.");
                    });
                });
            }
            catch (Exception ex)
            {
                loadingAd = false;
                Debug.LogError("[AdMob] Interstitial request exception: " + ex.Message);
                ScheduleInterstitialRetry();
            }
        }

        private void RegisterInterstitialEvents(InterstitialAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
                MobileAdsEventExecutor.ExecuteInUpdate(() => FinishAdAndContinue());
            ad.OnAdFullScreenContentFailed += error =>
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    Debug.LogWarning("[AdMob] Interstitial display failed: " + error);
                    FinishAdAndContinue();
                });
        }

        private void LoadRewarded()
        {
            if (!CanLoadAds() || loadingRewarded || showingRewarded ||
                rewardedAd != null ||
                Time.realtimeSinceStartup < nextRewardedRetryTime)
                return;

            string unitId = GetRewardedId();
            if (string.IsNullOrEmpty(unitId)) return;

            loadingRewarded = true;
            RewardedStateChanged?.Invoke();
            int requestGeneration = adGeneration;
            try
            {
                RewardedAd.Load(unitId, new AdRequest(), (ad, error) =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        if (this == null || requestGeneration != adGeneration)
                        {
                            ad?.Destroy();
                            return;
                        }
                        loadingRewarded = false;
                        if (!CanLoadAds() || error != null || ad == null)
                        {
                            ad?.Destroy();
                            if (CanLoadAds()) ScheduleRewardedRetry();
                            Debug.LogWarning("[AdMob Rewarded] Load failed: " + error);
                            RewardedStateChanged?.Invoke();
                            return;
                        }

                        rewardedAd = ad;
                        rewardedLoadedAt = Time.realtimeSinceStartup;
                        rewardedFailureCount = 0;
                        nextRewardedRetryTime = 0f;
                        RegisterRewardedEvents(ad);
                        Debug.Log("[AdMob Rewarded] Loaded successfully.");
                        RewardedStateChanged?.Invoke();
                    });
                });
            }
            catch (Exception ex)
            {
                loadingRewarded = false;
                Debug.LogError("[AdMob Rewarded] Request exception: " + ex.Message);
                ScheduleRewardedRetry();
                RewardedStateChanged?.Invoke();
            }
        }

        private void RegisterRewardedEvents(RewardedAd ad)
        {
            ad.OnAdFullScreenContentOpened += () =>
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    Debug.Log("[AdMob Rewarded] Opened."));

            ad.OnAdFullScreenContentClosed += () =>
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (this != null && rewardedAd == ad)
                        FinishRewardedAd();
                });
            ad.OnAdFullScreenContentFailed += error =>
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (this != null && rewardedAd == ad)
                    {
                        Debug.LogWarning("[AdMob Rewarded] Show failed: " + error);
                        FinishRewardedAd();
                    }
                });
        }

        public bool ShowRewardedForCoins()
        {
#if UNITY_EDITOR
            Debug.Log("[AdMob Rewarded] Android device required.");
            return false;
#else
            if (!IsRewardedReady) return false;
            RewardedAd currentAd = rewardedAd;
            showingRewarded = true;
            rewardGrantedForCurrentAd = false;
            RewardedStateChanged?.Invoke();
            int rewardProcessed = 0;
            try
            {
                currentAd.Show(reward =>
                {
                    // The SDK callback may run off the Unity thread.
                    if (System.Threading.Interlocked.Exchange(ref rewardProcessed, 1) != 0)
                        return;
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        if (this == null) return;
                        rewardGrantedForCurrentAd = true;
                        AdventureSaveService.AddRewardedCoins(rewardedCoinAmount);
                        Debug.Log("[AdMob Rewarded] Reward granted: +" + rewardedCoinAmount);
                        RewardedCoinsGranted?.Invoke(rewardedCoinAmount);
                    });
                });
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[AdMob Rewarded] Show exception: " + ex.Message);
                FinishRewardedAd();
                return false;
            }
#endif
        }

        private void FinishRewardedAd()
        {
            if (!showingRewarded) return;
            showingRewarded = false;
            Debug.Log("[AdMob Rewarded] Closed; reward granted=" +
                      rewardGrantedForCurrentAd);
            DestroyRewarded();
            if (clearAdsAfterFullScreen)
            {
                clearAdsAfterFullScreen = false;
                DestroyInterstitial();
            }
            RewardedStateChanged?.Invoke();
            if (CanLoadAds()) LoadRewarded();

            // A transition clicked during a rewarded ad must not be lost.
            // Never show an interstitial immediately after a rewarded ad.
            Action continuation = deferredRewardedContinuation;
            deferredRewardedContinuation = null;
            InvokeContinuation(continuation);
        }

        public void RunAtTransition(Action continuation)
        {
            if (continuation == null) return;

            if (showingRewarded)
            {
                if (deferredRewardedContinuation == null)
                {
                    deferredRewardedContinuation = continuation;
                    Debug.Log("[AdMob] Transition deferred until rewarded closes.");
                }
                else Debug.LogWarning("[AdMob] Duplicate transition ignored.");
                return;
            }
            if (showingAd)
            {
                // The first continuation is already guaranteed at ad close.
                Debug.LogWarning("[AdMob] Duplicate transition ignored while interstitial is open.");
                return;
            }

            transitionCount++;
            bool frequencyReached = transitionCount >= transitionsPerAd;
            bool cooldownReached = Time.realtimeSinceStartup - lastAdTime >=
                                   minimumSecondsBetweenAds;
            bool adReady = CanLoadAds() && interstitialAd != null &&
                           interstitialAd.CanShowAd() &&
                           Time.realtimeSinceStartup - interstitialLoadedAt < CacheLifetimeSeconds;

            if (!frequencyReached || !cooldownReached || !adReady)
            {
                InvokeContinuation(continuation);
                return;
            }

            transitionCount = 0;
            lastAdTime = Time.realtimeSinceStartup;
            showingAd = true;
            pendingContinuation = continuation;
            Debug.Log("[AdMob] Showing interstitial.");
            try
            {
                interstitialAd.Show();
            }
            catch (Exception ex)
            {
                Debug.LogError("[AdMob] Interstitial show exception: " + ex.Message);
                FinishAdAndContinue();
            }
        }

        private void FinishAdAndContinue()
        {
            if (!showingAd) return;
            showingAd = false;
            DestroyInterstitial();
            if (clearAdsAfterFullScreen)
            {
                clearAdsAfterFullScreen = false;
                DestroyRewarded();
            }
            Action continuation = pendingContinuation;
            pendingContinuation = null;
            if (CanLoadAds()) LoadInterstitial();
            InvokeContinuation(continuation);
        }

        private static void InvokeContinuation(Action continuation)
        {
            if (continuation == null) return;
            try { continuation(); }
            catch (Exception ex)
            {
                Debug.LogError("[AdMob] Transition callback threw: " + ex);
            }
        }

        private void DestroyInterstitial()
        {
            if (interstitialAd == null) return;
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        private void DestroyRewarded()
        {
            if (rewardedAd == null) return;
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        public void ShowPrivacyOptions()
        {
            if (!CanShowPrivacyOptions || privacyOptionsOpen) return;
            privacyOptionsOpen = true;
            try
            {
                ConsentForm.ShowPrivacyOptionsForm(error =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        if (this == null) return;
                        privacyOptionsOpen = false;
                        if (error != null)
                            Debug.LogWarning("[AdMob] Privacy options: " + error.Message);
                        ReconcileConsent(error == null);
                    });
                });
            }
            catch (Exception ex)
            {
                privacyOptionsOpen = false;
                Debug.LogError("[AdMob] Privacy form exception: " + ex.Message);
                ReconcileConsent(false);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            if (!paused) nextMaintenanceTime = 0f;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            adGeneration++;
            DestroyInterstitial();
            DestroyRewarded();
            pendingContinuation = null;
            deferredRewardedContinuation = null;
        }
    }
}

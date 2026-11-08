
using System;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using BlockPuzzle.SaveSystem;
using UnityEngine;

namespace BlockPuzzle.Monetization
{
    public sealed class AdMobManager : MonoBehaviour
    {
        public static AdMobManager Instance { get; private set; }
        public bool IsSdkInitialized => sdkInitialized;
        public bool UseTestAds => useTestAds;
        private const string AndroidTestInterstitialId =
            "ca-app-pub-3940256099942544/1033173712";

        [Header("Ad Configuration")]
        [SerializeField] private bool useTestAds = true;

        [SerializeField] private string androidInterstitialId = "";

        [Header("Frequency Rules")]
        [SerializeField, Min(1)] private int transitionsPerAd = 3;

        [SerializeField, Min(0f)]
        private float minimumSecondsBetweenAds = 120f;

        [Header("Rewarded Ads")]
        [SerializeField] private string androidRewardedId = "";

        [SerializeField, Min(1)]
        private int rewardedCoinAmount = 50;

        private const string AndroidTestRewardedId =
            "ca-app-pub-3940256099942544/5224354917";

        private RewardedAd rewardedAd;

        private bool loadingRewarded;
        private bool showingRewarded;
        private bool rewardGrantedForCurrentAd;

        // Prevent an interstitial from opening during a reward ad.
        public bool IsFullScreenAdShowing =>
            showingAd || showingRewarded;

        public bool IsRewardedReady =>
            sdkInitialized &&
            !loadingRewarded &&
            !IsFullScreenAdShowing &&
            rewardedAd != null &&
            rewardedAd.CanShowAd();

        public int RewardedCoinAmount => rewardedCoinAmount;

        public event Action RewardedStateChanged;
        public event Action<int> RewardedCoinsGranted;

        private InterstitialAd interstitialAd;
        private bool sdkInitialized;
        private bool sdkInitializing;
        private bool loadingAd;
        private bool showingAd;

        private int transitionCount;
        private float lastAdTime = -100000f;

        private Action pendingContinuation;

        public bool CanShowPrivacyOptions =>
            ConsentInformation.PrivacyOptionsRequirementStatus ==
            PrivacyOptionsRequirementStatus.Required;

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

    // Previously obtained consent may already allow requests.
    if (ConsentInformation.CanRequestAds())
    {
        Debug.Log(
            "[AdMob] Existing consent permits ad requests."
        );

        TryInitializeAds();
    }

    // Still refresh consent on every app launch.
    RequestConsent();
#else
            Debug.Log("[AdMob] Editor mode.");
#endif
        }



        private void RequestConsent()
        {
            Debug.Log("[AdMob] Starting UMP consent update.");

            ConsentRequestParameters parameters =
                new ConsentRequestParameters();

            ConsentInformation.Update(
                parameters,
                error =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        if (this == null)
                            return;

                        if (error != null)
                        {
                            Debug.LogWarning(
                                "[AdMob] UMP update failed: " +
                                error.Message
                            );

                            Debug.Log(
                                "[AdMob] CanRequestAds after error: " +
                                ConsentInformation.CanRequestAds()
                            );

                            TryInitializeAds();
                            return;
                        }

                        Debug.Log(
                            "[AdMob] UMP update succeeded."
                        );

                        Debug.Log(
                            "[AdMob] CanRequestAds: " +
                            ConsentInformation.CanRequestAds()
                        );

                        // Consent might already be sufficient.
                        TryInitializeAds();

                        ConsentForm.LoadAndShowConsentFormIfRequired(
                            formError =>
                            {
                                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                                {
                                    if (this == null)
                                        return;

                                    if (formError != null)
                                    {
                                        Debug.LogWarning(
                                            "[AdMob] UMP form error: " +
                                            formError.Message
                                        );
                                    }
                                    else
                                    {
                                        Debug.Log(
                                            "[AdMob] UMP form flow completed."
                                        );
                                    }

                                    Debug.Log(
                                        "[AdMob] CanRequestAds after form: " +
                                        ConsentInformation.CanRequestAds()
                                    );

                                    TryInitializeAds();
                                });
                            });
                    });
                });
        }


        private string GetRewardedId()
        {
            if (useTestAds)
                return AndroidTestRewardedId;

            return androidRewardedId == null
                ? string.Empty
                : androidRewardedId.Trim();
        }

        private void LoadRewarded()
        {
            if (!sdkInitialized ||
                loadingRewarded ||
                showingRewarded ||
                rewardedAd != null)
            {
                return;
            }

            if (!ConsentInformation.CanRequestAds())
                return;

            string adUnitId = GetRewardedId();

            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                Debug.LogWarning(
                    "[AdMob Rewarded] Ad Unit ID is empty."
                );
                return;
            }

            loadingRewarded = true;
            RewardedStateChanged?.Invoke();

            Debug.Log("[AdMob Rewarded] Loading...");

            RewardedAd.Load(
                adUnitId,
                new AdRequest(),
                (ad, error) =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        loadingRewarded = false;

                        if (this == null)
                        {
                            ad?.Destroy();
                            return;
                        }

                        if (error != null || ad == null)
                        {
                            ad?.Destroy();

                            Debug.LogWarning(
                                "[AdMob Rewarded] Load failed: " +
                                error
                            );

                            RewardedStateChanged?.Invoke();
                            return;
                        }

                        rewardedAd = ad;

                        RegisterRewardedEvents(ad);

                        Debug.Log(
                            "[AdMob Rewarded] Loaded successfully."
                        );

                        RewardedStateChanged?.Invoke();
                    });
                });
        }

        private void RegisterRewardedEvents(RewardedAd ad)
        {
            ad.OnAdFullScreenContentOpened += () =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (this == null)
                        return;

                    Debug.Log("[AdMob Rewarded] Opened.");
                });
            };

            ad.OnAdFullScreenContentClosed += () =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (this == null)
                        return;

                    if (rewardedAd != ad)
                        return;

                    Debug.Log(
                        "[AdMob Rewarded] Closed. Reward granted: " +
                        rewardGrantedForCurrentAd
                    );

                    FinishRewardedAd();
                });
            };

            ad.OnAdFullScreenContentFailed += error =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (this == null)
                        return;

                    if (rewardedAd != ad)
                        return;

                    Debug.LogWarning(
                        "[AdMob Rewarded] Show failed: " + error
                    );

                    FinishRewardedAd();
                });
            };
        }


        public bool ShowRewardedForCoins()
        {
#if UNITY_EDITOR
            Debug.Log(
                "[AdMob Rewarded] Android device required."
            );
            return false;
#else
    if (!IsRewardedReady)
    {
        Debug.LogWarning(
            "[AdMob Rewarded] Ad is not ready."
        );
        return false;
    }

    RewardedAd currentAd = rewardedAd;

    showingRewarded = true;
    rewardGrantedForCurrentAd = false;

    RewardedStateChanged?.Invoke();

    // This variable belongs to this specific ad showing.
    int rewardProcessed = 0;

    try
    {
        currentAd.Show(reward =>
        {
            // Deduplicate before dispatching to Unity.
            if (System.Threading.Interlocked.Exchange(
                    ref rewardProcessed, 1) != 0)
            {
                return;
            }

            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (this == null)
                    return;

                rewardGrantedForCurrentAd = true;

                int coins = rewardedCoinAmount;

                AdventureSaveService.AddRewardedCoins(
                    coins
                );

                Debug.Log(
                    "[AdMob Rewarded] Reward granted: +" +
                    coins
                );

                RewardedCoinsGranted?.Invoke(coins);
            });
        });

        return true;
    }
    catch (Exception exception)
    {
        Debug.LogError(
            "[AdMob Rewarded] Show failed: " +
            exception.Message
        );

        FinishRewardedAd();
        return false;
    }
#endif
        }


        private void TryInitializeAds()
        {
            if (sdkInitialized || sdkInitializing)
                return;

            bool canRequest =
                ConsentInformation.CanRequestAds();

            Debug.Log(
                "[AdMob] TryInitializeAds. CanRequestAds: " +
                canRequest
            );

            if (!canRequest)
            {
                Debug.LogWarning(
                    "[AdMob] Initialization deferred: consent not ready."
                );
                return;
            }

            sdkInitializing = true;

            Debug.Log("[AdMob] Calling MobileAds.Initialize.");

            MobileAds.Initialize(status =>
            {
                Debug.Log(
                    "[AdMob] MobileAds.Initialize callback received."
                );

                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (this == null)
                        return;

                    sdkInitializing = false;

                    if (status == null)
                    {
                        sdkInitialized = false;

                        Debug.LogError(
                            "[AdMob] SDK returned null initialization status."
                        );

                        return;
                    }

                    sdkInitialized = true;

                    Debug.Log("[AdMob] SDK Initialized: True");

                    LoadInterstitial();
                    LoadRewarded();
                });
            });
        }


        private void FinishRewardedAd()
        {
            showingRewarded = false;

            if (rewardedAd != null)
            {
                rewardedAd.Destroy();
                rewardedAd = null;
            }

            RewardedStateChanged?.Invoke();

            if (sdkInitialized &&
                ConsentInformation.CanRequestAds())
            {
                LoadRewarded();
            }
        }

        private string GetInterstitialId()
        {
            if (useTestAds)
                return AndroidTestInterstitialId;

            return androidInterstitialId.Trim();
        }

        private void LoadInterstitial()
        {
            if (!sdkInitialized || loadingAd ||
                showingAd || interstitialAd != null)
                return;

            string adUnitId = GetInterstitialId();

            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                Debug.LogWarning(
                    "[AdMob] Interstitial ID is empty."
                );
                return;
            }

            loadingAd = true;

            InterstitialAd.Load(
                adUnitId,
                new AdRequest(),
                (ad, error) =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        loadingAd = false;

                        if (this == null)
                        {
                            ad?.Destroy();
                            return;
                        }

                        if (error != null || ad == null)
                        {
                            ad?.Destroy();

                            Debug.LogWarning(
                                "[AdMob] Interstitial load failed: " +
                                (error == null ? "Unknown" : error.ToString())
                            );

                            // Do not retry continuously on errors.
                            return;
                        }

                        interstitialAd = ad;

                        RegisterInterstitialEvents(ad);

                        Debug.Log(
                            "[AdMob] Interstitial loaded."
                        );
                    });
                });
        }

        private void RegisterInterstitialEvents(
            InterstitialAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    FinishAdAndContinue();
                });
            };

            ad.OnAdFullScreenContentFailed += error =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    Debug.LogWarning(
                        "[AdMob] Ad display failed: " + error
                    );

                    FinishAdAndContinue();
                });
            };
        }


        public void RunAtTransition(Action continuation)
        {
            if (continuation == null)
                return;

            if (IsFullScreenAdShowing)
            {
                Debug.LogWarning(
                    "[AdMob DEBUG] An ad is already showing."
                );
                return;
            }

            transitionCount++;

            bool frequencyReached =
                transitionCount >= transitionsPerAd;

            float elapsed =
                Time.realtimeSinceStartup - lastAdTime;

            bool cooldownFinished =
                elapsed >= minimumSecondsBetweenAds;

            bool adReady =
     !showingRewarded &&
     interstitialAd != null &&
     interstitialAd.CanShowAd();

            Debug.Log(
                "[AdMob DEBUG] Transition requested\n" +
                $"SDK Initialized: {sdkInitialized}\n" +
                $"Consent CanRequestAds: " +
                $"{ConsentInformation.CanRequestAds()}\n" +
                $"Loading Ad: {loadingAd}\n" +
                $"Ad Ready: {adReady}\n" +
                $"Transition Count: {transitionCount}\n" +
                $"Required Count: {transitionsPerAd}\n" +
                $"Frequency Reached: {frequencyReached}\n" +
                $"Cooldown Finished: {cooldownFinished}"
            );

            if (!sdkInitialized ||
                !frequencyReached ||
                !cooldownFinished ||
                !adReady)
            {
                Debug.Log(
                    "[AdMob DEBUG] Skipping ad, continuing game."
                );

                continuation();
                return;
            }

            transitionCount = 0;
            lastAdTime = Time.realtimeSinceStartup;
            showingAd = true;
            pendingContinuation = continuation;

            Debug.Log("[AdMob DEBUG] Showing interstitial.");

            try
            {
                interstitialAd.Show();
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[AdMob DEBUG] Show failed: " +
                    exception.Message
                );

                FinishAdAndContinue();
            }
        }


        private void FinishAdAndContinue()
        {
            if (!showingAd)
                return;

            showingAd = false;

            DestroyInterstitial();

            Action continuation = pendingContinuation;
            pendingContinuation = null;

            // Reload for a future opportunity, not the current one.
            LoadInterstitial();

            continuation?.Invoke();
        }

        private void DestroyInterstitial()
        {
            if (interstitialAd == null)
                return;

            interstitialAd.Destroy();
            interstitialAd = null;
        }

        public void ShowPrivacyOptions()
        {
            if (!CanShowPrivacyOptions)
                return;

            ConsentForm.ShowPrivacyOptionsForm(error =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (this == null)
                        return;

                    if (error != null)
                    {
                        Debug.LogWarning(
                            "[AdMob] Privacy options: " +
                            error.Message
                        );
                    }

                    if (ConsentInformation.CanRequestAds())
                        TryInitializeAds();
                    else
                        DestroyInterstitial();
                });
            });
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            DestroyInterstitial();
            pendingContinuation = null;

            if (rewardedAd != null)
            {
                rewardedAd.Destroy();
                rewardedAd = null;
            }
        }
    }
}

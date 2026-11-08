using System;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockPuzzle.Monetization
{
    /// <summary>
    /// One anchored adaptive banner shared between MainMenu and AdventureMap.
    /// A failed request is retried on the SAME BannerView with backoff.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AdMobManager))]
    public sealed class AdMobAdaptiveBanner : MonoBehaviour
    {
        private const string AndroidTestBannerId =
            "ca-app-pub-3940256099942544/9214589741";
        private const float CheckIntervalSeconds = 0.5f;
        private const float FirstRetrySeconds = 15f;
        private const float MaxRetrySeconds = 120f;

        [Header("Banner Settings")]
        [SerializeField] private bool enableBanner = true;
        [SerializeField] private string androidBannerId = "";

        [Header("Scenes")]
        [SerializeField] private bool showOnMainMenu = true;
        [SerializeField] private bool showOnAdventureMap = true;

        private AdMobManager adMob;
        private BannerView bannerView;
        private bool bannerLoaded;
        private bool bannerVisible;
        private bool requestInFlight;
        private bool applicationPaused;
        private int lastConsentRevision = -1;
        private int failureCount;
        private float nextRetryTime;
        private float nextVisibilityCheck;

        private void Awake()
        {
            adMob = GetComponent<AdMobManager>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            RefreshBannerState();
        }

        private void PositionBannerAboveNavigationBar()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
    if (bannerView == null)
        return;

    float scale = MobileAds.Utils.GetDeviceScale();

    if (scale <= 0f)
        scale = 1f;

    float bannerHeightPixels =
        bannerView.GetHeightInPixels();

    if (bannerHeightPixels <= 0f)
        return;

    Rect safeArea = Screen.safeArea;

    float yPixels =
        Screen.height -
        safeArea.yMin -
        bannerHeightPixels;

    int xDp = Mathf.FloorToInt(
        safeArea.xMin / scale);

    int yDp = Mathf.Max(0, Mathf.FloorToInt(
        yPixels / scale));

    bannerView.SetPosition(xDp, yDp);
#endif
        }

        private void Update()
        {
            if (Time.realtimeSinceStartup < nextVisibilityCheck) return;
            nextVisibilityCheck = Time.realtimeSinceStartup + CheckIntervalSeconds;
            RefreshBannerState();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshBannerState();
        }

        private bool IsAllowedScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            return (showOnMainMenu && sceneName == "MainMenu") ||
                   (showOnAdventureMap && sceneName == "AdventureMap");
        }

        private string GetBannerUnitId()
        {
            if (adMob != null && adMob.UseTestAds) return AndroidTestBannerId;
            return (androidBannerId ?? "").Trim();
        }

        private void RefreshBannerState()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (adMob == null) adMob = AdMobManager.Instance;
            if (adMob == null) return;

            if (lastConsentRevision != adMob.ConsentRevision)
            {
                lastConsentRevision = adMob.ConsentRevision;
                DestroyBanner();
                failureCount = 0;
                nextRetryTime = 0f;
                Debug.Log("[Banner] Consent revision: " + lastConsentRevision);
            }

            if (!ConsentInformation.CanRequestAds())
            {
                DestroyBanner();
                return;
            }

            if (!enableBanner || !IsAllowedScene() || applicationPaused ||
                adMob.IsPrivacyOptionsOpen || adMob.IsFullScreenAdShowing)
            {
                HideBanner();
                return;
            }

            if (!adMob.IsSdkInitialized)
            {
                HideBanner();
                return;
            }

            if (bannerView == null)
            {
                if (Time.realtimeSinceStartup >= nextRetryTime)
                    CreateBanner();
                return;
            }

            if (bannerLoaded)
            {
                ShowBanner();
            }
            else if (!requestInFlight && Time.realtimeSinceStartup >= nextRetryTime)
            {
                // Google recommends requesting another ad on an existing
                // banner view rather than recreate/destroy on each load error.
                RequestBannerAd(bannerView);
            }
#endif
        }

        private void CreateBanner()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            string unitId = GetBannerUnitId();
            if (string.IsNullOrEmpty(unitId))
            {
                Debug.LogWarning("[Banner] Android banner unit ID is missing.");
                nextRetryTime = Time.realtimeSinceStartup + MaxRetrySeconds;
                return;
            }

            int widthDp = MobileAds.Utils.GetDeviceSafeWidth();
            if (widthDp <= 0) return;

            try
            {
                AdSize size = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(widthDp);
                BannerView view = new BannerView(unitId, size, AdPosition.Bottom);
                bannerView = view;
                bannerLoaded = false;
                bannerVisible = false;
                requestInFlight = false;
                view.Hide();

                view.OnBannerAdLoaded += () =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        if (this == null || bannerView != view) return;
                        requestInFlight = false;
                        bannerLoaded = true;
                        failureCount = 0;
                        nextRetryTime = 0f;
                        Debug.Log("[Banner] Anchored adaptive banner loaded.");
                        RefreshBannerState();
                    });
                };

                view.OnBannerAdLoadFailed += error =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        if (this == null || bannerView != view) return;
                        requestInFlight = false;
                        bannerLoaded = false;
                        HideBanner();
                        ScheduleRetry();
                        Debug.LogWarning("[Banner] Load failed: " + error);
                    });
                };

                Debug.Log("[Banner] Created anchored adaptive; width=" + widthDp + "dp.");
                RequestBannerAd(view);
            }
            catch (Exception ex)
            {
                Debug.LogError("[Banner] Create failed: " + ex.Message);
                DestroyBanner();
                ScheduleRetry();
            }
#endif
        }

        private void RequestBannerAd(BannerView view)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (view == null || view != bannerView || requestInFlight ||
                !ConsentInformation.CanRequestAds()) return;
            requestInFlight = true;
            Debug.Log("[Banner] Requesting anchored adaptive banner.");
            try
            {
                view.LoadAd(new AdRequest());
            }
            catch (Exception ex)
            {
                requestInFlight = false;
                Debug.LogError("[Banner] Load exception: " + ex.Message);
                ScheduleRetry();
            }
#endif
        }

        private void ScheduleRetry()
        {
            float seconds = Mathf.Min(MaxRetrySeconds,
                FirstRetrySeconds * Mathf.Pow(2f, Mathf.Min(3, failureCount)));
            failureCount++;
            nextRetryTime = Time.realtimeSinceStartup + seconds;
            Debug.LogWarning($"[Banner] Retry in {seconds:0}s.");
        }


        private void ShowBanner()
        {
            if (bannerView == null ||
                !bannerLoaded ||
                bannerVisible)
                return;

            PositionBannerAboveNavigationBar();

            bannerView.Show();
            bannerVisible = true;

            Debug.Log(
                "[Banner] Visible: " +
                SceneManager.GetActiveScene().name);
        }


        private void HideBanner()
        {
            if (bannerView == null || !bannerVisible) return;
            bannerView.Hide();
            bannerVisible = false;
            Debug.Log("[Banner] Hidden: " + SceneManager.GetActiveScene().name);
        }

        private void DestroyBanner()
        {
            if (bannerView != null)
            {
                bannerView.Destroy();
                bannerView = null;
                Debug.Log("[Banner] Destroyed.");
            }
            bannerLoaded = false;
            bannerVisible = false;
            requestInFlight = false;
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            if (paused) HideBanner();
            else nextVisibilityCheck = 0f;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            DestroyBanner();
        }
    }

}

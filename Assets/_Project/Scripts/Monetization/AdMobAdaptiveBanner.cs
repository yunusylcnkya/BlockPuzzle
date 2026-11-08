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
    /// One native anchored adaptive banner, shared by MainMenu and AdventureMap.
    /// Attach to the existing, persistent AdMobManager object in Boot.
    /// AdMobManager owns consent, SDK initialization and test-ad configuration.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AdMobManager))]
    public sealed class AdMobAdaptiveBanner : MonoBehaviour
    {
        // Official Google Android ANCHORED ADAPTIVE banner test unit.
        private const string AndroidTestBannerId =
            "ca-app-pub-3940256099942544/9214589741";

        [Header("Banner Settings")]
        [SerializeField] private bool enableBanner = true;
        [SerializeField] private string androidBannerId = "";

        [Header("Scenes")]
        [SerializeField] private bool showOnMainMenu = true;
        [SerializeField] private bool showOnAdventureMap = true;

        private const float VisibilityCheckInterval = 0.5f;

        private AdMobManager adMob;
        private BannerView bannerView;
        private bool bannerLoaded;
        private bool bannerVisible;
        private bool applicationPaused;
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

        private void Update()
        {
            if (Time.unscaledTime < nextVisibilityCheck)
                return;

            nextVisibilityCheck =
                Time.unscaledTime + VisibilityCheckInterval;

            RefreshBannerState();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // A native banner is not part of Unity's Canvas. Hide it as soon
            // as the new scene is ready so it never covers gameplay controls.
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
            if (adMob != null && adMob.UseTestAds)
                return AndroidTestBannerId;

            return string.IsNullOrWhiteSpace(androidBannerId)
                ? string.Empty
                : androidBannerId.Trim();
        }

        private void RefreshBannerState()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (adMob == null)
                adMob = AdMobManager.Instance;

            if (!enableBanner || !IsAllowedScene() || applicationPaused)
            {
                HideBanner();
                return;
            }

            // Do not request ads until the existing UMP / MobileAds
            // initialization pipeline has completed successfully.
            bool canRequestAds = ConsentInformation.CanRequestAds();
            if (!canRequestAds)
            {
                DestroyBanner();
                return;
            }

            if (adMob == null || !adMob.IsSdkInitialized)
            {
                HideBanner();
                return;
            }

            if (adMob.IsFullScreenAdShowing)
            {
                HideBanner();
                return;
            }

            if (bannerView == null)
                CreateBanner();

            if (bannerLoaded)
                ShowBanner();
#else
            // No native ads in Unity Editor or non-Android builds.
#endif
        }

        private void CreateBanner()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            string unitId = GetBannerUnitId();
            if (string.IsNullOrEmpty(unitId))
            {
                // Production builds must be given their own banner ad unit.
                Debug.LogWarning("[Banner] Android banner Ad Unit ID is empty.");
                return;
            }

            int safeWidthDp = MobileAds.Utils.GetDeviceSafeWidth();
            if (safeWidthDp <= 0)
                return; // Retry on a subsequent update when metrics are ready.

            try
            {
                AdSize adaptiveSize =
                    AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(
                        safeWidthDp);

                BannerView view = new BannerView(
                    unitId, adaptiveSize, AdPosition.Bottom);

                bannerView = view;
                bannerLoaded = false;
                bannerVisible = false;
                view.Hide();

                view.OnBannerAdLoaded += () =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        if (this == null || bannerView != view)
                            return;

                        bannerLoaded = true;
                        Debug.Log("[Banner] Anchored adaptive banner loaded.");
                        RefreshBannerState();
                    });
                };

                view.OnBannerAdLoadFailed += error =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        if (this == null || bannerView != view)
                            return;

                        bannerLoaded = false;
                        bannerVisible = false;
                        Debug.LogWarning("[Banner] Load failed: " + error);
                        // Avoid tight request/retry loops. SDK ad refresh
                        // settings or a later app launch can retry.
                    });
                };

                Debug.Log(
                    "[Banner] Loading anchored adaptive test=" +
                    adMob.UseTestAds + " width=" + safeWidthDp + "dp.");

                view.LoadAd(new AdRequest());
            }
            catch (Exception ex)
            {
                Debug.LogError("[Banner] Could not create banner: " + ex.Message);
                DestroyBanner();
            }
#endif
        }

        private void ShowBanner()
        {
            if (bannerView == null || !bannerLoaded || bannerVisible)
                return;

            bannerView.Show();
            bannerVisible = true;
            Debug.Log("[Banner] Visible: " + SceneManager.GetActiveScene().name);
        }

        private void HideBanner()
        {
            if (bannerView == null || !bannerVisible)
                return;

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
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            if (paused)
                HideBanner();
            else
                nextVisibilityCheck = 0f;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            DestroyBanner();
        }
    }
}

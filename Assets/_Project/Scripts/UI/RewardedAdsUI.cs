
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Monetization;
using BlockPuzzle.SaveSystem;

namespace BlockPuzzle.UI
{
    public sealed class RewardedAdsUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button watchAdButton;
        [SerializeField] private TMP_Text rewardAmountText;
        [SerializeField] private TMP_Text rewardStatusText;
        [SerializeField] private TMP_Text totalCoinsText;

        private AdMobManager manager;

        private void Start()
        {
            manager = AdMobManager.Instance;

            if (manager == null)
            {
                Debug.LogWarning(
                    "[RewardedAdsUI] AdMobManager missing."
                );

                UpdateUI();
                return;
            }

            manager.RewardedStateChanged += UpdateUI;
            manager.RewardedCoinsGranted += OnRewardGranted;

            AdventureSaveService.DataChanged += UpdateUI;

            if (watchAdButton != null)
                watchAdButton.onClick.AddListener(WatchAd);

            UpdateUI();
        }

        private void OnDestroy()
        {
            if (manager != null)
            {
                manager.RewardedStateChanged -= UpdateUI;
                manager.RewardedCoinsGranted -= OnRewardGranted;
            }

            AdventureSaveService.DataChanged -= UpdateUI;

            if (watchAdButton != null)
                watchAdButton.onClick.RemoveListener(WatchAd);
        }

        private void UpdateUI()
        {
            bool ready =
                manager != null &&
                manager.IsRewardedReady;

            if (watchAdButton != null)
                watchAdButton.interactable = ready;

            if (rewardAmountText != null)
            {
                int amount = manager != null
                    ? manager.RewardedCoinAmount
                    : 50;

                rewardAmountText.text =
                    $"+{amount} COINS";
            }

            if (rewardStatusText != null)
            {
                rewardStatusText.text = ready
                    ? "AD READY"
                    : "AD NOT READY";
            }

            if (totalCoinsText != null)
            {
                totalCoinsText.text =
                    $"COINS: {AdventureSaveService.TotalCoins:N0}";
            }
        }

        private void WatchAd()
        {
            if (manager == null)
                return;

            if (!manager.ShowRewardedForCoins())
            {
                if (rewardStatusText != null)
                    rewardStatusText.text = "AD NOT READY";
            }
        }

        private void OnRewardGranted(int coins)
        {
            if (rewardStatusText != null)
            {
                rewardStatusText.text =
                    $"+{coins} COINS RECEIVED!";
            }

            if (totalCoinsText != null)
            {
                totalCoinsText.text =
                    $"COINS: {AdventureSaveService.TotalCoins:N0}";
            }

            Debug.Log(
                $"[RewardedAdsUI] Player received {coins} coins."
            );
        }
    }
}

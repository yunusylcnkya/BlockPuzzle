using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Blocks;
using BlockPuzzle.Adventure;
using BlockPuzzle.Core;
using BlockPuzzle.SaveSystem;

namespace BlockPuzzle.UI
{
    /// <summary>
    /// In-level coin shop. First purchasable item is an immediate tray reroll.
    /// Keep this component on an always-active GameObject OUTSIDE shopPanel.
    /// </summary>
    public sealed class AdventureCoinShopUI : MonoBehaviour
    {
        [Header("Gameplay")]
        [SerializeField] private BlockSpawner blockSpawner;
        [SerializeField] private AdventureUndoController undoController;

        [Header("Shop UI")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Button openShopButton;
        [SerializeField] private Button closeShopButton;
        [SerializeField] private Button rerollButton;
        [SerializeField] private Button undoButton;

        [Header("Texts")]
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private TMP_Text rerollPriceText;
        [SerializeField] private TMP_Text undoPriceText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text hudCoinsText; // Optional

        [Header("Price")]
        [SerializeField, Min(1)] private int rerollPrice = 50;
        [SerializeField, Min(1)] private int undoPrice = 75;

        private bool processingPurchase;
        private GameState lastObservedState = (GameState)(-1);

        private void Awake()
        {
            if (shopPanel != null)
                shopPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (openShopButton != null)
                openShopButton.onClick.AddListener(OpenShop);

            if (closeShopButton != null)
                closeShopButton.onClick.AddListener(CloseShop);

            if (rerollButton != null)
                rerollButton.onClick.AddListener(PurchaseReroll);
            if (undoButton != null)
                undoButton.onClick.AddListener(PurchaseUndo);
            if (undoController != null)
                undoController.AvailabilityChanged += RefreshUI;

            AdventureSaveService.DataChanged += RefreshUI;

            if (blockSpawner != null)
                blockSpawner.TrayChanged += RefreshUI;

            RefreshUI();
        }

        private void Start()
        {
            RefreshUI();
        }

        private void Update()
        {
            GameState state = GameManager.Instance != null
                ? GameManager.Instance.CurrentState
                : GameState.Boot;

            if (state == lastObservedState)
                return;

            lastObservedState = state;

            if (state != GameState.Playing && shopPanel != null)
                shopPanel.SetActive(false);

            RefreshUI();
        }

        private void OnDisable()
        {
            AdventureSaveService.DataChanged -= RefreshUI;

            if (blockSpawner != null)
                blockSpawner.TrayChanged -= RefreshUI;

            if (openShopButton != null)
                openShopButton.onClick.RemoveListener(OpenShop);

            if (closeShopButton != null)
                closeShopButton.onClick.RemoveListener(CloseShop);

            if (rerollButton != null)
                rerollButton.onClick.RemoveListener(PurchaseReroll);
            if (undoButton != null)
                undoButton.onClick.RemoveListener(PurchaseUndo);
            if (undoController != null)
                undoController.AvailabilityChanged -= RefreshUI;
        }

        public void OpenShop()
        {
            if (!CanShop() || shopPanel == null)
                return;

            shopPanel.SetActive(true);
            shopPanel.transform.SetAsLastSibling();

            if (statusText != null)
                statusText.text = string.Empty;

            RefreshUI();
        }

        public void CloseShop()
        {
            if (shopPanel != null)
                shopPanel.SetActive(false);
        }

        public void PurchaseReroll()
        {
            if (processingPurchase || !CanShop() ||
                shopPanel == null || !shopPanel.activeInHierarchy ||
                blockSpawner == null)
                return;

            if (!blockSpawner.CanRerollRemainingBlocks())
            {
                SetStatus("NO AVAILABLE REROLL");
                RefreshUI();
                return;
            }

            if (AdventureSaveService.TotalCoins < rerollPrice)
            {
                SetStatus("NOT ENOUGH COINS");
                RefreshUI();
                return;
            }

            processingPurchase = true;
            bool success;

            try
            {
                // The spawner validates and chooses new blocks BEFORE calling
                // this payment callback, so ordinary failures cost nothing.
                success = blockSpawner.TryRerollRemainingBlocks(
                    () => AdventureSaveService.TrySpendCoins(rerollPrice));
            }
            finally
            {
                processingPurchase = false;
            }

            if (success)
            {
                Debug.Log($"[CoinShop] Tray rerolled for {rerollPrice} coins.");
                CloseShop();
            }
            else
            {
                SetStatus("PURCHASE NOT COMPLETED");
            }

            RefreshUI();
        }

        public void PurchaseUndo()
        {
            if (processingPurchase || !CanShop() ||
                shopPanel == null || !shopPanel.activeInHierarchy ||
                undoController == null)
                return;

            if (!undoController.CanUndo)
            {
                SetStatus("NO MOVE TO UNDO");
                RefreshUI();
                return;
            }
            if (AdventureSaveService.TotalCoins < undoPrice)
            {
                SetStatus("NOT ENOUGH COINS");
                RefreshUI();
                return;
            }

            processingPurchase = true;
            bool success;
            try
            {
                success = undoController.TryPurchaseUndo(undoPrice);
            }
            finally
            {
                processingPurchase = false;
            }

            if (success)
            {
                Debug.Log($"[CoinShop] Last move undone for {undoPrice} coins.");
                CloseShop();
            }
            else
            {
                SetStatus("UNDO NOT COMPLETED");
            }
            RefreshUI();
        }

        private bool CanShop()
        {
            return GameManager.Instance != null &&
                   GameManager.Instance.CurrentState == GameState.Playing;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        public void RefreshUI()
        {
            int coins = AdventureSaveService.TotalCoins;

            if (balanceText != null)
                balanceText.text = $"COINS: {coins:N0}";

            if (hudCoinsText != null)
                hudCoinsText.text = $"{coins:N0}";

            if (rerollPriceText != null)
                rerollPriceText.text = $"{rerollPrice} COINS";
            if (undoPriceText != null)
                undoPriceText.text = $"{undoPrice} COINS";

            bool canShop = CanShop();
            bool canReroll = canShop && blockSpawner != null &&
                             blockSpawner.CanRerollRemainingBlocks();

            if (openShopButton != null)
                openShopButton.interactable = canShop;

            if (rerollButton != null)
                rerollButton.interactable = canReroll &&
                    !processingPurchase && coins >= rerollPrice;

            if (undoButton != null)
                undoButton.interactable = canShop && !processingPurchase &&
                    undoController != null && undoController.CanUndo &&
                    coins >= undoPrice;
        }
    }
}

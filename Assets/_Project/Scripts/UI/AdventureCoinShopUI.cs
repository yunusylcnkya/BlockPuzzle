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
    /// Adventure coin shop: reroll, one-step undo, and extra moves.
    /// v1.3.5.4.4 adds a per-level-attempt reroll usage limit.
    /// Keep this component on an always-active GameObject outside shopPanel.
    /// </summary>
    public sealed class AdventureCoinShopUI : MonoBehaviour
    {
        [Header("Gameplay")]
        [SerializeField] private BlockSpawner blockSpawner;
        [SerializeField] private AdventureUndoController undoController;
        [SerializeField] private AdventureGameController adventureGame;

        [Header("Shop UI")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Button openShopButton;
        [SerializeField] private Button closeShopButton;
        [SerializeField] private Button rerollButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button extraMovesButton;
        [SerializeField] private Button resultExtraMovesOfferButton;

        [Header("Texts")]
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private TMP_Text rerollPriceText;
        [SerializeField] private TMP_Text undoPriceText;
        [SerializeField] private TMP_Text extraMovesPriceText;
        [SerializeField] private TMP_Text extraMovesDescriptionText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text hudCoinsText; // Optional

        [Header("Prices")]
        [SerializeField, Min(1)] private int rerollPrice = 50;
        [SerializeField, Min(1)] private int undoPrice = 75;
        [SerializeField, Min(1)] private int extraMovesPrice = 100;

        [Header("Reroll Usage Limit - v1.3.5.4.4")]
        [SerializeField, Range(1, 10)] private int maxRerollsPerAttempt = 3;
        [SerializeField] private TMP_Text rerollUsageText; // Optional; assign existing RerollDescriptionText

        private bool processingPurchase;
        private GameState lastObservedState = (GameState)(-1);
        private int rerollsUsed;

        public int RerollsUsed => rerollsUsed;
        public int MaxRerollsPerAttempt => maxRerollsPerAttempt;
        private bool HasRerollsRemaining => rerollsUsed < maxRerollsPerAttempt;

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
            if (extraMovesButton != null)
                extraMovesButton.onClick.AddListener(PurchaseExtraMoves);
            if (resultExtraMovesOfferButton != null)
                resultExtraMovesOfferButton.onClick.AddListener(OpenExtraMovesOffer);
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
                ? GameManager.Instance.CurrentState : GameState.Boot;

            if (state == lastObservedState)
                return;

            lastObservedState = state;
            if (!CanOpenShop() && shopPanel != null)
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
            if (extraMovesButton != null)
                extraMovesButton.onClick.RemoveListener(PurchaseExtraMoves);
            if (resultExtraMovesOfferButton != null)
                resultExtraMovesOfferButton.onClick.RemoveListener(OpenExtraMovesOffer);
            if (undoController != null)
                undoController.AvailabilityChanged -= RefreshUI;
        }

        private bool CanShop() => GameManager.Instance != null &&
            GameManager.Instance.CurrentState == GameState.Playing;

        private bool CanOpenShop() => CanShop() ||
            (adventureGame != null && adventureGame.IsAwaitingExtraMoves &&
             adventureGame.CanPurchaseExtraMoves);

        public void OpenShop()
        {
            if (!CanOpenShop() || shopPanel == null)
                return;

            shopPanel.SetActive(true);
            shopPanel.transform.SetAsLastSibling();
            if (statusText != null)
                statusText.text = string.Empty;
            RefreshUI();
        }

        // ResultPanel has a dedicated offer only for NO MOVES LEFT failures.
        public void OpenExtraMovesOffer()
        {
            if (adventureGame == null || !adventureGame.IsAwaitingExtraMoves ||
                !adventureGame.CanPurchaseExtraMoves)
                return;
            OpenShop();
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

            // Economy rule: a new scene / retry begins a new level attempt.
            // Only successful rerolls consume one use.
            if (!HasRerollsRemaining)
            {
                SetStatus("REROLL LIMIT REACHED");
                RefreshUI();
                return;
            }
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
                success = blockSpawner.TryRerollRemainingBlocks(
                    () => AdventureSaveService.TrySpendCoins(rerollPrice));
                if (success)
                    rerollsUsed++;
            }
            finally
            {
                processingPurchase = false;
            }

            if (success)
            {
                Debug.Log($"[CoinShop] Tray rerolled for {rerollPrice} coins. " +
                    $"Uses: {rerollsUsed}/{maxRerollsPerAttempt}.");
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

        public void PurchaseExtraMoves()
        {
            // At NO MOVES LEFT, GameOver is allowed only for this product.
            if (processingPurchase || !CanOpenShop() ||
                shopPanel == null || !shopPanel.activeInHierarchy ||
                adventureGame == null)
                return;

            if (!adventureGame.CanPurchaseExtraMoves)
            {
                SetStatus("EXTRA MOVES UNAVAILABLE");
                RefreshUI();
                return;
            }
            if (AdventureSaveService.TotalCoins < extraMovesPrice)
            {
                SetStatus("NOT ENOUGH COINS");
                RefreshUI();
                return;
            }

            processingPurchase = true;
            bool success;
            try
            {
                success = adventureGame.TryPurchaseExtraMoves(
                    () => AdventureSaveService.TrySpendCoins(extraMovesPrice));
            }
            finally
            {
                processingPurchase = false;
            }

            if (success)
            {
                Debug.Log($"[CoinShop] +{adventureGame.ExtraMovesPerPurchase} " +
                    $"moves purchased for {extraMovesPrice} coins.");
                CloseShop();
            }
            else
            {
                SetStatus("PURCHASE NOT COMPLETED");
            }
            RefreshUI();
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
            if (extraMovesPriceText != null)
                extraMovesPriceText.text = $"{extraMovesPrice} COINS";
            if (rerollUsageText != null)
                rerollUsageText.text =
                    $"REPLACE REMAINING BLOCKS ({rerollsUsed}/{maxRerollsPerAttempt})";
            if (extraMovesDescriptionText != null && adventureGame != null)
                extraMovesDescriptionText.text =
                    $"+{adventureGame.ExtraMovesPerPurchase} MOVES " +
                    $"({adventureGame.ExtraMovePurchasesUsed}/" +
                    $"{adventureGame.MaxExtraMovePurchases})";

            bool canShop = CanShop();
            bool canOpen = CanOpenShop();
            bool canReroll = canShop && HasRerollsRemaining && blockSpawner != null &&
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
            if (extraMovesButton != null)
                extraMovesButton.interactable = canOpen && !processingPurchase &&
                    adventureGame != null && adventureGame.CanPurchaseExtraMoves &&
                    coins >= extraMovesPrice;
        }
    }
}

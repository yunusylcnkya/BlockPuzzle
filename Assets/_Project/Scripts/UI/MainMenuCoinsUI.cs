
using TMPro;
using UnityEngine;
using BlockPuzzle.SaveSystem;

namespace BlockPuzzle.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class MainMenuCoinsUI : MonoBehaviour
    {
        private TMP_Text coinText;

        private void Awake()
        {
            coinText = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            AdventureSaveService.DataChanged += RefreshCoins;
            RefreshCoins();
        }

        private void OnDisable()
        {
            AdventureSaveService.DataChanged -= RefreshCoins;
        }

        private void RefreshCoins()
        {
            if (coinText == null)
                return;

            coinText.text =
                $"COINS: {AdventureSaveService.TotalCoins:N0}";
        }
    }
}

using System;
using Cysharp.Threading.Tasks;
using Gley.EasyIAP;
using Presenters;
using Services;
using Services.InApp;
using Systems.CurrencySystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceFresh.States
{
    /// <summary>
    /// The continue offers of the failed menu (GDD 4.3 / 8.1 / 11.3): one rewarded-ad continue per attempt,
    /// then gem continues at the prices of GameConfig.ContinueGemCosts. Without enough gems the gem button
    /// sells the once-a-day "need moves" offer instead. The counters live as long as the level scene (one attempt).
    /// </summary>
    public sealed class ContinueOffers
    {
        // Two buttons side by side; a single one is centered. 380 px wide buttons pulse up to 1.2x
        // (456 px), so 240 keeps a gap between them and stays inside the 1006 px panel.
        private const float SideOffset = 240f;

        private readonly LevelManager _levelManager;
        private readonly Action _applyContinue;

        private int _adContinuesUsed;
        private int _paidContinuesUsed;

        /// <param name="applyContinue">Gives the extra moves / time and resumes the level.</param>
        public ContinueOffers(LevelManager levelManager, Action applyContinue)
        {
            _levelManager = levelManager;
            _applyContinue = applyContinue;
        }

        /// <summary>The rewarded "continue" ad was watched: free extra moves.</summary>
        public void OnAdContinueGranted()
        {
            _adContinuesUsed++;
            Models.LevelSessionStats.ContinuesUsed++;
            GameAnalyticsEvents.ContinueUsed(_levelManager.currentLevel, "ad");
            _applyContinue();
        }

        /// <summary>Shows the offers that are still available on the failed menu and binds their buttons.</summary>
        public void Setup(Transform failedMenu)
        {
            var config = _levelManager.GameConfig;
            var adLimit = config != null ? config.ContinueAdsPerAttempt : 1;

            // Rewarded continue: once per attempt, afterwards only gems.
            var adsTransform = failedMenu.Find("AdsButton") as RectTransform;
            var hasAdContinue = _adContinuesUsed < adLimit;
            if (adsTransform != null)
                adsTransform.gameObject.SetActive(hasAdContinue);

            var continueTransform = failedMenu.Find("ButtonContinue") as RectTransform;
            Button continueButton = continueTransform != null ? continueTransform.GetComponent<Button>() : null;

            var costs = config != null ? config.ContinueGemCosts : null;
            var hasPaidContinue = continueButton != null && costs != null && _paidContinuesUsed < costs.Length;
            if (continueButton != null)
                continueButton.gameObject.SetActive(hasPaidContinue);

            if (adsTransform != null)
                adsTransform.anchoredPosition = new Vector2(hasPaidContinue ? -SideOffset : 0f, adsTransform.anchoredPosition.y);

            if (continueTransform != null)
            {
                continueTransform.anchoredPosition =
                    new Vector2(hasAdContinue && adsTransform != null ? SideOffset : 0f, continueTransform.anchoredPosition.y);
            }

            if (hasPaidContinue)
                SetupPaidContinue(continueButton, costs[_paidContinuesUsed]);
        }

        private void SetupPaidContinue(Button continueButton, int cost)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.interactable = true;

            TMP_Text costText = continueButton.transform.Find("Cost")?.GetComponent<TMP_Text>();
            var icon = continueButton.transform.Find("Icon")?.gameObject;

            // Not enough gems: the "need moves" offer (once a day) takes the button with its real price.
            if (!HasGems(cost) && CanOfferNeedMoves())
            {
                continueButton.onClick.AddListener(() => OnNeedMovesOfferClicked(continueButton).Forget());
                if (costText != null)
                    costText.text = ShopCatalogBuilder.PriceOf(_levelManager.InAppConfig, ShopProductNames.NeedMovesOffer);

                if (icon != null) icon.SetActive(false);
                return;
            }

            if (icon != null) icon.SetActive(true);
            continueButton.onClick.AddListener(() => OnPaidContinueClicked(cost));

            if (costText != null)
                costText.text = cost.ToString();
        }

        private bool HasGems(int cost)
        {
            var gems = _levelManager.CurrencyService?.GetCurrency(CurrencyType.Diamond);
            return gems != null && gems.Value >= cost;
        }

        private bool CanOfferNeedMoves()
        {
            return Gley.EasyIAP.API.IsInitialized()
                   && ContextOffers.IsAvailableToday(ShopProductNames.NeedMovesOffer)
                   && !string.IsNullOrEmpty(ShopCatalogBuilder.PriceOf(_levelManager.InAppConfig, ShopProductNames.NeedMovesOffer));
        }

        private async UniTaskVoid OnNeedMovesOfferClicked(Button button)
        {
            if (InAppPurchasingService.IsPurchaseInProgress)
                return;

            button.interactable = false;
            var bought = await InAppPurchasingService.TryBuyConsumableAsync(ShopProductNames.NeedMovesOffer);
            if (button != null) button.interactable = true;

            if (!bought)
            {
                if (InAppPurchasingService.LastFailure != PurchaseFailure.Cancelled)
                    UI.Toast.Show(GameLocalization.L10n.Get("purchase_failed", "Purchase failed. Please try again."));

                return;
            }

            ContextOffers.MarkUsedToday(ShopProductNames.NeedMovesOffer);

            // The offer granted extra-moves boosters and gems (InAppGrantService); one booster is used right now.
            var moves = _levelManager.BoostersProvider?.GetBoosterModel(BoostType.ExtraMoves);
            if (moves != null && moves.HasBooster())
            {
                moves.Use();
                _levelManager.BoostersProvider.Save();
            }

            Models.LevelSessionStats.ContinuesUsed++;
            GameAnalyticsEvents.ContinueUsed(_levelManager.currentLevel, "offer");
            _applyContinue();
        }

        private void OnPaidContinueClicked(int cost)
        {
            var currencyService = _levelManager.CurrencyService;
            if (currencyService == null)
                return;

            var gems = currencyService.GetCurrency(CurrencyType.Diamond);
            if (gems == null || gems.Value < cost || !currencyService.SpendCurrency(CurrencyType.Diamond, cost))
            {
                UI.Toast.Show(GameLocalization.L10n.Get("not_enough_gems", "Not enough gems"));
                return;
            }

            _paidContinuesUsed++;
            Models.LevelSessionStats.ContinuesUsed++;
            GameAnalyticsEvents.GemsSpent("continue", cost);
            GameAnalyticsEvents.ContinueUsed(_levelManager.currentLevel, "gems", cost);
            _applyContinue();
        }
    }
}

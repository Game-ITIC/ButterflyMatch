using System;
using Configs;
using Cysharp.Threading.Tasks;
using Data;
using Gley.EasyIAP;
using Providers;
using Services;
using Services.InApp;
using Systems;
using UnityEngine;
using UnityEngine.UI;
using Views;

namespace Initializers
{
    /// <summary>
    /// The menu shop: top-bar counters open it, the catalog is built from InAppConfig, real-money purchases
    /// rebuild it afterwards and booster cards are paid with gems.
    /// </summary>
    internal sealed class CountryShopBinder : IDisposable
    {
        private readonly InAppView _inAppView;
        private readonly InAppConfig _inAppConfig;
        private readonly InternetState _internetState;
        private readonly HealthSystem _healthSystem;
        private readonly BoostersProvider _boostersProvider;
        private readonly Systems.CurrencySystem.Interfaces.ICurrencyService _currencyService;

        private Presenters.ShopCatalogBuilder _shopBuilder;

        public CountryShopBinder(
            InAppView inAppView,
            InAppConfig inAppConfig,
            InternetState internetState,
            HealthSystem healthSystem,
            BoostersProvider boostersProvider,
            Systems.CurrencySystem.Interfaces.ICurrencyService currencyService)
        {
            _inAppView = inAppView;
            _inAppConfig = inAppConfig;
            _internetState = internetState;
            _healthSystem = healthSystem;
            _boostersProvider = boostersProvider;
            _currencyService = currencyService;
        }

        public async UniTask WarmupAsync()
        {
            if(_inAppView != null)
            {
                var stepSw = System.Diagnostics.Stopwatch.StartNew();
                await _inAppView.Warmup();
                Debug.Log($"[CountryInitializer] _inAppView.Warmup() finished in {stepSw.ElapsedMilliseconds} ms");
            }
        }

        public void BindTopBar(TopBarPanel topBar)
        {
            if(topBar != null && _inAppView != null)
            {
                BindShopButtons(topBar.HealthBarView);
                BindShopButtons(topBar.StarView);
                BindShopButtons(topBar.GemView);
            }
        }

        public void BuildCatalog()
        {
            if(_inAppView != null && _inAppConfig != null && _inAppView.ButtonsParent != null)
            {
                InitializeInAppProducts();
            }
        }

        public void Dispose()
        {
            _shopBuilder?.Dispose();
        }

        private void ShowInAppView()
        {
            _inAppView?.Show();
        }

        private void BindShopButtons(Component view)
        {
            if(view == null || _inAppView == null) return;

            var buttons = view.GetComponentsInChildren<Button>(true);
            for(var i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                if(button == null) continue;

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(ShowInAppView);
            }
        }

        private void InitializeInAppProducts()
        {
            if (_inAppConfig.InAppProducts == null || _inAppConfig.InAppProducts.Count == 0)
            {
                return;
            }

            _shopBuilder?.Dispose();
            _shopBuilder = new Presenters.ShopCatalogBuilder(
                _inAppConfig,
                _inAppView.ButtonsParent,
                _internetState,
                (product, view) => HandlePurchaseInApp(product, view).Forget(),
                () => _healthSystem != null && !_healthSystem.CanPlay,
                BuyBoosterForGems);
            _shopBuilder.Build();
        }

        private async UniTaskVoid HandlePurchaseInApp(ShopProductNames shopProduct, InAppProductView productView)
        {
            if(InAppPurchasingService.IsPurchaseInProgress) return;

            if(productView != null && productView.BuyButton != null)
            {
                productView.BuyButton.interactable = false;
            }

            // The reward is granted by InAppGrantService inside ProcessPurchase; here we only update the UI.
            var isBought = await InAppPurchasingService.TryBuyConsumableAsync(shopProduct);

            if(productView != null && productView.BuyButton != null)
            {
                productView.BuyButton.interactable = true;
            }

            if(!isBought)
            {
                // Visible feedback (GDD 11.1); a cancel by the player needs no message.
                switch (InAppPurchasingService.LastFailure)
                {
                    case PurchaseFailure.StoreUnavailable:
                        UI.Toast.Show(GameLocalization.L10n.Get("purchase_store_unavailable", "Store is not available."));
                        break;
                    case PurchaseFailure.Failed:
                        UI.Toast.Show(GameLocalization.L10n.Get("purchase_failed", "Purchase failed. Please try again."));
                        break;
                }

                return;
            }

            UI.Toast.Show(GameLocalization.L10n.Get("purchase_success", "Thank you for your purchase!"));
            GameAnalyticsEvents.IapPurchase(shopProduct.ToString(), API.IsInitialized() ? API.GetLocalizedPriceString(shopProduct) : string.Empty);

            if(_shopBuilder == null) return;

            // Rebuild so one-time offers, today's deal and No Ads disappear after purchase.
            _shopBuilder.MarkPurchased(shopProduct);
            _shopBuilder.Build();
        }

        /// <summary>Menu shop booster card: spends gems first, the booster is added only when the payment went through.</summary>
        private void BuyBoosterForGems(BoosterOffer offer)
        {
            if(_currencyService == null || _boostersProvider == null)
            {
                return;
            }

            if(!_currencyService.SpendCurrency(Systems.CurrencySystem.CurrencyType.Diamond, offer.gemPrice))
            {
                UI.Toast.Show(GameLocalization.L10n.Get("not_enough_gems", "Not enough gems"));
                return;
            }

            _boostersProvider.GetBoosterModel(offer.type).Add(Mathf.Max(1, offer.count));
            _boostersProvider.Save();

            var boosterName = GameLocalization.L10n.Get("boost_" + offer.type.ToString().ToLowerInvariant(), offer.type.ToString());
            UI.Toast.Show(GameLocalization.L10n.Format("you_got_n", boosterName));
        }
    }
}

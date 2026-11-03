using System;
using System.Collections.Generic;
using System.Threading;
using Configs;
using Cysharp.Threading.Tasks;
using Data;
using Gley.EasyIAP;
using Itic.Scopes;
using Models;
using Providers;
using R3;
using Services;
using Services.InApp;
using Systems;
using UI;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using Views;
using ZLinq;
using Object = UnityEngine.Object;

namespace Initializers
{
    public class CountryInitializer : IAsyncStartable, IDisposable
    {
        private readonly SceneLoader _sceneLoader;
        private readonly InAppView _inAppView;
        private readonly Systems.CurrencySystem.Interfaces.ICurrencyService _currencyService;
        private readonly InternetState _internetState;
        private readonly InAppConfig _inAppConfig;
        private readonly BoostersProvider _boostersProvider;
        private readonly LevelConfig _levelConfig;
        private readonly HealthSystem _healthSystem;
        private readonly LifePopup _lifePopup;
        private readonly AdRewardService _adRewardService;
        private readonly IronSourceManager _ironSourceManager;
        private readonly AdEventModel _adEventModel;
        private readonly RewardPopup _rewardPopup;
        private readonly MainMenuPanel _mainMenuPanel;
        private readonly RegionService _regionService;
        private readonly IslandBuildService _islandBuildService;

        private CompositeDisposable _disposable = new();
        private Presenters.ShopCatalogBuilder _shopBuilder;

        public CountryInitializer(
            SceneLoader sceneLoader,
            Systems.CurrencySystem.Interfaces.ICurrencyService currencyService,
            InternetState internetState,
            InAppConfig inAppConfig,
            BoostersProvider boostersProvider,
            LevelConfig levelConfig,
            HealthSystem healthSystem,
            AdRewardService adRewardService,
            IronSourceManager ironSourceManager,
            AdEventModel adEventModel,
            IObjectResolver resolver)
        {
            _sceneLoader = sceneLoader;
            _currencyService = currencyService;
            _internetState = internetState;
            _inAppConfig = inAppConfig;
            _boostersProvider = boostersProvider;
            _levelConfig = levelConfig;
            _healthSystem = healthSystem;
            _adRewardService = adRewardService;
            _ironSourceManager = ironSourceManager;
            _adEventModel = adEventModel;
            _inAppView = resolver.ResolveOrDefault<InAppView>();
            _lifePopup = resolver.ResolveOrDefault<LifePopup>();
            _rewardPopup = resolver.ResolveOrDefault<RewardPopup>();
            _regionService = resolver.ResolveOrDefault<RegionService>();
            _islandBuildService = resolver.ResolveOrDefault<IslandBuildService>();
            _mainMenuPanel = resolver.ResolveOrDefault<MainMenuPanel>();
        }

        public async UniTask StartAsync(CancellationToken cancellation = new CancellationToken())
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Debug.Log("[CountryInitializer] Starting CountryInitializer.StartAsync()...");

            try
            {
                if(_regionService != null)
                {
                    var stepSw = System.Diagnostics.Stopwatch.StartNew();
                    if(_islandBuildService != null)
                    {
                        var activeIsland = _islandBuildService.ActiveIsland;
                        if(activeIsland != null)
                        {
                            _regionService.SetStartupPrefab(activeIsland.IslandPrefab);
                        }
                    }

                    await _regionService.Warmup();
                    Debug.Log($"[CountryInitializer] _regionService.Warmup() finished in {stepSw.ElapsedMilliseconds} ms");

                    if(_islandBuildService != null)
                    {
                        _islandBuildService.EnsureActiveIslandSaved();
                        _islandBuildService.Restore();
                    }
                }

                if(_inAppView != null)
                {
                    var stepSw = System.Diagnostics.Stopwatch.StartNew();
                    await _inAppView.Warmup();
                    Debug.Log($"[CountryInitializer] _inAppView.Warmup() finished in {stepSw.ElapsedMilliseconds} ms");
                }

                var topBar = _mainMenuPanel?.TopBarPanel;
                if(topBar != null && _inAppView != null)
                {
                    BindShopButtons(topBar.HealthBarView);
                    BindShopButtons(topBar.StarView);
                    BindShopButtons(topBar.GemView);
                }

                var playButton = _mainMenuPanel?.MenuActionPanel?.PlayButton?.Button
                                 ?? FindButtonInScene("PlayButton", "Play Button", "Play", "StartGame", "Start Game");
                if(playButton != null)
                {
                    Debug.Log($"[CountryInitializer] Bound Play button: {playButton.gameObject.name}");
                    playButton.onClick.RemoveAllListeners();
                    playButton.onClick.AddListener(StartGame);
                }
                else
                {
                    Debug.LogWarning("[CountryInitializer] Play button could not be found!");
                }

                InitializeLifePopup();

                // The Build button is driven by BuildingCatalogPresenter / IslandBuildService.

                if(_inAppView != null && _inAppConfig != null && _inAppView.ButtonsParent != null)
                {
                    InitializeInAppProducts();
                }

                var nextLevel = PlayerPrefs.GetInt("OpenLevel", 1);
                var playActionButton = _mainMenuPanel?.MenuActionPanel?.PlayButton;
                if(playActionButton != null)
                {
                    playActionButton.SetLabel(GameLocalization.L10n.Get("play", "Play"));
                    playActionButton.SetSubLabel(GameLocalization.L10n.Format("level_n", nextLevel));
                }
                else if(playButton != null)
                {
                    var tmp = playButton.GetComponentInChildren<TMPro.TMP_Text>(true);
                    if(tmp != null) tmp.text = GameLocalization.L10n.Format("level_n", nextLevel);
                }

                // Language can be switched in Settings while the menu is open.
                _refreshPlayLabel = () =>
                {
                    if(playActionButton != null) { playActionButton.SetLabel(GameLocalization.L10n.Get("play", "Play")); playActionButton.SetSubLabel(GameLocalization.L10n.Format("level_n", nextLevel)); }
                };
                GameLocalization.L10n.Changed += _refreshPlayLabel;

                Debug.Log($"[CountryInitializer] Initialization complete in {sw.ElapsedMilliseconds} ms.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CountryInitializer] Critical error during StartAsync ({sw.ElapsedMilliseconds} ms): {ex}");
            }
        }

        private void InitializeLifePopup()
        {
            if(_lifePopup == null)
            {
                return;
            }

            if(_lifePopup.AdsButton != null && _adRewardService != null && _ironSourceManager != null)
            {
                _lifePopup.AdsButton.onClick.RemoveAllListeners();
                _lifePopup.AdsButton.onClick.AddListener(() =>
                                                         {
                                                             if(_healthSystem.CurrentLives.Value >= _healthSystem.MaxLives)
                                                             {
                                                                 _lifePopup.Hide();
                                                                 return;
                                                             }

                                                             _adRewardService.TryShowRewarded(AdRewardType.Life);
                                                         });
            }

            if(_rewardPopup != null && _adRewardService != null)
            {
                _adRewardService.OnRewardGrantedTyped
                    .Where(type => type == AdRewardType.Life)
                    .Subscribe(_ =>
                               {
                                   _lifePopup.Hide();
                                   _rewardPopup.Show();
                               })
                    .AddTo(_disposable);
            }

            if(_lifePopup.BuyButton != null)
            {
                _lifePopup.BuyButton.onClick.RemoveAllListeners();
                _lifePopup.BuyButton.onClick.AddListener(() =>
                                                         {
                                                             if(_healthSystem.CurrentLives.Value >= _healthSystem.MaxLives)
                                                             {
                                                                 _lifePopup.Hide();
                                                                 return;
                                                             }

                                                             // Spend first: lives are only added when the payment went through.
                                                             if(_currencyService != null
                                                                && _currencyService.SpendCurrency(Systems.CurrencySystem.CurrencyType.Diamond, 200))
                                                             {
                                                                 _healthSystem.RestoreAllLives();
                                                                 _lifePopup.Hide();
                                                             }
                                                         });
            }
        }

        private void BackToMap()
        {
            _sceneLoader.LoadMapAsync().Forget();
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

        private void StartGame()
        {
            var nextLevel = PlayerPrefs.GetInt("OpenLevel", 1);

            if(_levelConfig.Testing)
            {
                nextLevel = _levelConfig.LevelToPlay;
            }

            if(_healthSystem.CanPlay)
            {
                PlayerPrefs.SetInt("OpenLevel", nextLevel);
                _sceneLoader.LoadGameAsync().Forget();
            }
            else if(_lifePopup != null)
            {
                _lifePopup.Show();
            }
            else
            {
                Debug.LogWarning("Cannot start game: no lives available and life popup is not assigned.");
            }
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
                () => _healthSystem != null && !_healthSystem.CanPlay);
            _shopBuilder.Build();
        }

        private static Button FindButtonInScene(params string[] names)
        {
            for (var sceneIndex = 0; sceneIndex < UnityEngine.SceneManagement.SceneManager.sceneCount; sceneIndex++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    var buttons = root.GetComponentsInChildren<Button>(true);
                    foreach (var button in buttons)
                    {
                        foreach (var name in names)
                        {
                            if (string.Equals(button.gameObject.name.Trim(), name.Trim(), System.StringComparison.OrdinalIgnoreCase))
                            {
                                return button;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private Action _refreshPlayLabel;

        public void Dispose()
        {
            if(_refreshPlayLabel != null) GameLocalization.L10n.Changed -= _refreshPlayLabel;
            _shopBuilder?.Dispose();
            _disposable.Dispose();
        }
    }
}

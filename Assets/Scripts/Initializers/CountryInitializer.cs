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
        private const string OneTimeProductBoughtPrefix = "in-app-one-time-bought-";

        private readonly SceneLoader _sceneLoader;
        private readonly InAppView _inAppView;
        private readonly Systems.CurrencySystem.Interfaces.ICurrencyService _currencyService;
        private readonly InternetState _internetState;
        private readonly InAppConfig _inAppConfig;
        private readonly BoostersProvider _boostersProvider;
        private readonly LevelConfig _levelConfig;
        private readonly RegionModel _regionModel;
        private readonly RegionUpgradeService _regionUpgradeService;
        private readonly HealthSystem _healthSystem;
        private readonly RegionConfig _regionConfig;
        private readonly LifePopup _lifePopup;
        private readonly AdRewardService _adRewardService;
        private readonly IronSourceManager _ironSourceManager;
        private readonly AdEventModel _adEventModel;
        private readonly RewardPopup _rewardPopup;
        private readonly MainMenuPanel _mainMenuPanel;
        private readonly RegionService _regionService;

        private CompositeDisposable _disposable = new();
        private bool _isUpgrading;

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
            _regionModel = resolver.ResolveOrDefault<RegionModel>();
            _regionUpgradeService = resolver.ResolveOrDefault<RegionUpgradeService>();
            _regionConfig = resolver.ResolveOrDefault<RegionConfig>();
            _lifePopup = resolver.ResolveOrDefault<LifePopup>();
            _rewardPopup = resolver.ResolveOrDefault<RewardPopup>();
            _regionService = resolver.ResolveOrDefault<RegionService>();
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
                    await _regionService.Warmup();
                    Debug.Log($"[CountryInitializer] _regionService.Warmup() finished in {stepSw.ElapsedMilliseconds} ms");

                    if(_regionUpgradeService != null && _regionModel != null && _regionModel.RegionService?.ActiveRegion != null)
                    {
                        _regionUpgradeService.Initialize(_regionModel);
                        _regionUpgradeService.JumpToFrame(0);

                        if(_regionModel.CurrentLevelProgress != 0
                           && _regionModel.RegionService.ActiveRegion.data != null
                           && _regionModel.CurrentLevelProgress <= _regionModel.RegionService.ActiveRegion.data.Count)
                        {
                            int endFrame = _regionModel.RegionService.ActiveRegion.data[_regionModel.CurrentLevelProgress - 1]
                                .endFrame;
                            _regionUpgradeService.JumpToFrame(endFrame);
                        }
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

                var buildActionButton = _mainMenuPanel?.MenuActionPanel?.BuildButton;
                var buildButton = buildActionButton?.Button
                                  ?? FindButtonInScene("BuildButton", "Build Button", "Build");
                if(buildButton != null && _regionModel != null && _regionUpgradeService != null)
                {
                    Debug.Log($"[CountryInitializer] Bound Build button: {buildButton.gameObject.name}");
                    buildButton.onClick.RemoveAllListeners();
                    buildButton.onClick.AddListener(() => { Upgrade().Forget(); });
                }
                else if(buildButton == null)
                {
                    Debug.LogWarning("[CountryInitializer] Build button could not be found!");
                }

                if(_inAppView != null && _inAppConfig != null && _inAppView.ButtonsParent != null)
                {
                    InitializeInAppProducts();
                }

                var nextLevel = PlayerPrefs.GetInt("OpenLevel", 1);
                var playActionButton = _mainMenuPanel?.MenuActionPanel?.PlayButton;
                if(playActionButton != null)
                {
                    playActionButton.SetLabel("LEVEL " + nextLevel);
                }
                else if(playButton != null)
                {
                    var tmp = playButton.GetComponentInChildren<TMPro.TMP_Text>(true);
                    if(tmp != null) tmp.text = "LEVEL " + nextLevel;
                }

                UpdateBuildButtonLabels();

                if (_regionService != null)
                {
                    _regionService.OnActiveRegionChanged += OnActiveRegionChangedHandler;
                }

                Debug.Log($"[CountryInitializer] Initialization complete in {sw.ElapsedMilliseconds} ms.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CountryInitializer] Critical error during StartAsync ({sw.ElapsedMilliseconds} ms): {ex}");
            }
        }

        private void OnActiveRegionChangedHandler(int index)
        {
            UpdateBuildButtonLabels();
        }

        public void UpdateBuildButtonLabels()
        {
            var buildActionButton = _mainMenuPanel?.MenuActionPanel?.BuildButton;
            var buildButton = buildActionButton?.Button
                              ?? FindButtonInScene("BuildButton", "Build Button", "Build");
            var upgradeStars = _regionModel != null ? _regionModel.UpgradeCost : Models.RegionModel.DefaultUpgradeCost;

            if (buildActionButton != null)
            {
                buildActionButton.SetSubLabel(upgradeStars.ToString());
            }
            else if (buildButton != null)
            {
                var actionButton = buildButton.GetComponent<MenuActionButton>() ?? buildButton.GetComponentInParent<MenuActionButton>();
                if (actionButton != null)
                {
                    actionButton.SetSubLabel(upgradeStars.ToString());
                }
                else
                {
                    var texts = buildButton.GetComponentsInChildren<TMPro.TMP_Text>(true);
                    if (texts.Length > 1)
                    {
                        texts[1].text = upgradeStars.ToString();
                    }
                }
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
                                                             _adRewardService.SetAdRewardType(AdRewardType.Life);
                                                             _ironSourceManager.ShowRewardedAd();
                                                         });
            }

            if(_rewardPopup != null && _adEventModel != null)
            {
                _adEventModel.OnRewardGranted.Subscribe(_ =>
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
                                                             var diamonds = (int)(_currencyService?.GetCurrency(Systems.CurrencySystem.CurrencyType.Diamond)?.Value ?? 0);
                                                             if(diamonds >= 200)
                                                             {
                                                                 _healthSystem.AddLives(5);
                                                                 _currencyService?.SpendCurrency(Systems.CurrencySystem.CurrencyType.Diamond, 200);
                                                                 _lifePopup.Hide();
                                                             }
                                                         });
            }
        }

        private async UniTask Upgrade()
        {
            if (_isUpgrading)
            {
                Debug.Log("[CountryInitializer] Upgrade is already in progress, ignoring click.");
                return;
            }

            _isUpgrading = true;
            try
            {
                if (_regionModel == null || _regionUpgradeService == null) return;

                int prevProgress = _regionModel.CurrentLevelProgress;
                int totalSteps = _regionModel.TotalSteps;

                if (_regionModel.CanLoadNewRegion())
                {
                    if (prevProgress >= totalSteps && totalSteps > 0)
                    {
                        _regionUpgradeService.Initialize(_regionModel);
                        _regionUpgradeService.JumpToFrame(0);
                    }
                }
                else
                {
                    return;
                }

                if (_regionModel.CanUpgrade())
                {
                    _regionModel.Upgrade();

                    var activeData = _regionModel.RegionService?.ActiveRegion?.data;
                    if (activeData != null && _regionModel.CurrentLevelProgress > 0 && _regionModel.CurrentLevelProgress <= activeData.Count)
                    {
                        var currentStepData = activeData[_regionModel.CurrentLevelProgress - 1];
                        int startFrame = currentStepData.startFrame;
                        int endFrame = currentStepData.endFrame;

                        Debug.Log($"[CountryInitializer] 🏗️ Build triggered: Step {_regionModel.CurrentLevelProgress}/{_regionModel.TotalSteps} (Configured startFrame: {startFrame}, endFrame: {endFrame})");

                        _regionUpgradeService.JumpToFrame(startFrame);
                        await _regionUpgradeService.PlayToFrame(endFrame);

                        Debug.Log($"[CountryInitializer] ✅ Build step {_regionModel.CurrentLevelProgress} completed.");
                    }
                }
            }
            finally
            {
                _isUpgrading = false;
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
            var isBought = await InAppPurchasingService.TryBuyConsumableAsync(shopProduct);

            if(!isBought) return;

            var productConfig = _inAppConfig.InAppProducts.AsValueEnumerable().First(v => v.product == shopProduct);

            GrantProduct(productConfig);

            if(productConfig.oneTimePurchase)
            {
                PlayerPrefs.SetInt(GetOneTimeProductKey(productConfig.product), 1);
                PlayerPrefs.Save();
                productView.gameObject.SetActive(false);
            }
        }

        private string ResolvePriceLabel(InAppProduct productConfig)
        {
            if(!string.IsNullOrWhiteSpace(productConfig.priceLabel))
            {
                return productConfig.priceLabel;
            }

            var localizedPrice = API.GetLocalizedPriceString(productConfig.product);
            return string.IsNullOrWhiteSpace(localizedPrice) || localizedPrice == "-"
                ? string.Empty
                : localizedPrice;
        }

        private void GrantProduct(InAppProduct productConfig)
        {
            if(productConfig.gems > 0)
            {
                _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Diamond, productConfig.gems);
            }

            AddBooster(BoostType.Bomb, productConfig.bombs);
            AddBooster(BoostType.Shovel, productConfig.butterflies);
            AddBooster(BoostType.ExtraMoves, productConfig.extraMoves);

            if(productConfig.lives > 0)
            {
                _healthSystem.AddLives(productConfig.lives);
            }

            if(productConfig.unlimitedLivesMinutes > 0)
            {
                _healthSystem.AddUnlimitedLives(TimeSpan.FromMinutes(productConfig.unlimitedLivesMinutes));
            }
        }

        private void AddBooster(BoostType boostType, int amount)
        {
            if(amount <= 0)
            {
                return;
            }

            _boostersProvider.GetBoosterModel(boostType).Add(amount);
            _boostersProvider.Save();
        }

        private static bool IsOneTimeProductBought(InAppProduct productConfig)
        {
            return productConfig.oneTimePurchase
                   && PlayerPrefs.GetInt(GetOneTimeProductKey(productConfig.product), 0) == 1;
        }

        private static string GetOneTimeProductKey(ShopProductNames shopProduct)
        {
            return OneTimeProductBoughtPrefix + shopProduct;
        }

        private void InitializeInAppProducts()
        {
            if (_inAppConfig.InAppProducts == null || _inAppConfig.InAppProducts.Count == 0)
            {
                return;
            }

            var mainParent = _inAppView.ButtonsParent;

            if (_inAppConfig.SmartFilling)
            {
                SpawnSmartInAppProducts(_inAppConfig, mainParent);
            }
            else
            {
                SpawnSequentialInAppProducts(_inAppConfig, mainParent);
            }
        }

        private void SpawnSequentialInAppProducts(InAppConfig config, Transform mainParent)
        {
            Transform currentHlgRow = null;
            int currentUnits = 0;

            foreach (var inAppProduct in config.InAppProducts)
            {
                if (IsOneTimeProductBought(inAppProduct))
                {
                    continue;
                }

                int units = inAppProduct.columnWidth.GetUnits();

                if (currentHlgRow == null || currentUnits + units > 3)
                {
                    currentHlgRow = CreateHlgRow(config, mainParent);
                    currentUnits = 0;
                }

                SpawnAndInitProductView(config, currentHlgRow, inAppProduct);
                currentUnits += units;
            }
        }

        private void SpawnSmartInAppProducts(InAppConfig config, Transform mainParent)
        {
            var remaining = new List<InAppProduct>();
            foreach (var product in config.InAppProducts)
            {
                if (!IsOneTimeProductBought(product))
                {
                    remaining.Add(product);
                }
            }

            while (remaining.Count > 0)
            {
                Transform currentHlgRow = CreateHlgRow(config, mainParent);
                int currentUnits = 0;

                int i = 0;
                while (i < remaining.Count)
                {
                    int units = remaining[i].columnWidth.GetUnits();
                    if (currentUnits + units <= 3)
                    {
                        SpawnAndInitProductView(config, currentHlgRow, remaining[i]);
                        currentUnits += units;
                        remaining.RemoveAt(i);

                        if (currentUnits >= 3)
                        {
                            break;
                        }
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        private Transform CreateHlgRow(InAppConfig config, Transform mainParent)
        {
            if (config.HLGParentPrefab != null)
            {
                return Object.Instantiate(config.HLGParentPrefab, mainParent, false);
            }
            return mainParent;
        }

        private void SpawnAndInitProductView(InAppConfig config, Transform hlgRow, InAppProduct inAppProduct)
        {
            var prefab = config.GetProductViewPrefab(inAppProduct.columnWidth);
            if (prefab == null) return;

            var product = Object.Instantiate(prefab, hlgRow, false);

            var price = ResolvePriceLabel(inAppProduct);
            var rewardText = inAppProduct.gems > 0 ? inAppProduct.gems.ToString() : string.Empty;

            product.Init(inAppProduct.productName, inAppProduct.icon, price, rewardText);

            if (product.BuyButton != null)
            {
                product.BuyButton.onClick.RemoveAllListeners();
                product.BuyButton.onClick.AddListener(() => HandlePurchaseInApp(inAppProduct.product, product).Forget());
            }
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

        public void Dispose()
        {
            if (_regionService != null)
            {
                _regionService.OnActiveRegionChanged -= OnActiveRegionChangedHandler;
            }
            _disposable.Dispose();
        }
    }
}

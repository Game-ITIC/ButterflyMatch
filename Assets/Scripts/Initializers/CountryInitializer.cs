using System;
using System.Threading;
using Configs;
using Cysharp.Threading.Tasks;
using Data;
using Itic.Scopes;
using Models;
using Providers;
using Services;
using Systems;
using UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Views;

namespace Initializers
{
    /// <summary>
    /// Main-menu startup, in order: island model, shop warmup, top-bar shop buttons, Play button, life popup,
    /// shop catalog, Play labels. The work lives in CountryIslandStartup, CountryShopBinder,
    /// CountryPlayButtonBinder and CountryLifePopupBinder (Initializers/Country).
    /// </summary>
    public class CountryInitializer : IAsyncStartable, IDisposable
    {
        private readonly MainMenuPanel _mainMenuPanel;

        private readonly CountryIslandStartup _islandStartup;
        private readonly CountryShopBinder _shop;
        private readonly CountryPlayButtonBinder _playButton;
        private readonly CountryLifePopupBinder _lifePopup;

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
            var inAppView = resolver.ResolveOrDefault<InAppView>();
            var lifePopup = resolver.ResolveOrDefault<LifePopup>();
            var rewardPopup = resolver.ResolveOrDefault<RewardPopup>();
            var regionService = resolver.ResolveOrDefault<RegionService>();
            var islandBuildService = resolver.ResolveOrDefault<IslandBuildService>();
            _mainMenuPanel = resolver.ResolveOrDefault<MainMenuPanel>();

            _islandStartup = new CountryIslandStartup(regionService, islandBuildService);
            _shop = new CountryShopBinder(inAppView, inAppConfig, internetState, healthSystem, boostersProvider, currencyService);
            _playButton = new CountryPlayButtonBinder(sceneLoader, levelConfig, healthSystem, lifePopup, _mainMenuPanel);
            _lifePopup = new CountryLifePopupBinder(lifePopup, rewardPopup, healthSystem, adRewardService, ironSourceManager, currencyService);
        }

        public async UniTask StartAsync(CancellationToken cancellation = new CancellationToken())
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Debug.Log("[CountryInitializer] Starting CountryInitializer.StartAsync()...");

            try
            {
                await _islandStartup.RunAsync();
                await _shop.WarmupAsync();
                _shop.BindTopBar(_mainMenuPanel?.TopBarPanel);
                _playButton.BindButton();
                _lifePopup.Bind();

                // The Build button is driven by BuildingCatalogPresenter / IslandBuildService.

                _shop.BuildCatalog();
                _playButton.ShowLabels();

                Debug.Log($"[CountryInitializer] Initialization complete in {sw.ElapsedMilliseconds} ms.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CountryInitializer] Critical error during StartAsync ({sw.ElapsedMilliseconds} ms): {ex}");
            }
        }

        public void Dispose()
        {
            _playButton.Dispose();
            _shop.Dispose();
            _lifePopup.Dispose();
        }
    }
}

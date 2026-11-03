using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using Initializers;
using Itic.Scopes;
using UnityEngine;
using VContainer.Unity;
using Debug = UnityEngine.Debug;

namespace Itic.Services
{
    public class ServicesStartup : IAsyncStartable
    {
        private const string CoinsMigratedKey = "butterfly-match-coins-migrated";
        private const int CoinsPerDiamond = 10;
        private const string StarterGrantedKey = "butterfly-match-starter-granted";
        private const int StarterDiamonds = 300; // Docs/GDD.md 3: a new player starts with 300 gems

        private readonly SceneLoader _sceneLoader;
        private readonly AdsInitializer _adsInitializer;
        private readonly Systems.CurrencySystem.Interfaces.ICurrencyService _currencyService;

        public ServicesStartup(
            SceneLoader sceneLoader,
            AdsInitializer adsInitializer,
            Systems.CurrencySystem.Interfaces.ICurrencyService currencyService
        )
        {
            _sceneLoader = sceneLoader;
            _adsInitializer = adsInitializer;
            _currencyService = currencyService;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            var sw = Stopwatch.StartNew();
            Debug.Log("[ServicesStartup] === STEP 2/3: Starting ServicesStartup ===");

            GrantStarterBalance();
            MigrateCoinsToDiamonds();

            try
            {
                Debug.Log("[ServicesStartup] Starting AdsInitializer.Warmup()...");
                var adsSw = Stopwatch.StartNew();
                await _adsInitializer.Warmup();
                Debug.Log($"[ServicesStartup] AdsInitializer.Warmup() finished in {adsSw.ElapsedMilliseconds} ms");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ServicesStartup] AdsInitializer warmup failed with error: {e}");
            }

            await UniTask.SwitchToMainThread();

            Debug.Log($"[ServicesStartup] Requesting SceneLoader.LoadMenuAsync() (Total elapsed: {sw.ElapsedMilliseconds} ms)...");
            await _sceneLoader.LoadMenuAsync();
            Debug.Log($"[ServicesStartup] === STEP 2/3 Complete: ServicesStartup finished in {sw.ElapsedMilliseconds} ms ===");
        }

        /// <summary>A brand-new player (no gem balance saved yet) gets the starting gems once.</summary>
        private void GrantStarterBalance()
        {
            if(_currencyService == null || PlayerPrefs.GetInt(StarterGrantedKey, 0) == 1)
            {
                return;
            }

            var isNewPlayer = !PlayerPrefs.HasKey(Systems.CurrencySystem.PlayerPrefsCurrencySaveService.KeyGem)
                              && PlayerPrefs.GetInt("OpenLevel", 1) <= 1;
            if(isNewPlayer)
            {
                _currencyService.AddCurrency(Systems.CurrencySystem.CurrencyType.Diamond, StarterDiamonds);
            }

            PlayerPrefs.SetInt(StarterGrantedKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Coins (Cash) were removed (Docs/GDD.md 3). A leftover balance is converted once: 10 coins -> 1 gem.
        /// </summary>
        private void MigrateCoinsToDiamonds()
        {
            if(_currencyService == null || PlayerPrefs.GetInt(CoinsMigratedKey, 0) == 1)
            {
                return;
            }

            var cash = _currencyService.GetCurrency(Systems.CurrencySystem.CurrencyType.Cash);
            var coins = cash != null ? Mathf.FloorToInt(cash.Value) : 0;
            var diamonds = coins / CoinsPerDiamond;

            if(diamonds > 0)
            {
                _currencyService.AddCurrency(Systems.CurrencySystem.CurrencyType.Diamond, diamonds);
            }

            if(coins > 0)
            {
                _currencyService.SetCurrency(Systems.CurrencySystem.CurrencyType.Cash, 0);
            }

            PlayerPrefs.SetInt(CoinsMigratedKey, 1);
            PlayerPrefs.Save();
            Debug.Log($"[ServicesStartup] Coins migrated: {coins} coins -> {diamonds} gems.");
        }
    }
}

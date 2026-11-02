using System;
using System.Collections.Generic;
using Configs;
using Data;
using Gley.EasyIAP;
using JetBrains.Annotations;
using Providers;
using R3;
using Systems;
using Systems.CurrencySystem.Interfaces;
using UnityEngine;

namespace Services.InApp
{
    /// <summary>
    /// Grants IAP rewards by product id from Unity IAP ProcessPurchase (via API.PurchaseGrantHandler).
    /// Lives in the Services scope so purchases re-delivered on startup are granted before any menu exists.
    /// </summary>
    [UsedImplicitly]
    public class InAppGrantService : IDisposable
    {
        public const string OneTimeProductBoughtPrefix = "in-app-one-time-bought-";
        private const string RemoveAdsKey = "iap-remove-ads";
        private const string GrantedTransactionsKey = "iap-granted-transactions";
        private const int MaxStoredTransactions = 50;

        private readonly InAppConfig _inAppConfig;
        private readonly ICurrencyService _currencyService;
        private readonly BoostersProvider _boostersProvider;
        private readonly HealthSystem _healthSystem;
        private readonly InternetState _internetState;
        private readonly List<string> _grantedTransactions;

        public readonly Subject<ShopProductNames> OnProductGranted = new();

        public bool HasRemoveAds => PlayerPrefs.GetInt(RemoveAdsKey, 0) == 1;

        public InAppGrantService(
            InAppConfig inAppConfig,
            ICurrencyService currencyService,
            BoostersProvider boostersProvider,
            HealthSystem healthSystem,
            InternetState internetState)
        {
            _inAppConfig = inAppConfig;
            _currencyService = currencyService;
            _boostersProvider = boostersProvider;
            _healthSystem = healthSystem;
            _internetState = internetState;
            _grantedTransactions = LoadTransactions();
            _internetState.HasRemoveAds = HasRemoveAds;
        }

        /// <summary>Call before API.Initialize so startup re-deliveries are granted.</summary>
        public void Attach()
        {
            API.PurchaseGrantHandler = Grant;
            API.FlushPendingPurchases();
        }

        public void SetRemoveAds()
        {
            PlayerPrefs.SetInt(RemoveAdsKey, 1);
            PlayerPrefs.Save();
            _internetState.HasRemoveAds = true;

            if(IronSourceManager.Instance != null)
            {
                IronSourceManager.Instance.ApplyRemoveAds();
            }
        }

        public static string GetOneTimeProductKey(ShopProductNames shopProduct)
        {
            return OneTimeProductBoughtPrefix + shopProduct;
        }

        public static bool IsOneTimeProductBought(InAppProduct productConfig)
        {
            return productConfig.oneTimePurchase
                   && PlayerPrefs.GetInt(GetOneTimeProductKey(productConfig.product), 0) == 1;
        }

        private bool Grant(StoreProduct storeProduct, string transactionId)
        {
            if(storeProduct == null || !Enum.TryParse(storeProduct.productName, out ShopProductNames product))
            {
                Debug.LogError($"[InAppGrantService] Unknown product '{storeProduct?.productName}', purchase left pending.");
                return false;
            }

            if(!string.IsNullOrEmpty(transactionId) && _grantedTransactions.Contains(transactionId))
            {
                Debug.LogWarning($"[InAppGrantService] Transaction {transactionId} for {product} already granted, skipping.");
                return true;
            }

            if(product == ShopProductNames.RemoveAds)
            {
                SetRemoveAds();
            }
            else if(!TryGrantConfiguredProduct(product))
            {
                Debug.LogError($"[InAppGrantService] No InAppConfig entry for {product}, purchase left pending.");
                return false;
            }

            RememberTransaction(transactionId);
            PlayerPrefs.Save();
            OnProductGranted.OnNext(product);
            Debug.Log($"[InAppGrantService] Granted {product} (tx {transactionId}).");
            return true;
        }

        private bool TryGrantConfiguredProduct(ShopProductNames product)
        {
            if(_inAppConfig == null || _inAppConfig.InAppProducts == null)
            {
                return false;
            }

            for (var i = 0; i < _inAppConfig.InAppProducts.Count; i++)
            {
                var config = _inAppConfig.InAppProducts[i];
                if(config.product != product)
                {
                    continue;
                }

                if(config.gems > 0)
                {
                    _currencyService.AddCurrency(Systems.CurrencySystem.CurrencyType.Diamond, config.gems);
                }

                AddBooster(BoostType.Bomb, config.bombs);
                AddBooster(BoostType.Shovel, config.butterflies);
                AddBooster(BoostType.ExtraMoves, config.extraMoves);

                if(config.lives > 0)
                {
                    _healthSystem.AddLives(config.lives);
                }

                if(config.unlimitedLivesMinutes > 0)
                {
                    _healthSystem.AddUnlimitedLives(TimeSpan.FromMinutes(config.unlimitedLivesMinutes));
                }

                if(config.oneTimePurchase)
                {
                    PlayerPrefs.SetInt(GetOneTimeProductKey(config.product), 1);
                }

                return true;
            }

            return false;
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

        private void RememberTransaction(string transactionId)
        {
            if(string.IsNullOrEmpty(transactionId))
            {
                return;
            }

            _grantedTransactions.Add(transactionId);
            while (_grantedTransactions.Count > MaxStoredTransactions)
            {
                _grantedTransactions.RemoveAt(0);
            }

            PlayerPrefs.SetString(GrantedTransactionsKey, string.Join("\n", _grantedTransactions));
        }

        private static List<string> LoadTransactions()
        {
            var raw = PlayerPrefs.GetString(GrantedTransactionsKey, string.Empty);
            return string.IsNullOrEmpty(raw)
                ? new List<string>()
                : new List<string>(raw.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        }

        public void Dispose()
        {
            if(API.PurchaseGrantHandler == (Func<StoreProduct, string, bool>)Grant)
            {
                API.PurchaseGrantHandler = null;
            }

            OnProductGranted.Dispose();
        }
    }
}

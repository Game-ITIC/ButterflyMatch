using System;
using System.Threading;
using Configs;
using Cysharp.Threading.Tasks;
using Interfaces;
using Models;
using Providers;
using R3;
using Systems;
using UnityEngine;
using VContainer.Unity;

namespace Services
{
    public class AdRewardService : IInitializable, IDisposable
    {
        // LevelPlay may deliver OnAdRewarded shortly AFTER OnAdClosed, so the pending reward survives a short grace period.
        private const int RewardAfterCloseGraceMs = 3000;

        private readonly AdEventModel _adEventModel;
        private readonly Systems.CurrencySystem.Interfaces.ICurrencyService _currencyService;
        private readonly BoostersProvider _boostersProvider;
        private readonly HealthSystem _healthSystem;
        private readonly GameConfig _gameConfig;

        private AdRewardType _adRewardType;
        private bool _isTypeSet;
        private int _showId;
        private BoostType _boostType;
        private Sprite _icon;
        private string _description;

        private CompositeDisposable _compositeDisposable = new();
        private readonly CancellationTokenSource _lifetimeCts = new();
        public ReactiveCommand OnContinueRewardGranted = new ReactiveCommand();

        /// <summary>Fires after a reward was granted, with the type that was granted.</summary>
        public readonly Subject<AdRewardType> OnRewardGrantedTyped = new();

        public AdRewardService(
            AdEventModel adEventModel,
            Systems.CurrencySystem.Interfaces.ICurrencyService currencyService,
            BoostersProvider boostersProvider,
            HealthSystem healthSystem,
            GameConfig gameConfig
        )
        {
            _adEventModel = adEventModel;
            _currencyService = currencyService;
            _boostersProvider = boostersProvider;
            _healthSystem = healthSystem;
            _gameConfig = gameConfig;
        }

        public void Initialize()
        {
            _adEventModel.OnRewardedReward
                .Subscribe(GrantReward)
                .AddTo(_compositeDisposable);

            _adEventModel.OnRewardedFailed
                .Subscribe(_ => ClearPendingReward())
                .AddTo(_compositeDisposable);

            _adEventModel.OnRewardedClosed
                .Subscribe(_ =>
                           {
                               LastRewardedClosedRealtime = Time.realtimeSinceStartup;
                               ClearPendingRewardAfterGraceAsync(_showId).Forget();
                           })
                .AddTo(_compositeDisposable);
        }

        private const string LifeAdsKey = "ads-free-life";
        private const string BoosterAdsKey = "ads-free-booster";

        /// <summary>Realtime (seconds since startup) when the last rewarded ad was closed; used by interstitial caps.</summary>
        public float LastRewardedClosedRealtime { get; private set; } = -1000f;

        /// <summary>Daily caps from GDD 8.1 (free life 5/day and only below max lives, free booster 3/day).</summary>
        public bool IsWithinLimit(AdRewardType type)
        {
            switch (type)
            {
                case AdRewardType.Life:
                    return _healthSystem.CurrentLives.Value < _healthSystem.MaxLives
                           && Utils.Save.DailyCounter.IsBelow(LifeAdsKey, _gameConfig.FreeLifeAdsPerDay);
                case AdRewardType.Booster:
                    return Utils.Save.DailyCounter.IsBelow(BoosterAdsKey, _gameConfig.FreeBoosterAdsPerDay);
                default:
                    return true;
            }
        }

        private void CountTowardsLimit(AdRewardType type)
        {
            if(type == AdRewardType.Life)
            {
                Utils.Save.DailyCounter.Increment(LifeAdsKey);
            }
            else if(type == AdRewardType.Booster)
            {
                Utils.Save.DailyCounter.Increment(BoosterAdsKey);
            }
        }

        /// <summary>
        /// Shows a rewarded ad bound to this reward type. Returns false when no ad is ready
        /// (nothing is shown and no reward stays pending) so the caller can give feedback.
        /// </summary>
        public bool TryShowRewarded(AdRewardType adRewardType, BoostType? boostType = null)
        {
            if(!IsWithinLimit(adRewardType))
            {
                UI.Toast.Show(GameLocalization.L10n.Get("ad_limit_reached", "No more free rewards today. Come back tomorrow!"));
                return false;
            }

            var ads = IronSourceManager.Instance;
            if(ads == null || !ads.IsRewardedReady())
            {
                ClearPendingReward();
                ads?.LoadRewardedAd();
                Debug.LogWarning($"[AdRewardService] Rewarded ad not ready for {adRewardType}.");
                UI.Toast.Show(GameLocalization.L10n.Get("ad_not_available", "Ad is not available right now. Try again later."));
                return false;
            }

            SetAdRewardType(adRewardType, boostType);
            _showId++;

            if(!ads.ShowRewardedAd())
            {
                ClearPendingReward();
                return false;
            }

            GameAnalyticsEvents.AdRewardedShown(adRewardType.ToString());

            return true;
        }

        public void GrantReward(Unit _)
        {
            if(!_isTypeSet) return;

            var grantedType = _adRewardType;
            _isTypeSet = false;
            CountTowardsLimit(grantedType);

            switch (grantedType)
            {
                case AdRewardType.Coin: // legacy: coins were removed, treated as gems
                case AdRewardType.Gem:
                    _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Diamond, 50);
                    break;
                case AdRewardType.Booster:
                    var booster = _boostersProvider.GetBoosterModel(_boostType);
                    booster.Add(1);
                    _boostersProvider.Save();
                    break;
                case AdRewardType.X2:
                    // Win-screen bonus ad (GDD 8.1 win_bonus): gems, not doubled coins.
                    _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Diamond, _gameConfig.WinAdDiamondReward);
                    break;
                case AdRewardType.Life:
                    _healthSystem.AddLives(1);
                    break;
                case AdRewardType.Continue:
                    OnContinueRewardGranted?.Execute(Unit.Default);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            GameAnalyticsEvents.AdRewardedCompleted(grantedType.ToString());
            OnRewardGrantedTyped.OnNext(grantedType);
            _adEventModel.OnRewardGranted.OnNext(Unit.Default);
        }

        public void SetAdRewardType(AdRewardType adRewardType, BoostType? boostType = null)
        {
            _isTypeSet = true;
            _adRewardType = adRewardType;

            if(boostType != null)
            {
                _boostType = (BoostType)boostType;
            }
        }

        private void ClearPendingReward()
        {
            _isTypeSet = false;
        }

        private async UniTaskVoid ClearPendingRewardAfterGraceAsync(int showId)
        {
            var cancelled = await UniTask.Delay(RewardAfterCloseGraceMs, DelayType.Realtime, cancellationToken: _lifetimeCts.Token)
                .SuppressCancellationThrow();

            // Only clear the reward of the ad that was closed; a newer show keeps its own pending reward.
            if(!cancelled && showId == _showId)
            {
                ClearPendingReward();
            }
        }

        public void Dispose()
        {
            _lifetimeCts.Cancel();
            _lifetimeCts.Dispose();
            _compositeDisposable.Dispose();
            OnRewardGrantedTyped.Dispose();
        }

        public void SetInfo(Sprite icon, string description)
        {
            _icon = icon;
            _description = description;
        }

        public RewardInfo GetInfo()
        {
            return new RewardInfo
            {
                Icon = _icon, Description = _description
            };
        }
    }

    public class RewardInfo
    {
        public Sprite Icon;
        public string Description;
    }

    public enum AdRewardType
    {
        Coin,
        Gem,
        Booster,
        X2,
        Life,
        Continue
    }
}

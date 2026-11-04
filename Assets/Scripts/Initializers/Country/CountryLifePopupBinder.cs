using System;
using R3;
using Services;
using Systems;
using UI;
using Views;

namespace Initializers
{
    /// <summary>
    /// The menu life popup: watch an ad for a life, or refill all lives for gems. A popup opened at full
    /// lives just closes.
    /// </summary>
    internal sealed class CountryLifePopupBinder : IDisposable
    {
        private const int RefillGemPrice = 200;

        private readonly LifePopup _lifePopup;
        private readonly RewardPopup _rewardPopup;
        private readonly HealthSystem _healthSystem;
        private readonly AdRewardService _adRewardService;
        private readonly IronSourceManager _ironSourceManager;
        private readonly Systems.CurrencySystem.Interfaces.ICurrencyService _currencyService;
        private readonly CompositeDisposable _disposable = new();

        public CountryLifePopupBinder(
            LifePopup lifePopup,
            RewardPopup rewardPopup,
            HealthSystem healthSystem,
            AdRewardService adRewardService,
            IronSourceManager ironSourceManager,
            Systems.CurrencySystem.Interfaces.ICurrencyService currencyService)
        {
            _lifePopup = lifePopup;
            _rewardPopup = rewardPopup;
            _healthSystem = healthSystem;
            _adRewardService = adRewardService;
            _ironSourceManager = ironSourceManager;
            _currencyService = currencyService;
        }

        public void Bind()
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
                                                                && _currencyService.SpendCurrency(Systems.CurrencySystem.CurrencyType.Diamond, RefillGemPrice))
                                                             {
                                                                 _healthSystem.RestoreAllLives();
                                                                 _lifePopup.Hide();
                                                             }
                                                         });
            }
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}

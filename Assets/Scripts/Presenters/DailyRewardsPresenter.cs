using Gley.DailyRewards.API;
using Models;
using Providers;
using UnityEngine;
using UnityEngine.UI;
using VContainer.Unity;
using ZLinq;

namespace Presenters
{
    public class DailyRewardsPresenter : IInitializable
    {
        private readonly Button _dailyButton;
        private readonly Systems.CurrencySystem.Interfaces.ICurrencyService _currencyService;
        private readonly BoostersProvider _boostersProvider;

        public DailyRewardsPresenter(
            Button dailyButton,
            Systems.CurrencySystem.Interfaces.ICurrencyService currencyService,
            BoostersProvider boostersProvider
        )
        {
            _dailyButton = dailyButton;
            _currencyService = currencyService;
            _boostersProvider = boostersProvider;
        }

        public void Initialize()
        {
            if(_dailyButton != null)
            {
                _dailyButton.onClick.RemoveListener(OpenCalendar);
                _dailyButton.onClick.AddListener(OpenCalendar);
            }
            
            Calendar.AddClickListener(OnDayClick);
        }

        private void OnDayClick(int day, int value, Sprite icon)
        {
            switch (day)
            {
                case 1:
                    _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Diamond, value);
                    break;
                case 2:
                    _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Diamond, value);
                    break;
                case 3:
                    _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Diamond, value);
                    break;
                case 4:
                {
                    var booster = _boostersProvider.BoostersModels
                        .AsValueEnumerable()
                        .First(v => v.Type == BoostType.Bomb);
                    booster.Add(value);
                    _boostersProvider.Save();
                }
                    break;
                case 5:
                {
                    var booster = _boostersProvider.BoostersModels
                        .AsValueEnumerable()
                        .First(v => v.Type == BoostType.Shovel);
                    booster.Add(value);
                    _boostersProvider.Save();
                }
                    break;
                case 6:
                    _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Diamond, value);
                    break;
                case 7:
                    _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Diamond, value);
                    break;
            }
        }


        private void OpenCalendar()
        {
            Calendar.Show();
        }
    }
}

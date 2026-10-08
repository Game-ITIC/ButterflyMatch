using System;
using System.Collections.Generic;
using System.Linq;
using Gley.DailyRewards.API;
using Gley.DailyRewards.Internal;
using Models;
using Providers;
using R3;
using UI;
using UnityEngine;
using UnityEngine.UI;
using VContainer.Unity;
using Views;
using ZLinq;
using Object = UnityEngine.Object;

namespace Presenters
{
    public class DailyCardsPresenter : IInitializable, IDisposable
    {
        private readonly Systems.CurrencySystem.Interfaces.ICurrencyService _currencyService;
        private readonly BoostersProvider _boostersProvider;

        private readonly List<DailyCardView> _cards = new();
        private readonly CompositeDisposable _disposables = new();

        public DailyCardsPresenter(
            Systems.CurrencySystem.Interfaces.ICurrencyService currencyService,
            BoostersProvider boostersProvider)
        {
            _currencyService = currencyService;
            _boostersProvider = boostersProvider;
        }

        public void Initialize()
        {
            Calendar.AddClickListener(OnDayClaimed);
            BindCards();

            if(_cards.Count == 0)
            {
                return;
            }

            RefreshCards();

            Observable.Interval(TimeSpan.FromSeconds(0.5f))
                .Subscribe(_ => RefreshCards())
                .AddTo(_disposables);
        }

        private void BindCards()
        {
            _cards.Clear();

            _ = Calendar.GetRemainingTimeSpan();

            var settings = Resources.Load<DailyRewardsData>(Constants.DATA_NAME_RUNTIME);
            if(settings == null || settings.allDays == null || settings.allDays.Count == 0)
            {
                Debug.LogWarning("Daily rewards data is missing. Configure Gley Daily Rewards.");
                return;
            }

            var cardViews = FindDailyCards()
                .OrderBy(card => card.transform.GetSiblingIndex())
                .ToList();

            if(cardViews.Count == 0)
            {
                Debug.LogWarning("Daily rewards UI is missing a DailyCardView template.");
                return;
            }

            EnsureCardViews(cardViews, settings.allDays.Count);

            var daysToBind = Mathf.Min(cardViews.Count, settings.allDays.Count);

            for(var i = 0; i < daysToBind; i++)
            {
                var card = cardViews[i];
                var day = settings.allDays[i];

                card.Bind(i + 1, day.dayTexture, day.rewardValue);
                WireClaimButton(card);
                _cards.Add(card);
            }
        }

        private static IEnumerable<DailyCardView> FindDailyCards()
        {
            var panels = Object.FindObjectsByType<DailyRewardsPanelFit>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if(panels != null && panels.Length > 0)
            {
                return panels[0].GetComponentsInChildren<DailyCardView>(true);
            }

            return Object.FindObjectsByType<DailyCardView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private static void EnsureCardViews(List<DailyCardView> cardViews, int requiredCount)
        {
            if(cardViews.Count == 0 || requiredCount <= cardViews.Count)
            {
                return;
            }

            var template = cardViews[0];
            var parent = template.transform.parent;
            if(parent == null)
            {
                return;
            }

            HidePlaceholderChildren(parent);

            for(var i = cardViews.Count; i < requiredCount; i++)
            {
                var card = Object.Instantiate(template, parent);
                card.name = $"{template.name} Day {i + 1}";
                card.transform.SetSiblingIndex(i);
                cardViews.Add(card);
            }
        }

        private static void HidePlaceholderChildren(Transform parent)
        {
            for(var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if(child.GetComponent<DailyCardView>() == null)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private void WireClaimButton(DailyCardView card)
        {
            if(card.ClaimButton == null)
            {
                return;
            }

            card.ClaimButton.onClick.RemoveAllListeners();
            card.ClaimButton.onClick.AddListener(() => TryClaim(card));
        }

        private void TryClaim(DailyCardView card)
        {
            var manager = CalendarManager.Instance;
            var currentDay = manager.GetCurrentDay();
            var timeExpired = manager.TimeExpired();

            if(!card.CanClaim(currentDay, timeExpired))
            {
                return;
            }

            manager.ButtonClick(card.DayNumber, card.RewardValue, card.RewardSprite);
        }

        private void RefreshCards()
        {
            if(_cards.Count == 0)
            {
                return;
            }

            var manager = CalendarManager.Instance;
            var currentDay = manager.GetCurrentDay();
            var timeExpired = manager.TimeExpired();

            foreach (var card in _cards)
            {
                card.ApplyState(currentDay, timeExpired);
            }
        }

        private void OnDayClaimed(int day, int value, Sprite icon)
        {
            switch (day)
            {
                case 1:
                    _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Cash, value);
                    break;
                case 2:
                    _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Diamond, value);
                    break;
                case 3:
                    _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Cash, value);
                    break;
                case 4:
                {
                    var booster = _boostersProvider.BoostersModels
                        .AsValueEnumerable()
                        .First(v => v.Type == BoostType.Bomb);
                    booster.Add(value);
                }
                    break;
                case 5:
                {
                    var booster = _boostersProvider.BoostersModels
                        .AsValueEnumerable()
                        .First(v => v.Type == BoostType.Shovel);
                    booster.Add(value);
                }
                    break;
                case 6:
                    _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Cash, value);
                    break;
                case 7:
                    _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Diamond, value);
                    break;
            }

            RefreshCards();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}

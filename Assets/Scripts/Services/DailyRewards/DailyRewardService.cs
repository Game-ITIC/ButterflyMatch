using System;
using System.Collections.Generic;
using Models;
using Providers;
using Systems.CurrencySystem;
using Systems.CurrencySystem.Interfaces;
using UnityEngine;

namespace Services.DailyRewards
{
    public enum DailyRewardGrantType
    {
        Cash,
        Diamond,
        Bomb,
        Shovel
    }

    public sealed class DailyRewardDefinition
    {
        public DailyRewardDefinition(DailyRewardGrantType grantType, int amount)
        {
            GrantType = grantType;
            Amount = amount;
        }

        public DailyRewardGrantType GrantType { get; }
        public int Amount { get; }
    }

    public class DailyRewardService
    {
        private const string ClaimedDayIndexKey = "butterfly-match-daily-reward-day-index";
        private const string LastClaimDateKey = "butterfly-match-daily-reward-last-claim-date";

        private static readonly DailyRewardDefinition[] Rewards =
        {
            new(DailyRewardGrantType.Cash, 50),
            new(DailyRewardGrantType.Diamond, 2),
            new(DailyRewardGrantType.Cash, 100),
            new(DailyRewardGrantType.Bomb, 1),
            new(DailyRewardGrantType.Shovel, 1),
            new(DailyRewardGrantType.Cash, 150),
            new(DailyRewardGrantType.Diamond, 5)
        };

        private readonly ICurrencyService _currencyService;
        private readonly BoostersProvider _boostersProvider;

        public DailyRewardService(ICurrencyService currencyService, BoostersProvider boostersProvider)
        {
            _currencyService = currencyService;
            _boostersProvider = boostersProvider;
        }

        public IReadOnlyList<DailyRewardDefinition> GetAllRewards() => Rewards;

        public int GetCurrentDayIndex() => PlayerPrefs.GetInt(ClaimedDayIndexKey, 0);

        public bool CanClaimToday()
        {
            var lastDate = PlayerPrefs.GetString(LastClaimDateKey, string.Empty);
            return lastDate != Today();
        }

        public DailyRewardDefinition GetTodayReward()
        {
            if (Rewards.Length == 0)
                return null;

            return Rewards[GetCurrentDayIndex() % Rewards.Length];
        }

        public DailyRewardDefinition GetReward(int index)
        {
            if (index < 0 || index >= Rewards.Length)
                return null;

            return Rewards[index];
        }

        public void ClaimReward()
        {
            if (!CanClaimToday())
                return;

            var reward = GetTodayReward();
            if (reward == null)
                return;

            Grant(reward);
            PlayerPrefs.SetInt(ClaimedDayIndexKey, GetCurrentDayIndex() + 1);
            PlayerPrefs.SetString(LastClaimDateKey, Today());
            PlayerPrefs.Save();
        }

        private void Grant(DailyRewardDefinition reward)
        {
            switch (reward.GrantType)
            {
                case DailyRewardGrantType.Cash:
                    _currencyService?.AddCurrency(CurrencyType.Cash, reward.Amount);
                    break;
                case DailyRewardGrantType.Diamond:
                    _currencyService?.AddCurrency(CurrencyType.Diamond, reward.Amount);
                    break;
                case DailyRewardGrantType.Bomb:
                    _boostersProvider?.GetBoosterModel(BoostType.Bomb).Add(reward.Amount);
                    break;
                case DailyRewardGrantType.Shovel:
                    _boostersProvider?.GetBoosterModel(BoostType.Shovel).Add(reward.Amount);
                    break;
            }
        }

        private static string Today() => DateTime.UtcNow.ToString("yyyy-MM-dd");
    }
}

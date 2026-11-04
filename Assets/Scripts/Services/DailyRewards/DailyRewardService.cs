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
        Diamond,
        Bomb,
        Shovel
    }

    public sealed class DailyRewardDefinition
    {
        public DailyRewardDefinition(DailyRewardGrantType grantType, int amount, int extraMoves = 0, int unlimitedLivesMinutes = 0)
        {
            GrantType = grantType;
            Amount = amount;
            ExtraMoves = extraMoves;
            UnlimitedLivesMinutes = unlimitedLivesMinutes;
        }

        public DailyRewardGrantType GrantType { get; }
        public int Amount { get; }

        /// <summary>Bonus "+5 moves" boosters on top of the main reward (day 7).</summary>
        public int ExtraMoves { get; }

        /// <summary>Bonus unlimited lives on top of the main reward (day 7).</summary>
        public int UnlimitedLivesMinutes { get; }
    }

    public class DailyRewardService
    {
        private const string ClaimedDayIndexKey = "butterfly-match-daily-reward-day-index";
        public const string LastClaimDateKey ="butterfly-match-daily-reward-last-claim-date";

        private static readonly DailyRewardDefinition[] Rewards =
        {
            // Docs/GDD.md 9 (coins removed).
            new(DailyRewardGrantType.Diamond, 10),
            new(DailyRewardGrantType.Diamond, 20),
            new(DailyRewardGrantType.Bomb, 1),
            new(DailyRewardGrantType.Diamond, 30),
            new(DailyRewardGrantType.Shovel, 1),
            new(DailyRewardGrantType.Diamond, 40),
            new(DailyRewardGrantType.Diamond, 100, extraMoves: 1, unlimitedLivesMinutes: 30)
        };

        private readonly ICurrencyService _currencyService;
        private readonly BoostersProvider _boostersProvider;
        private readonly Systems.HealthSystem _healthSystem;

        public DailyRewardService(ICurrencyService currencyService, BoostersProvider boostersProvider, Systems.HealthSystem healthSystem)
        {
            _currencyService = currencyService;
            _boostersProvider = boostersProvider;
            _healthSystem = healthSystem;
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
                case DailyRewardGrantType.Diamond:
                    _currencyService?.AddCurrency(CurrencyType.Diamond, reward.Amount);
                    break;
                case DailyRewardGrantType.Bomb:
                    _boostersProvider?.GetBoosterModel(BoostType.Bomb).Add(reward.Amount);
                    _boostersProvider?.Save();
                    break;
                case DailyRewardGrantType.Shovel:
                    _boostersProvider?.GetBoosterModel(BoostType.Shovel).Add(reward.Amount);
                    _boostersProvider?.Save();
                    break;
            }

            if (reward.ExtraMoves > 0 && _boostersProvider != null)
            {
                _boostersProvider.GetBoosterModel(BoostType.ExtraMoves).Add(reward.ExtraMoves);
                _boostersProvider.Save();
            }

            if (reward.UnlimitedLivesMinutes > 0)
            {
                _healthSystem?.AddUnlimitedLives(TimeSpan.FromMinutes(reward.UnlimitedLivesMinutes));
            }
        }

        private static string Today() => DateTime.UtcNow.ToString("yyyy-MM-dd");
    }
}

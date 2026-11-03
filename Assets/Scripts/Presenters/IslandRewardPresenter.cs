using System;
using System.Collections.Generic;
using Configs;
using Providers;
using Services;
using Systems;
using Systems.CurrencySystem;
using Systems.CurrencySystem.Interfaces;
using UI;
using UnityEngine;
using VContainer.Unity;

namespace Presenters
{
    /// <summary>
    /// Reward for a fully built island (Docs/GDD.md 5.3): gems, boosters and unlimited lives, once per island,
    /// then the next island opens (IslandBuildService.IsUnlocked).
    /// </summary>
    public class IslandRewardPresenter : IInitializable, IDisposable
    {
        private const string ClaimedKeyPrefix = "island-reward-claimed-";

        private readonly struct Reward
        {
            public Reward(int gems, int boosters, int unlimitedLivesMinutes)
            {
                Gems = gems;
                Boosters = boosters;
                UnlimitedLivesMinutes = unlimitedLivesMinutes;
            }

            public int Gems { get; }
            public int Boosters { get; }
            public int UnlimitedLivesMinutes { get; }
        }

        private static readonly Dictionary<string, Reward> Rewards = new()
        {
            { "japan", new Reward(100, 1, 30) },
            { "china", new Reward(150, 1, 30) },
            { "korea", new Reward(300, 2, 60) },   // end of chapter
            { "norway", new Reward(150, 1, 30) },
            { "lapland", new Reward(200, 1, 30) },
            { "iceland", new Reward(300, 2, 60) }, // end of chapter
            { "france", new Reward(200, 1, 30) },
            { "greece", new Reward(250, 1, 30) },
            { "italy", new Reward(400, 2, 60) },
        };

        private static readonly Reward DefaultReward = new(150, 1, 30);

        private readonly IslandBuildService _buildService;
        private readonly ICurrencyService _currencyService;
        private readonly BoostersProvider _boostersProvider;
        private readonly HealthSystem _healthSystem;
        private readonly RewardPopup _rewardPopup;

        public IslandRewardPresenter(
            IslandBuildService buildService,
            ICurrencyService currencyService,
            BoostersProvider boostersProvider,
            HealthSystem healthSystem,
            VContainer.IObjectResolver resolver)
        {
            _buildService = buildService;
            _currencyService = currencyService;
            _boostersProvider = boostersProvider;
            _healthSystem = healthSystem;
            _rewardPopup = VContainer.IObjectResolverExtensions.ResolveOrDefault<RewardPopup>(resolver);
        }

        public void Initialize()
        {
            _buildService.IslandCompleted += OnIslandCompleted;

            // Islands finished before this reward existed (or while the app was killed) still pay once.
            foreach (var island in _buildService.OrderedIslands())
            {
                if (_buildService.IsCompleted(island))
                {
                    Grant(island, showPopup: false);
                }
            }
        }

        public void Dispose()
        {
            _buildService.IslandCompleted -= OnIslandCompleted;
        }

        private void OnIslandCompleted(IslandSO island)
        {
            Grant(island, showPopup: true);
        }

        private void Grant(IslandSO island, bool showPopup)
        {
            if (island == null)
            {
                return;
            }

            var key = ClaimedKeyPrefix + island.IslandId;
            if (PlayerPrefs.GetInt(key, 0) == 1)
            {
                return;
            }

            PlayerPrefs.SetInt(key, 1);

            var reward = Rewards.TryGetValue(island.IslandId, out var r) ? r : DefaultReward;
            _currencyService?.AddCurrency(CurrencyType.Diamond, reward.Gems);

            if (_boostersProvider != null && reward.Boosters > 0)
            {
                foreach (BoostType type in Enum.GetValues(typeof(BoostType)))
                {
                    _boostersProvider.GetBoosterModel(type)?.Add(reward.Boosters);
                }

                _boostersProvider.Save();
            }

            if (reward.UnlimitedLivesMinutes > 0)
            {
                _healthSystem?.AddUnlimitedLives(TimeSpan.FromMinutes(reward.UnlimitedLivesMinutes));
            }

            PlayerPrefs.Save();

            if (!showPopup || _rewardPopup == null)
            {
                return;
            }

            var description = GameLocalization.L10n.Format(
                "island_reward_desc",
                GameLocalization.L10n.Translate(island.DisplayName),
                reward.Gems,
                reward.Boosters,
                reward.UnlimitedLivesMinutes).Replace("\\n", "\n");

            if (_buildService.AllIslandsCompleted())
            {
                description += "\n" + GameLocalization.L10n.Get("islands_coming_soon", "New islands are coming soon!");
            }

            _rewardPopup.Init(new RewardInfo { Icon = island.Icon, Description = description });
            _rewardPopup.Show();
        }
    }
}

using System;
using Configs;
using R3;
using Services;
using UnityEngine;
using Utils.Save;

namespace Models
{
    public class RegionModel : IDisposable
    {
        public const int DefaultUpgradeCost = 5;

        private readonly Systems.CurrencySystem.Interfaces.ICurrencyService _currencyService;
        private readonly RegionService _regionService;

        public RegionService RegionService => _regionService;

        public int UpgradeCost
        {
            get
            {
                if (_regionService?.ActiveRegionData != null && _regionService.ActiveRegionData.starCost > 0)
                {
                    return _regionService.ActiveRegionData.starCost;
                }
                return DefaultUpgradeCost;
            }
        }

        public int RequiredStars => UpgradeCost;

        public int TotalSteps
        {
            get
            {
                if (_regionService != null)
                {
                    return GetRegionTotalSteps(_regionService.CurrentRegionIndex);
                }
                return 0;
            }
        }

        public int CurrentLevelProgress
        {
            get => CurrentLevelProgressReactiveProperty.Value;
            private set => CurrentLevelProgressReactiveProperty.Value = value;
        }

        public readonly ReactiveProperty<int> CurrentLevelProgressReactiveProperty = new();

        public RegionModel(
            Systems.CurrencySystem.Interfaces.ICurrencyService currencyService,
            RegionService regionService
        )
        {
            _currencyService = currencyService;
            _regionService = regionService;

            if (_regionService != null)
            {
                _regionService.OnActiveRegionChanged += OnActiveRegionChanged;
            }

            LoadCurrentRegionProgress();
        }

        private void OnActiveRegionChanged(int index)
        {
            LoadCurrentRegionProgress();
        }

        public string GetRegionProgressKey(int index)
        {
            return index == 0 ? PlayerPrefsKeys.AsiaBuildingsAnimation : $"region_progress_{index}";
        }

        public int GetRegionProgress(int index)
        {
            return PlayerPrefs.GetInt(GetRegionProgressKey(index), 0);
        }

        public void SetRegionProgress(int index, int value)
        {
            PlayerPrefs.SetInt(GetRegionProgressKey(index), value);
            PlayerPrefs.Save();
        }

        public int GetRegionTotalSteps(int index)
        {
            if (_regionService != null && _regionService.CurrentRegionIndex == index && _regionService.ActiveRegion?.data != null)
            {
                return _regionService.ActiveRegion.data.Count;
            }

            return _regionService?.RegionConfig != null ? _regionService.RegionConfig.GetTotalStepsForRegion(index) : 0;
        }

        public float GetRegionProgressNormalized(int index)
        {
            var total = GetRegionTotalSteps(index);
            if (total <= 0) return 0f;

            var progress = GetRegionProgress(index);
            return Mathf.Clamp01(progress / (float)total);
        }

        public bool IsRegionUnlocked(int index)
        {
            if (index == 0) return true;
            if (_regionService?.RegionConfig == null) return false;

            var regionData = _regionService.RegionConfig.GetRegionByIndex(index);
            if (regionData == null || regionData.isComingSoon) return false;

            return GetRegionProgressNormalized(index - 1) >= 0.5f;
        }

        public bool CanUpgrade()
        {
            var stars = _currencyService?.GetCurrency(Systems.CurrencySystem.CurrencyType.Star)?.Value ?? 0f;
            return stars >= UpgradeCost && TotalSteps > CurrentLevelProgress;
        }

        public bool CanLoadNewRegion()
        {
            var stars = _currencyService?.GetCurrency(Systems.CurrencySystem.CurrencyType.Star)?.Value ?? 0f;
            if (stars >= UpgradeCost && CurrentLevelProgress >= TotalSteps && TotalSteps > 0)
            {
                if (!_regionService.CanLoadNextRegion()) return false;

                _regionService.LoadNextRegion();
                CurrentLevelProgress = 0;
                SetRegionProgress(_regionService.CurrentRegionIndex, 0);
            }

            return true;
        }

        public void Upgrade()
        {
            var cost = UpgradeCost;
            var activeIndex = _regionService != null ? _regionService.CurrentRegionIndex : 0;
            var current = GetRegionProgress(activeIndex) + 1;

            SetRegionProgress(activeIndex, current);
            CurrentLevelProgress = current;
            _currencyService?.SpendCurrency(Systems.CurrencySystem.CurrencyType.Star, cost);
        }

        private void LoadCurrentRegionProgress()
        {
            var activeIndex = _regionService != null ? _regionService.CurrentRegionIndex : 0;
            CurrentLevelProgress = GetRegionProgress(activeIndex);
        }

        public void Dispose()
        {
            if (_regionService != null)
            {
                _regionService.OnActiveRegionChanged -= OnActiveRegionChanged;
            }
        }
    }
}
using System;
using Configs;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils.Save;
using Object = UnityEngine.Object;

namespace Services
{
    public class RegionService
    {
        private readonly RegionConfig _regionConfig;
        private readonly Transform _regionParent;

        private BuildingsAnimationConfig _activeRegion;
        private RegionData _activeRegionData;
        private int _currentRegionIndex;

        public event Action<int> OnActiveRegionChanged;

        public RegionConfig RegionConfig => _regionConfig;
        public RegionData ActiveRegionData => _activeRegionData;
        public BuildingsAnimationConfig ActiveRegion => _activeRegion;
        public int CurrentRegionIndex => _currentRegionIndex;

        public RegionService(RegionConfig regionConfig, Transform regionParent)
        {
            _regionConfig = regionConfig;
            _regionParent = regionParent;
        }

        public bool CanLoadNextRegion()
        {
            var nextRegionIndex = _currentRegionIndex + 1;
            return _regionConfig != null && nextRegionIndex >= 0 && nextRegionIndex < _regionConfig.Count;
        }

        public async UniTask Warmup()
        {
            Load();
            LoadRegion(_currentRegionIndex);
            OnActiveRegionChanged?.Invoke(_currentRegionIndex);
            await UniTask.Yield();
        }

        private void Load()
        {
            _currentRegionIndex = PlayerPrefs.GetInt(PlayerPrefsKeys.RegionIndex, 0);
            var maxCount = GetTotalRegionsCount();

            if (maxCount <= 0)
            {
                _currentRegionIndex = -1;
                return;
            }

            if (_currentRegionIndex < 0 || _currentRegionIndex >= maxCount)
            {
                _currentRegionIndex = 0;
                Save();
            }
        }

        private int GetTotalRegionsCount()
        {
            return _regionConfig != null ? _regionConfig.Count : 0;
        }

        private void Save()
        {
            PlayerPrefs.SetInt(PlayerPrefsKeys.RegionIndex, _currentRegionIndex);
            PlayerPrefs.Save();
        }

        public void SetRegion(int index)
        {
            if (_regionConfig == null || index < 0 || index >= _regionConfig.Count)
            {
                Debug.LogWarning($"[RegionService] Cannot set region to index {index}");
                return;
            }

            _currentRegionIndex = index;
            Save();
            LoadRegion(_currentRegionIndex);
            OnActiveRegionChanged?.Invoke(_currentRegionIndex);
        }

        public void LoadNextRegion()
        {
            _currentRegionIndex++;
            Save();
            LoadRegion(_currentRegionIndex);
            OnActiveRegionChanged?.Invoke(_currentRegionIndex);
        }

        public void LoadRegion(int index)
        {
            if (_activeRegion != null)
            {
                Object.Destroy(_activeRegion.gameObject);
                _activeRegion = null;
            }

            _activeRegionData = null;

            if (_regionConfig == null)
            {
                Debug.LogError("[RegionService] RegionConfig is not assigned!");
                return;
            }

            if (index < 0 || index >= _regionConfig.Count)
            {
                Debug.LogWarning($"[RegionService] Invalid region index {index} (Total: {_regionConfig.Count})");
                return;
            }

            _activeRegionData = _regionConfig.GetRegionByIndex(index);

            if (_activeRegionData == null || _activeRegionData.regionPrefab == null)
            {
                Debug.LogWarning($"[RegionService] Region data or regionPrefab is not assigned for index {index}");
                return;
            }

            var instance = Object.Instantiate(_activeRegionData.regionPrefab, _regionParent);
            instance.SetActive(true);
            _activeRegion = instance.GetComponent<BuildingsAnimationConfig>() ??
                            instance.GetComponentInChildren<BuildingsAnimationConfig>(true);

            SetupActiveRegionAnimator(instance);
        }

        private void SetupActiveRegionAnimator(GameObject instance)
        {
            if (_activeRegion == null && instance != null)
            {
                _activeRegion = instance.GetComponent<BuildingsAnimationConfig>() ??
                                instance.GetComponentInChildren<BuildingsAnimationConfig>(true);
            }

            if (_activeRegion != null)
            {
                if (_activeRegion.animator == null && instance != null)
                {
                    _activeRegion.animator = instance.GetComponent<Animator>() ??
                                             instance.GetComponentInChildren<Animator>(true);
                }

                if (_activeRegion.animator != null)
                {
                    _activeRegion.animator.speed = 1f;
                }
            }
        }
    }
}

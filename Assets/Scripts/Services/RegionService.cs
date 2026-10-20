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
        private GameObject _activeInstance;
        private GameObject _startupPrefab;
        private int _currentRegionIndex;

        public RegionConfig RegionConfig => _regionConfig;
        public RegionData ActiveRegionData => _activeRegionData;
        public BuildingsAnimationConfig ActiveRegion => _activeRegion;
        public GameObject ActiveInstance => _activeInstance;
        public int CurrentRegionIndex => _currentRegionIndex;

        public void SetStartupPrefab(GameObject prefab)
        {
            _startupPrefab = prefab;
        }

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
            if (_startupPrefab != null)
            {
                LoadPrefab(_startupPrefab);
            }
            else
            {
                LoadRegion(_currentRegionIndex);
            }

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

        public void LoadNextRegion()
        {
            _currentRegionIndex++;
            LoadRegion(_currentRegionIndex);
            Save();
        }

        public void LoadRegion(int index)
        {
            _activeRegionData = null;

            if (_regionConfig == null)
            {
                Debug.LogError("[RegionService] RegionConfig is not assigned!");
                DestroyActiveInstance();
                return;
            }

            if (index < 0 || index >= _regionConfig.Count)
            {
                Debug.LogWarning($"[RegionService] Invalid region index {index} (Total: {_regionConfig.Count})");
                DestroyActiveInstance();
                return;
            }

            _activeRegionData = _regionConfig.GetRegionByIndex(index);

            if (_activeRegionData == null || _activeRegionData.regionPrefab == null)
            {
                Debug.LogWarning($"[RegionService] Region data or regionPrefab is not assigned for index {index}");
                DestroyActiveInstance();
                return;
            }

            LoadPrefab(_activeRegionData.regionPrefab);
        }

        public void LoadPrefab(GameObject prefab)
        {
            DestroyActiveInstance();

            if (prefab == null)
            {
                Debug.LogWarning("[RegionService] Island prefab is not assigned.");
                return;
            }

            if (_regionParent == null)
            {
                Debug.LogError("[RegionService] Region parent is not assigned.");
                return;
            }

            _activeInstance = Object.Instantiate(prefab, _regionParent);
            _activeInstance.SetActive(true);
            _activeRegion = _activeInstance.GetComponent<BuildingsAnimationConfig>() ??
                            _activeInstance.GetComponentInChildren<BuildingsAnimationConfig>(true);
            SetupActiveRegionAnimator(_activeInstance);
        }

        private void DestroyActiveInstance()
        {
            if (_activeInstance != null)
            {
                Object.Destroy(_activeInstance);
                _activeInstance = null;
            }
            else if (_activeRegion != null)
            {
                Object.Destroy(_activeRegion.gameObject);
            }

            _activeRegion = null;
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

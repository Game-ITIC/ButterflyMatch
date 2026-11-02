using Configs;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Services
{
    // Spawns the active island prefab under the menu world and exposes its build animation config.
    // Which island is active is decided by IslandBuildService (RegionSO/IslandSO flow).
    public class RegionService
    {
        private readonly Transform _regionParent;

        private BuildingsAnimationConfig _activeRegion;
        private GameObject _activeInstance;
        private GameObject _startupPrefab;

        public BuildingsAnimationConfig ActiveRegion => _activeRegion;
        public GameObject ActiveInstance => _activeInstance;

        public RegionService(Transform regionParent)
        {
            _regionParent = regionParent;
        }

        public void SetStartupPrefab(GameObject prefab)
        {
            _startupPrefab = prefab;
        }

        public async UniTask Warmup()
        {
            if (_startupPrefab != null)
            {
                LoadPrefab(_startupPrefab);
            }

            await UniTask.Yield();
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

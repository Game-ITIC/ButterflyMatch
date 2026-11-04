using Cysharp.Threading.Tasks;
using Services;
using UnityEngine;

namespace Initializers
{
    /// <summary>Menu startup of the island model: warm the active island's prefab, then show what is built.</summary>
    internal sealed class CountryIslandStartup
    {
        private readonly RegionService _regionService;
        private readonly IslandBuildService _islandBuildService;

        public CountryIslandStartup(RegionService regionService, IslandBuildService islandBuildService)
        {
            _regionService = regionService;
            _islandBuildService = islandBuildService;
        }

        public async UniTask RunAsync()
        {
            if(_regionService == null)
            {
                return;
            }

            var stepSw = System.Diagnostics.Stopwatch.StartNew();
            if(_islandBuildService != null)
            {
                var activeIsland = _islandBuildService.ActiveIsland;
                if(activeIsland != null)
                {
                    _regionService.SetStartupPrefab(activeIsland.IslandPrefab);
                }
            }

            await _regionService.Warmup();
            Debug.Log($"[CountryInitializer] _regionService.Warmup() finished in {stepSw.ElapsedMilliseconds} ms");

            if(_islandBuildService != null)
            {
                _islandBuildService.EnsureActiveIslandSaved();
                _islandBuildService.Restore();
            }
        }
    }
}

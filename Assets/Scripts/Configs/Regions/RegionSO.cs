using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "Region", menuName = "Configs/Regions/Region")]
    public class RegionSO : ScriptableObject
    {
        [SerializeField] private string regionId;
        [SerializeField] private string displayName;

        [PreviewField(60, ObjectFieldAlignment.Left)]
        [SerializeField] private Sprite icon;

        [ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private IslandSO[] islands;

        public string RegionId => string.IsNullOrEmpty(regionId) ? name : regionId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public Sprite Icon => icon;
        public IReadOnlyList<IslandSO> Islands => islands;
        public int IslandCount => islands != null ? islands.Length : 0;

#if UNITY_EDITOR
        public void Configure(string id, string name, Sprite regionIcon, IslandSO[] islandAssets)
        {
            regionId = id;
            displayName = name;
            icon = regionIcon;
            islands = islandAssets;
        }
#endif

        public IslandSO FindIsland(string islandId)
        {
            if (islands == null || string.IsNullOrEmpty(islandId))
            {
                return null;
            }

            for (var i = 0; i < islands.Length; i++)
            {
                var island = islands[i];
                if (island != null && island.IslandId == islandId)
                {
                    return island;
                }
            }

            return null;
        }

        public IslandSO FirstPlayable()
        {
            if (islands == null)
            {
                return null;
            }

            for (var i = 0; i < islands.Length; i++)
            {
                var island = islands[i];
                if (island != null && !island.IsComingSoon && island.IslandPrefab != null)
                {
                    return island;
                }
            }

            return islands.Length > 0 ? islands[0] : null;
        }
    }
}

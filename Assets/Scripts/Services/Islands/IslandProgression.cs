using System;
using System.Collections.Generic;
using Configs;

namespace Services.Islands
{
    /// <summary>
    /// Island order and unlocking (GDD 5.1): regions in order, islands in region order, one island after another.
    /// Reads the build progress, never changes it.
    /// </summary>
    public sealed class IslandProgression
    {
        private readonly IReadOnlyList<RegionSO> _regions;
        private readonly IslandBuildProgressStore _store;

        public IslandProgression(IReadOnlyList<RegionSO> regions, IslandBuildProgressStore store)
        {
            _regions = regions ?? Array.Empty<RegionSO>();
            _store = store;
        }

        public IReadOnlyList<RegionSO> Regions => _regions;

        /// <summary>All islands in progression order, no coming-soon ones.</summary>
        public List<IslandSO> OrderedIslands()
        {
            var result = new List<IslandSO>();
            for (var r = 0; r < _regions.Count; r++)
            {
                var islands = _regions[r] != null ? _regions[r].Islands : null;
                if (islands == null)
                {
                    continue;
                }

                for (var i = 0; i < islands.Count; i++)
                {
                    var island = islands[i];
                    if (island != null && !island.IsComingSoon && island.IslandPrefab != null)
                    {
                        result.Add(island);
                    }
                }
            }

            return result;
        }

        public int CountBuilt(IslandSO island)
        {
            if (island == null)
            {
                return 0;
            }

            return island.CountBuilt(_store.GetBuilt(island.IslandId));
        }

        public bool IsCompleted(IslandSO island)
        {
            return island != null && island.SlotCount > 0 && CountBuilt(island) >= island.SlotCount;
        }

        /// <summary>
        /// The first island is always open, the next one after the previous is fully built. An island the player
        /// already started stays open (saves from before this rule).
        /// </summary>
        public bool IsUnlocked(IslandSO island)
        {
            if (island == null || island.IsComingSoon)
            {
                return false;
            }

            var ordered = OrderedIslands();
            var index = ordered.IndexOf(island);
            if (index <= 0)
            {
                return index == 0;
            }

            return IsCompleted(ordered[index - 1]) || CountBuilt(island) > 0;
        }

        /// <summary>The first open island that is not finished (the last island when all are finished).</summary>
        public IslandSO CurrentProgressIsland()
        {
            var ordered = OrderedIslands();
            for (var i = 0; i < ordered.Count; i++)
            {
                if (IsUnlocked(ordered[i]) && !IsCompleted(ordered[i]))
                {
                    return ordered[i];
                }
            }

            return ordered.Count > 0 ? ordered[ordered.Count - 1] : null;
        }

        public bool AllIslandsCompleted()
        {
            var ordered = OrderedIslands();
            for (var i = 0; i < ordered.Count; i++)
            {
                if (!IsCompleted(ordered[i]))
                {
                    return false;
                }
            }

            return ordered.Count > 0;
        }

        public RegionSO RegionOf(IslandSO island)
        {
            if (island == null)
            {
                return null;
            }

            for (var i = 0; i < _regions.Count; i++)
            {
                if (_regions[i] != null && _regions[i].FindIsland(island.IslandId) == island)
                {
                    return _regions[i];
                }
            }

            return null;
        }

        public IslandSO FindIsland(string islandId)
        {
            for (var i = 0; i < _regions.Count; i++)
            {
                var island = _regions[i] != null ? _regions[i].FindIsland(islandId) : null;
                if (island != null)
                {
                    return island;
                }
            }

            return null;
        }

        public IslandSO FirstPlayable()
        {
            for (var i = 0; i < _regions.Count; i++)
            {
                var island = _regions[i] != null ? _regions[i].FirstPlayable() : null;
                if (island != null)
                {
                    return island;
                }
            }

            return null;
        }
    }
}

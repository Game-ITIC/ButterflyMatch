using System.Collections.Generic;
using Configs;

namespace Services.Islands
{
    /// <summary>One building card of the catalog: the slot and what the player can do with it.</summary>
    public readonly struct BuildingCatalogEntry
    {
        public BuildingCatalogEntry(BuildingSlot slot, bool built, bool requirementsMet, bool canAfford)
        {
            Slot = slot;
            Built = built;
            RequirementsMet = requirementsMet;
            CanAfford = canAfford;
        }

        public BuildingSlot Slot { get; }
        public bool Built { get; }
        public bool RequirementsMet { get; }
        public bool CanAfford { get; }

        public bool CanBuild => !Built && RequirementsMet && CanAfford;
    }

    /// <summary>
    /// The cards of an island's building catalog in display order: buildable now (cheapest first, so the top
    /// card matches the cost on the menu Build button), then the ones waiting for a requirement, then the built ones.
    /// </summary>
    public static class BuildingCatalogEntries
    {
        private const int GroupBuildable = 0;
        private const int GroupWaiting = 1;
        private const int GroupBuilt = 2;

        public static List<BuildingCatalogEntry> Build(IslandSO island, IslandBuildService buildService)
        {
            var result = new List<BuildingCatalogEntry>();
            var slots = island != null ? island.Slots : null;
            if (slots == null || buildService == null)
            {
                return result;
            }

            var order = new List<(BuildingCatalogEntry Entry, int Group, int Index)>();
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.Building == null)
                {
                    continue;
                }

                var built = buildService.IsBuilt(slot);
                var requirementsMet = buildService.CanSelect(island, slot);
                var entry = new BuildingCatalogEntry(slot, built, requirementsMet, buildService.CanAfford(slot));
                var group = built ? GroupBuilt : requirementsMet ? GroupBuildable : GroupWaiting;
                order.Add((entry, group, i));
            }

            order.Sort((a, b) =>
            {
                var groupCompare = a.Group.CompareTo(b.Group);
                if (groupCompare != 0)
                {
                    return groupCompare;
                }

                var costCompare = a.Group == GroupBuildable ? a.Entry.Slot.StarCost.CompareTo(b.Entry.Slot.StarCost) : 0;
                return costCompare != 0 ? costCompare : a.Index.CompareTo(b.Index);
            });

            for (var i = 0; i < order.Count; i++)
            {
                result.Add(order[i].Entry);
            }

            return result;
        }
    }
}

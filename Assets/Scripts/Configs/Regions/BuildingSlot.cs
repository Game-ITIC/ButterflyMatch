using System;
using System.Collections.Generic;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Configs
{
    [Serializable]
    public class BuildingSlot
    {
        [SerializeField] private string slotId;
        [SerializeField] private BuildingSO building;
        [MinValue(1)]
        [SerializeField] private int starCost = 5;
        [SerializeField] private BuildingSO[] requiredBuildings;
        [SerializeField] private int animationStep;
        [SerializeField] private string objectName;
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Vector3 localEulerAngles;
        [SerializeField] private Vector3 localScale = Vector3.one;

        public string SlotId => slotId;
        public BuildingSO Building => building;
        public int StarCost => starCost;
        public IReadOnlyList<BuildingSO> RequiredBuildings => ListedRequirements();
        public int AnimationStep => animationStep;
        public string ObjectName => objectName;
        public bool IsTree => ContainsTree(building != null ? building.BuildingId : slotId);

#if UNITY_EDITOR
        public static BuildingSlot Create(string id, BuildingSO buildingAsset, int cost, BuildingSO[] required, int step, string animatedObject = null)
        {
            return new BuildingSlot
            {
                slotId = id,
                building = buildingAsset,
                starCost = cost,
                requiredBuildings = required,
                animationStep = step,
                objectName = animatedObject
            };
        }
#endif

        public bool RequirementsMet(IslandSO island, IList<string> builtSlotIds)
        {
            if (IsTree)
            {
                return true;
            }

            var required = QueueRequirements();
            if (required == null || required.Count == 0)
            {
                return true;
            }

            for (var i = 0; i < required.Count; i++)
            {
                var needed = required[i];
                if (needed == null)
                {
                    continue;
                }

                var requiredSlot = island != null ? island.FindSlotByBuilding(needed) : null;
                if (requiredSlot == null || builtSlotIds == null || !builtSlotIds.Contains(requiredSlot.SlotId))
                {
                    return false;
                }
            }

            return true;
        }

        public string RequirementLabel()
        {
            var required = QueueRequirements();
            if (required == null || required.Count == 0)
            {
                return string.Empty;
            }

            var names = new StringBuilder();
            for (var i = 0; i < required.Count; i++)
            {
                var needed = required[i];
                if (needed == null)
                {
                    continue;
                }

                if (names.Length > 0)
                {
                    names.Append(", ");
                }

                names.Append(needed.DisplayName);
            }

            return names.Length == 0 ? string.Empty : "Build " + names + " first";
        }

        private IReadOnlyList<BuildingSO> ListedRequirements()
        {
            if (building != null && building.RequiredBuildings != null && building.RequiredBuildings.Length > 0)
            {
                return building.RequiredBuildings;
            }

            return requiredBuildings;
        }

        private List<BuildingSO> QueueRequirements()
        {
            var listed = ListedRequirements();
            var queue = new List<BuildingSO>();
            if (listed == null)
            {
                return queue;
            }

            for (var i = 0; i < listed.Count; i++)
            {
                var needed = listed[i];
                if (needed == null || ContainsTree(needed.BuildingId))
                {
                    continue;
                }

                queue.Add(needed);
            }

            return queue;
        }

        private static bool ContainsTree(string id)
        {
            return !string.IsNullOrEmpty(id) && id.IndexOf("tree", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

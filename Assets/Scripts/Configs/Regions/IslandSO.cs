using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "Island", menuName = "Configs/Regions/Island")]
    public class IslandSO : ScriptableObject
    {
        [SerializeField] private string islandId;
        [SerializeField] private string displayName;

        [PreviewField(60, ObjectFieldAlignment.Left)]
        [SerializeField] private Sprite icon;

        [SerializeField] private GameObject islandPrefab;
        [SerializeField] private bool isComingSoon;

        [ListDrawerSettings(ShowIndexLabels = true)]
        [SerializeField] private BuildingSlot[] slots;

        public string IslandId => string.IsNullOrEmpty(islandId) ? name : islandId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public Sprite Icon => icon;
        public GameObject IslandPrefab => islandPrefab;
        public bool IsComingSoon => isComingSoon;
        public IReadOnlyList<BuildingSlot> Slots => slots;
        public int SlotCount => slots != null ? slots.Length : 0;

#if UNITY_EDITOR
        public void Configure(string id, string name, Sprite islandIcon, GameObject prefab, bool comingSoon, BuildingSlot[] buildingSlots)
        {
            islandId = id;
            displayName = name;
            icon = islandIcon;
            islandPrefab = prefab;
            isComingSoon = comingSoon;
            slots = buildingSlots;
        }
#endif

        public bool Contains(BuildingSlot slot)
        {
            if (slot == null || slots == null)
            {
                return false;
            }

            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i] == slot)
                {
                    return true;
                }
            }

            return false;
        }

        public BuildingSlot FindSlot(string slotId)
        {
            if (slots == null || string.IsNullOrEmpty(slotId))
            {
                return null;
            }

            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot != null && slot.SlotId == slotId)
                {
                    return slot;
                }
            }

            return null;
        }

        public BuildingSlot FindSlotByBuilding(BuildingSO building)
        {
            if (slots == null || building == null)
            {
                return null;
            }

            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot != null && slot.Building == building)
                {
                    return slot;
                }
            }

            return null;
        }

        public int CountBuilt(IList<string> builtSlotIds)
        {
            if (slots == null || builtSlotIds == null)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot != null && builtSlotIds.Contains(slot.SlotId))
                {
                    count++;
                }
            }

            return count;
        }
    }
}

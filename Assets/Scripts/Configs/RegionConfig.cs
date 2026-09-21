using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = nameof(RegionConfig), menuName = nameof(Configs) + "/" + nameof(RegionConfig), order = 0)]
    public class RegionConfig : ScriptableObject
    {
        [Title("Regions")]
        [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = nameof(RegionData.regionName))]
        [SerializeField] private List<RegionData> regions = new();

        public IReadOnlyList<RegionData> RegionList => regions;
        public int Count => regions != null ? regions.Count : 0;

        public List<string> Regions
        {
            get
            {
                var names = new List<string>();
                if (regions != null)
                {
                    foreach (var r in regions)
                    {
                        names.Add(r != null && !string.IsNullOrEmpty(r.regionName) ? r.regionName : "Soon");
                    }
                }
                return names;
            }
        }

        public RegionData GetRegionByIndex(int index)
        {
            if (regions == null || index < 0 || index >= regions.Count) return null;
            return regions[index];
        }

        public bool HasNextRegion(int currentIndex)
        {
            return regions != null && currentIndex >= 0 && currentIndex + 1 < regions.Count;
        }

        public int GetTotalStepsForRegion(int index)
        {
            var region = GetRegionByIndex(index);
            return region != null ? region.GetTotalSteps() : 0;
        }
    }

    [Serializable]
    public class RegionData
    {
        [LabelText("Display Name")]
        public string regionName = "Japan";

        [PreviewField(60, ObjectFieldAlignment.Left)]
        [LabelText("2D Icon")]
        public Sprite regionIcon;

        [LabelText("3D Island Prefab")]
        public GameObject regionPrefab;

        [LabelText("Is Coming Soon?")]
        public bool isComingSoon = false;

        [LabelText("Star Cost per Upgrade"), HideIf(nameof(isComingSoon)), MinValue(1)]
        public int starCost = 5;

        public int GetTotalSteps()
        {
            if (regionPrefab == null) return 0;
            var config = regionPrefab.GetComponent<BuildingsAnimationConfig>() ??
                         regionPrefab.GetComponentInChildren<BuildingsAnimationConfig>(true);
            return config != null && config.data != null ? config.data.Count : 0;
        }
    }
}
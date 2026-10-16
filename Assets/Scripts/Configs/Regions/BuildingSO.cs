using Sirenix.OdinInspector;
using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "Building", menuName = "Configs/Regions/Building")]
    public class BuildingSO : ScriptableObject
    {
        [SerializeField] private string buildingId;
        [SerializeField] private string displayName;

        [PreviewField(60, ObjectFieldAlignment.Left)]
        [SerializeField] private Sprite icon;

        [SerializeField] private GameObject prefab;

        [LabelText("Must already be built")]
        [SerializeField] private BuildingSO[] requiredBuildings;

        public string BuildingId => string.IsNullOrEmpty(buildingId) ? name : buildingId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public Sprite Icon => icon;
        public GameObject Prefab => prefab;
        public BuildingSO[] RequiredBuildings => requiredBuildings;

#if UNITY_EDITOR
        public void Configure(string id, string name, Sprite buildingIcon, GameObject buildingPrefab, BuildingSO[] required)
        {
            buildingId = id;
            displayName = name;
            icon = buildingIcon;
            prefab = buildingPrefab;
            requiredBuildings = required;
        }
#endif
    }
}

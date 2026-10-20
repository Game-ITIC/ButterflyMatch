using System.Collections.Generic;
using Configs;
using Scopes.Country;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RegionContentSetup
{
    private const string Root = "Assets/Content/Configs/Regions";
    private const string MenuScene = "Assets/_Scenes/MainMenu.unity";

    [MenuItem("Tools/JigsawMatch2/Setup Asia Region Islands")]
    public static void Setup()
    {
        EnsureFolders();

        var lantern = Building("Lantern", "lantern", "Lantern", "Assets/Content/Models/Regions/Asia/Japan/fonar.prefab", Icon(0), null);
        const string housePrefab = "Assets/Content/Prefabs/Regions/China/Korea/KoreaHouse.prefab";
        const string treePrefab = "Assets/Content/Models/Tree/Tree.prefab";

        var japanHouse = Chain("japan", housePrefab, Icon(2),
            Piece(0, "Base", 6),
            Piece(1, "Foundation", 8),
            Piece(2, "Walls", 10),
            Piece(3, "Columns", 12),
            Piece(4, "Goldwork", 14),
            Piece(5, "Beam", 16),
            Piece(6, "Gold Trim", 18),
            Piece(7, "Roof Edge", 20),
            Piece(8, "Ladder", 22),
            Piece(9, "Roof", 24));
        var japan = Island("JapanIsland", "japan", "Japan", "Assets/Content/Models/Regions/Asia/RegionPrefabs/Japan_Island_Pref.prefab", Icon(0),
            Merge(
                japanHouse,
                new[] { BuildingSlot.Create("japan-lantern", lantern, 5, new[] { japanHouse[1].Building }, 1) },
                Trees("japan", treePrefab, Icon(0), 8, "Tree 01", "Tree 01 (1)", "Tree 01 (2)")));

        var chinaFoundation = Stack("china", housePrefab, Icon(1), Named("Foundation", "Cube.004", 8));
        var chinaBuildingAsset = Building("chinaBuilding", "china-building", "Building", housePrefab, Icon(1), new[] { chinaFoundation[0].Building });
        var chinaBuilding = BuildingSlot.CreateGroup(
            "china-building",
            chinaBuildingAsset,
            20,
            new[] { chinaFoundation[0].Building },
            new[] { "Cube", "Cube.019", "Cube.013", "Cube.017", "Cube.012", "Cube.007", "Cube.021", "Cube.005", "Cube.009", "Cube.015", "Cube.006" });
        var chinaRoofAsset = Building("chinaRoof", "china-roof", "Roof", housePrefab, Icon(1), new[] { chinaBuildingAsset });
        var chinaRoof = BuildingSlot.Create("china-roof", chinaRoofAsset, 18, new[] { chinaBuildingAsset }, 0, "Cube.001");
        var china = Island("ChinaIsland", "china", "China", "Assets/Content/Models/Regions/Asia/China/ChinaPrefabs.prefab", Icon(1),
            Merge(
                chinaFoundation,
                new[] { chinaBuilding, chinaRoof },
                Trees("china", treePrefab, Icon(1), 15, "Tree 01", "Tree 01 (1)")));

        var koreaHouse = Stack("korea", housePrefab, Icon(2),
            Named("Foundation", "pagoda_part2", 20),
            Named("Walls", "pagoda_part1", 22),
            Named("Roof", "pagoda_part3", 24),
            Named("Bridge", "bridge", 26));
        var korea = Island("KoreaIsland", "korea", "Korea", "Assets/Content/Models/Regions/Asia/RegionPrefabs/Korea_Environment_Pref.prefab", Icon(2),
            Merge(
                koreaHouse,
                new[] { BuildingSlot.Create("korea-lantern", lantern, 20, new[] { koreaHouse[0].Building }, 3) },
                Trees("korea", treePrefab, Icon(2), 25, "Tree 01", "Tree 01 (1)")));

        var region = LoadOrCreate<RegionSO>($"{Root}/AsiaRegion.asset");
        region.Configure("asia", "Asia", Icon(0), new[] { japan, china, korea });
        EditorUtility.SetDirty(region);
        AssignRegion(region);
        AssetDatabase.SaveAssets();
    }

    private readonly struct Stage
    {
        public Stage(int step, string name, int cost)
        {
            Step = step;
            Name = name;
            Cost = cost;
        }

        public int Step { get; }
        public string Name { get; }
        public int Cost { get; }
    }

    private readonly struct Part
    {
        public Part(string name, string objectName, int cost)
        {
            Name = name;
            ObjectName = objectName;
            Cost = cost;
        }

        public string Name { get; }
        public string ObjectName { get; }
        public int Cost { get; }
    }

    private static Stage Piece(int step, string name, int cost)
    {
        return new Stage(step, name, cost);
    }

    private static Part Named(string name, string objectName, int cost)
    {
        return new Part(name, objectName, cost);
    }

    private static BuildingSlot[] Chain(string islandId, string prefabPath, Sprite icon, params Stage[] stages)
    {
        var slots = new BuildingSlot[stages.Length];
        BuildingSO previous = null;
        for (var i = 0; i < stages.Length; i++)
        {
            var stage = stages[i];
            var key = stage.Name.Replace(" ", string.Empty);
            var building = Building(
                islandId + key,
                islandId + "-" + key.ToLowerInvariant(),
                stage.Name,
                prefabPath,
                icon,
                previous == null ? null : new[] { previous });
            slots[i] = BuildingSlot.Create(
                islandId + "-" + key.ToLowerInvariant(),
                building,
                stage.Cost,
                previous == null ? null : new[] { previous },
                stage.Step);
            previous = building;
        }

        return slots;
    }

    private static BuildingSlot[] Stack(string islandId, string prefabPath, Sprite icon, params Part[] parts)
    {
        var slots = new BuildingSlot[parts.Length];
        BuildingSO previous = null;
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var key = part.Name.Replace(" ", string.Empty);
            var required = previous == null ? null : new[] { previous };
            var building = Building(islandId + key, islandId + "-" + key.ToLowerInvariant(), part.Name, prefabPath, icon, required);
            slots[i] = BuildingSlot.Create(
                islandId + "-" + key.ToLowerInvariant(),
                building,
                part.Cost,
                required,
                0,
                part.ObjectName);
            previous = building;
        }

        return slots;
    }

    private static BuildingSlot[] Trees(string islandId, string prefabPath, Sprite icon, int cost, params string[] objectNames)
    {
        var slots = new BuildingSlot[objectNames.Length];
        for (var i = 0; i < objectNames.Length; i++)
        {
            var number = (i + 1).ToString();
            var building = Building(
                islandId + "Tree" + number,
                islandId + "-tree" + number,
                "Tree " + number,
                prefabPath,
                icon,
                null);
            slots[i] = BuildingSlot.Create(islandId + "-tree" + number, building, cost, null, -1, objectNames[i]);
        }

        return slots;
    }

    private static BuildingSlot[] Merge(params BuildingSlot[][] groups)
    {
        var slots = new List<BuildingSlot>();
        for (var i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null)
            {
                slots.AddRange(groups[i]);
            }
        }

        return slots.ToArray();
    }

    private static BuildingSO Building(string fileName, string id, string displayName, string prefabPath, Sprite icon, BuildingSO[] required)
    {
        var building = LoadOrCreate<BuildingSO>($"{Root}/Buildings/{fileName}.asset");
        building.Configure(id, displayName, icon, AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), required);
        EditorUtility.SetDirty(building);
        return building;
    }

    private static IslandSO Island(string fileName, string id, string displayName, string prefabPath, Sprite icon, params BuildingSlot[] slots)
    {
        var island = LoadOrCreate<IslandSO>($"{Root}/Islands/{fileName}.asset");
        island.Configure(id, displayName, icon, AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), false, slots);
        EditorUtility.SetDirty(island);
        return island;
    }

    private static void AssignRegion(RegionSO region)
    {
        var scene = EditorSceneManager.GetSceneByPath(MenuScene);
        if (!scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Additive);
        }

        var scopes = Resources.FindObjectsOfTypeAll<BaseCountryLifetimeScope>();
        for (var i = 0; i < scopes.Length; i++)
        {
            var scope = scopes[i];
            if (scope == null || scope.gameObject.scene != scene)
            {
                continue;
            }

            var serialized = new SerializedObject(scope);
            serialized.FindProperty("regionSO").objectReferenceValue = region;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(scope);
        }

        EditorSceneManager.SaveScene(scene);
    }

    private static Sprite Icon(int index)
    {
        var config = AssetDatabase.LoadAssetAtPath<RegionConfig>("Assets/Content/Configs/RegionConfig.asset");
        var region = config != null ? config.GetRegionByIndex(index) : null;
        return region != null ? region.regionIcon : null;
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolders()
    {
        CreateFolder("Assets/Content/Configs", "Regions");
        CreateFolder(Root, "Buildings");
        CreateFolder(Root, "Islands");
    }

    private static void CreateFolder(string parent, string folder)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + folder))
        {
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}

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
        foreach (var island in region.Islands) IslandPricing.Apply(island); // GDD 5.2 prices
        AssignRegion(region, 0);
        AssetDatabase.SaveAssets();
    }

    private const string NorthModels = "Assets/Content/Models/Regions/North";

    // North islands come from NorthRegionBuilder (Tools/Regions/Build North Region Islands): each prefab has
    // ten animation steps S01..S10, so every island is a linear chain of ten build steps like Japan.
    [MenuItem("Tools/JigsawMatch2/Setup North Region Islands")]
    public static void SetupNorth()
    {
        EnsureFolders();

        var norway = NorthIsland("NorwayIsland", "norway", "Norway", "VikingCamp", 10,
            "Palisade", "Gate", "Longhouse", "Longhouse Roof", "Tents",
            "Watchtower", "Banner", "Training Yard", "Fire Pits", "Pines");
        var lapland = NorthIsland("LaplandIsland", "lapland", "Lapland", "FrostCamp", 12,
            "Hut", "Hut Roof", "Antlers", "Cart", "Barrels",
            "Campfire", "Spit", "Benches", "Clothesline", "Pines");
        var iceland = NorthIsland("IcelandIsland", "iceland", "Iceland", "FrostKeep", 14,
            "Stone Hall", "Timber Hall", "Hall Roof", "Hall Banner", "Side House",
            "Boardwalk", "Railing", "Rim Banners", "Flag Poles", "Boulders");

        var region = LoadOrCreate<RegionSO>($"{Root}/NorthRegion.asset");
        region.Configure("north", "North", norway.Icon, new[] { norway, lapland, iceland });
        EditorUtility.SetDirty(region);
        foreach (var island in region.Islands) IslandPricing.Apply(island); // GDD 5.2 prices
        AssignRegion(region, 1);
        AssetDatabase.SaveAssets();
    }

    private const string EuropeModels = "Assets/Content/Models/Regions/Europe";

    // Europe islands come from EuropeRegionBuilder (Tools/Regions/Build Europe Region Islands), same ten-step layout as North.
    [MenuItem("Tools/JigsawMatch2/Setup Europe Region Islands")]
    public static void SetupEurope()
    {
        EnsureFolders();

        var france = BlenderIsland(EuropeModels, "FranceIsland", "france", "France", "ParisCafe", 16,
            "Paths", "Cafe", "Upper Floors", "Mansard Roof", "Tower Base",
            "Tower Top", "Terrace", "Street Lamps", "Bicycle", "Gardens");
        var greece = BlenderIsland(EuropeModels, "GreeceIsland", "greece", "Greece", "SantoriniChapel", 18,
            "Terrace", "Chapel", "Dome", "Bell Tower", "Cliff Stairs",
            "Pergola", "Dining Table", "Balcony", "Bougainvillea", "Olive Tree");
        var italy = BlenderIsland(EuropeModels, "ItalyIsland", "italy", "Italy", "PisaPlaza", 20,
            "Plaza", "Tower Base", "Tower Top", "Pizzeria", "Tile Roof",
            "Awning", "String Lights", "Scooter", "Cypress", "Planters");

        var region = LoadOrCreate<RegionSO>($"{Root}/EuropeRegion.asset");
        region.Configure("europe", "Europe", france.Icon, new[] { france, greece, italy });
        EditorUtility.SetDirty(region);
        foreach (var island in region.Islands) IslandPricing.Apply(island); // GDD 5.2 prices
        AssignRegion(region, 2);
        AssetDatabase.SaveAssets();
    }

    private const string AfricaModels = "Assets/Content/Models/Regions/Africa";
    private const string LatinAmericaModels = "Assets/Content/Models/Regions/LatinAmerica";
    private const string MenuBackgrounds = "Assets/Content/UI/Background";

    // Africa islands come from AfricaRegionBuilder (Tools/Regions/Build Africa Region Islands), same ten-step layout as Europe.
    [MenuItem("Tools/JigsawMatch2/Setup Africa Region Islands")]
    public static void SetupAfrica()
    {
        EnsureFolders();

        var egypt = BuiltIsland(AfricaModels, "EgyptIsland", "egypt", "Egypt", "GizaPyramids",
            "Stone Path", "Pyramid Base", "Pyramid Top", "Temple Gate", "Sphinx Body",
            "Sphinx Head", "Obelisks", "Oasis Pool", "Palm Trees", "Market Tent");
        var morocco = BuiltIsland(AfricaModels, "MoroccoIsland", "morocco", "Morocco", "MarrakechMedina",
            "Courtyard", "Medina Walls", "Medina Gate", "Minaret", "Minaret Top",
            "Tiled Fountain", "Riad House", "Market Stalls", "Palm Trees", "Flower Pots");
        var southAfrica = BuiltIsland(AfricaModels, "SouthAfricaIsland", "southafrica", "South Africa", "CapeTownHarbor",
            "Harbor Pier", "Table Mountain", "Lion's Head", "Cape Dutch House", "Thatched Roof",
            "Lighthouse", "Sailboat", "Umbrella Trees", "Protea Garden", "Harbor Lamps");

        SetupRegion("AfricaRegion", "africa", "Africa", $"{MenuBackgrounds}/AfricaMenuBackground.png", 3, egypt, morocco, southAfrica);
    }

    // Latin America islands come from LatinAmericaRegionBuilder (Tools/Regions/Build Latin America Region Islands).
    [MenuItem("Tools/JigsawMatch2/Setup Latin America Region Islands")]
    public static void SetupLatinAmerica()
    {
        EnsureFolders();

        var mexico = BuiltIsland(LatinAmericaModels, "MexicoIsland", "mexico", "Mexico", "ChichenItza",
            "Plaza", "Pyramid Base", "Pyramid Top", "Grand Stairway", "Temple",
            "Stelae", "Cenote", "Agave Garden", "Jungle Trees", "Flower Bushes");
        var peru = BuiltIsland(LatinAmericaModels, "PeruIsland", "peru", "Peru", "MachuPicchu",
            "Lower Terraces", "Upper Terraces", "Huayna Picchu", "Stone Houses", "Thatched Roofs",
            "Sun Temple", "Stone Stairs", "Water Channel", "Textile Stall", "Llamas");
        var brazil = BuiltIsland(LatinAmericaModels, "BrazilIsland", "brazil", "Brazil", "RioDeJaneiro",
            "Beach", "Hillside", "Statue Pedestal", "Redeemer Statue", "Sugarloaf",
            "Cable Car", "Colorful Houses", "Boardwalk", "Palm Trees", "Jungle Trees");

        SetupRegion("LatinAmericaRegion", "latin_america", "Latin America", $"{MenuBackgrounds}/LatinAmericaMenuBackground.png", 4, mexico, peru, brazil);
    }

    // Writes the region asset from the islands whose Blender prefab exists, prices them (GDD 5.2) and puts the region
    // at its progression slot in the MainMenu scope.
    private static void SetupRegion(string fileName, string id, string displayName, string backgroundPath, int order, params IslandSO[] islands)
    {
        var built = new List<IslandSO>();
        foreach (var island in islands)
        {
            if (island != null) built.Add(island);
        }

        if (built.Count == 0)
        {
            Debug.LogWarning($"[RegionContentSetup] {displayName}: no island prefabs built yet, region not written.");
            return;
        }

        var region = LoadOrCreate<RegionSO>($"{Root}/{fileName}.asset");
        region.Configure(id, displayName, built[0].Icon, built.ToArray());
        var background = AssetDatabase.LoadAssetAtPath<Sprite>(backgroundPath);
        if (background != null)
        {
            var so = new SerializedObject(region);
            so.FindProperty("menuBackground").objectReferenceValue = background;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning($"[RegionContentSetup] {displayName}: menu background {backgroundPath} not found.");
        }

        EditorUtility.SetDirty(region);
        foreach (var island in region.Islands) IslandPricing.Apply(island); // GDD 5.2 prices
        AssignRegion(region, order);
        AssetDatabase.SaveAssets();
    }

    // Like BlenderIsland, but returns null (and writes nothing) while the island prefab has not been built yet.
    private static IslandSO BuiltIsland(string modelsRoot, string fileName, string id, string displayName, string modelId, params string[] stepNames)
    {
        var prefabPath = $"{modelsRoot}/{modelId}/{modelId}_Island_Pref.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            Debug.LogWarning($"[RegionContentSetup] {displayName}: {prefabPath} not built yet, skipped.");
            return null;
        }

        // Placeholder costs; IslandPricing writes the GDD prices afterwards.
        return BlenderIsland(modelsRoot, fileName, id, displayName, modelId, 4, stepNames);
    }

    private static IslandSO NorthIsland(string fileName, string id, string displayName, string modelId, int firstCost, params string[] stepNames)
    {
        return BlenderIsland(NorthModels, fileName, id, displayName, modelId, firstCost, stepNames);
    }

    private static IslandSO BlenderIsland(string modelsRoot, string fileName, string id, string displayName, string modelId, int firstCost, params string[] stepNames)
    {
        var prefabPath = $"{modelsRoot}/{modelId}/{modelId}_Island_Pref.prefab";
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{modelsRoot}/{modelId}/{modelId}_Icon.png");
        var stages = new Stage[stepNames.Length];
        for (var i = 0; i < stepNames.Length; i++)
        {
            stages[i] = Piece(i, stepNames[i], firstCost + i * 2);
        }

        return Island(fileName, id, displayName, prefabPath, icon, Chain(id, prefabPath, icon, stages));
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
            var key = stage.Name.Replace(" ", string.Empty).Replace("'", string.Empty); // "Lion's Head" -> LionsHead
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

    // Puts the region at its progression slot in the MainMenu scope's ordered "regions" list.
    private static void AssignRegion(RegionSO region, int order)
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
            var list = serialized.FindProperty("regions");
            for (var r = list.arraySize - 1; r >= 0; r--)
            {
                if (list.GetArrayElementAtIndex(r).objectReferenceValue == region)
                {
                    list.DeleteArrayElementAtIndex(r);
                }
            }

            if (list.arraySize <= order)
            {
                list.arraySize = order + 1;
            }
            else
            {
                list.InsertArrayElementAtIndex(order);
            }

            list.GetArrayElementAtIndex(order).objectReferenceValue = region;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(scope);
        }

        EditorSceneManager.SaveScene(scene);
    }

    // Asia island icons: 0 = Japan, 1 = China, 2 = Korea.
    private const string AsiaIconSheet = "Assets/Content/UI/Regions/Сгенерированное изображение 1 (2).png";
    private static readonly string[] AsiaIconSprites =
    {
        "Сгенерированное изображение 1 (2)_2",
        "Сгенерированное изображение 1 (2)_0",
        "Сгенерированное изображение 1 (2)_1",
    };

    private static Sprite Icon(int index)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(AsiaIconSheet);
        for (var i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite && sprite.name == AsiaIconSprites[index])
            {
                return sprite;
            }
        }

        return null;
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

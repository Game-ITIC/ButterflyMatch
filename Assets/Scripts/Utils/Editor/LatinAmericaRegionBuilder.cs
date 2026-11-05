#if UNITY_EDITOR
using UnityEditor;

namespace Utils.Editor
{
    /// <summary>
    /// Builds the "Latin America" region islands (Blender exports from ArtSource/LatinAmerica, see
    /// ArtSource/LatinAmerica/scripts/run.py) into region prefabs with the same pipeline as the North/Europe regions,
    /// then writes their IslandSO/RegionSO configs.
    /// </summary>
    public static class LatinAmericaRegionBuilder
    {
        private const string RootDir = "Assets/Content/Models/Regions/LatinAmerica";

        // Modelled with the Europe conventions (front corner towards the camera, Blender preview azimuth -50).
        private const float Yaw = 16f;

        private static readonly NorthRegionBuilder.IslandDef[] Islands =
        {
            new NorthRegionBuilder.IslandDef { Root = RootDir, Id = "ChichenItza", Display = "Mexico", Yaw = Yaw },
            new NorthRegionBuilder.IslandDef { Root = RootDir, Id = "MachuPicchu", Display = "Peru", Yaw = Yaw },
            new NorthRegionBuilder.IslandDef { Root = RootDir, Id = "RioDeJaneiro", Display = "Brazil", Yaw = Yaw },
        };

        [MenuItem("Tools/Regions/Build Latin America Region Islands")]
        public static void BuildAllMenu()
        {
            BuildAll();
        }

        public static string BuildAll()
        {
            var log = NorthRegionBuilder.BuildIslands(Islands);
            RegionContentSetup.SetupLatinAmerica();
            return log;
        }
    }
}
#endif

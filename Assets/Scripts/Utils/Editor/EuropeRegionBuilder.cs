#if UNITY_EDITOR
using UnityEditor;

namespace Utils.Editor
{
    /// <summary>
    /// Builds the "Europe" region islands (Blender exports from ArtSource/Europe, see ArtSource/Europe/scripts/run.py)
    /// into region prefabs with the same pipeline as the North region, then writes their IslandSO/RegionSO configs.
    /// </summary>
    public static class EuropeRegionBuilder
    {
        private const string RootDir = "Assets/Content/Models/Regions/Europe";

        // Europe islands are modelled to be seen across their front corner, which is 28 degrees further round
        // than the North islands (Blender preview azimuth -50 vs -78).
        private const float Yaw = 16f;

        private static readonly NorthRegionBuilder.IslandDef[] Islands =
        {
            new NorthRegionBuilder.IslandDef { Root = RootDir, Id = "ParisCafe", Display = "France", Yaw = Yaw },
            new NorthRegionBuilder.IslandDef { Root = RootDir, Id = "SantoriniChapel", Display = "Greece", Yaw = Yaw },
            new NorthRegionBuilder.IslandDef { Root = RootDir, Id = "PisaPlaza", Display = "Italy", Yaw = Yaw },
        };

        [MenuItem("Tools/Regions/Build Europe Region Islands")]
        public static void BuildAllMenu()
        {
            BuildAll();
        }

        public static string BuildAll()
        {
            var log = NorthRegionBuilder.BuildIslands(Islands);
            RegionContentSetup.SetupEurope();
            return log;
        }
    }
}
#endif

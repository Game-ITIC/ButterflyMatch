#if UNITY_EDITOR
using UnityEditor;

namespace Utils.Editor
{
    /// <summary>
    /// Builds the "Africa" region islands (Blender exports from ArtSource/Africa, see ArtSource/Africa/scripts/run.py)
    /// into region prefabs with the same pipeline as the North/Europe regions, then writes their IslandSO/RegionSO configs.
    /// </summary>
    public static class AfricaRegionBuilder
    {
        private const string RootDir = "Assets/Content/Models/Regions/Africa";

        // Modelled with the Europe conventions (front corner towards the camera, Blender preview azimuth -50).
        private const float Yaw = 16f;

        private static readonly NorthRegionBuilder.IslandDef[] Islands =
        {
            new NorthRegionBuilder.IslandDef { Root = RootDir, Id = "GizaPyramids", Display = "Egypt", Yaw = Yaw },
            new NorthRegionBuilder.IslandDef { Root = RootDir, Id = "MarrakechMedina", Display = "Morocco", Yaw = Yaw },
            new NorthRegionBuilder.IslandDef { Root = RootDir, Id = "CapeTownHarbor", Display = "South Africa", Yaw = Yaw },
        };

        [MenuItem("Tools/Regions/Build Africa Region Islands")]
        public static void BuildAllMenu()
        {
            BuildAll();
        }

        public static string BuildAll()
        {
            var log = NorthRegionBuilder.BuildIslands(Islands);
            RegionContentSetup.SetupAfrica();
            return log;
        }
    }
}
#endif

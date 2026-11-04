using System.Collections.Generic;
using JuiceFresh;
using UnityEngine;

namespace JuiceFresh.Scripts
{
    /// <summary>
    /// Finds which level to play and reads its data from Resources/Levels/&lt;number&gt;.txt.
    /// </summary>
    public sealed class LevelLoaderService
    {
        private const string OpenLevelKey = "OpenLevel";
        private const string LevelsFolder = "Levels/";

        /// <summary>The level the player opened, mapped into the playable range (levels loop after the last one).</summary>
        public int ResolveOpenLevel()
        {
            int level = PlayerPrefs.GetInt(OpenLevelKey);
            if (level == 0)
                level = 1;

            return LevelProgressionHelper.CalculateLevel(level);
        }

        public LevelData Load(int level, IReadOnlyList<CollectedIngredients> collectedIngredients)
        {
            TextAsset mapText = Resources.Load<TextAsset>(LevelsFolder + level);
            if (mapText == null)
            {
                Debug.LogError($"[LevelLoader] Level file Resources/{LevelsFolder}{level} not found.");
                return null;
            }

            return LevelParserService.Parse(mapText.text, collectedIngredients);
        }
    }
}

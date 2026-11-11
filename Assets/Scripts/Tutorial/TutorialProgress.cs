using UnityEngine;

namespace GameTutorials
{
    /// <summary>Which tutorials the player has finished. One PlayerPrefs flag per tutorial id.</summary>
    public static class TutorialProgress
    {
        public const string Chain = "chain";
        public const string Moves = "moves";
        public const string Score = "score";
        public const string Goals = "goals";

        private const string KeyPrefix = "tutorial-done-";

        private static readonly string[] FixedIds = { Chain, Moves, Score, Goals };

        public static string Booster(BoostType type) => "boost-" + type;

        public static bool IsDone(string id) => PlayerPrefs.GetInt(KeyPrefix + id, 0) == 1;

        public static void MarkDone(string id)
        {
            PlayerPrefs.SetInt(KeyPrefix + id, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Shows every tutorial again (cheat panel).</summary>
        public static void ResetAll()
        {
            foreach (string id in FixedIds)
                PlayerPrefs.DeleteKey(KeyPrefix + id);

            foreach (BoostType type in System.Enum.GetValues(typeof(BoostType)))
                PlayerPrefs.DeleteKey(KeyPrefix + Booster(type));

            PlayerPrefs.Save();
        }
    }
}

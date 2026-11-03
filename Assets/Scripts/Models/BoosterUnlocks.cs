using Providers;
using UnityEngine;

namespace Models
{
    /// <summary>
    /// Boosters open with progress (Docs/GDD.md 7): Shovel 6, Energy 10, Bomb 14, +5 moves 18.
    /// On unlock the player gets 2 of that booster for free, once.
    /// </summary>
    public static class BoosterUnlocks
    {
        public const int FreeOnUnlock = 2;
        private const string GrantedKeyPrefix = "booster-unlock-granted-";

        public static int UnlockLevel(BoostType type)
        {
            switch (type)
            {
                case BoostType.Shovel: return 6;
                case BoostType.Energy: return 10;
                case BoostType.Bomb: return 14;
                case BoostType.ExtraMoves: return 18;
                default: return 1;
            }
        }

        public static bool IsUnlocked(BoostType type, int level) => level >= UnlockLevel(type);

        /// <summary>Gives the free boosters the first time the booster is open. Returns true if it just unlocked.</summary>
        public static bool GrantIfJustUnlocked(BoostType type, int level, BoostersProvider provider)
        {
            var key = GrantedKeyPrefix + (int)type;
            if (provider == null || !IsUnlocked(type, level) || PlayerPrefs.GetInt(key, 0) == 1)
            {
                return false;
            }

            PlayerPrefs.SetInt(key, 1);
            provider.GetBoosterModel(type).Add(FreeOnUnlock);
            provider.Save();
            return true;
        }
    }
}

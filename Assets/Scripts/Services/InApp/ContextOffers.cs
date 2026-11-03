using System;
using System.Globalization;
using Gley.EasyIAP;
using UnityEngine;
using Utils.Save;

namespace Services.InApp
{
    /// <summary>
    /// When the contextual offers from Docs/GDD.md 11.3 may be shown. State lives in PlayerPrefs, all time is UTC.
    /// </summary>
    public static class ContextOffers
    {
        public const int StarterOfferLevel = 5;
        public static readonly TimeSpan StarterOfferDuration = TimeSpan.FromHours(48);

        private const string StarterStartKey = "offer-starter-start";
        private const string HardLevelKey = "offer-hard-level";
        private const string HardLevelShownKey = "offer-hard-level-shown";
        private const string LossLevelKey = "offer-loss-level";
        private const string LossCountKey = "offer-loss-count";

        private static int OpenLevel => PlayerPrefs.GetInt("OpenLevel", 1);

        /// <summary>Starter bundle: after level 5, for 48 hours from the first time it could be seen (one purchase ever).</summary>
        public static bool IsStarterAvailable()
        {
            if (OpenLevel <= StarterOfferLevel)
            {
                return false;
            }

            var start = PlayerPrefs.GetString(StarterStartKey, string.Empty);
            if (!long.TryParse(start, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
            {
                PlayerPrefs.SetString(StarterStartKey, DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));
                PlayerPrefs.Save();
                return true;
            }

            return DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc) < StarterOfferDuration;
        }

        /// <summary>Time left on the starter offer, for a timer label.</summary>
        public static TimeSpan StarterTimeLeft()
        {
            var start = PlayerPrefs.GetString(StarterStartKey, string.Empty);
            if (!long.TryParse(start, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
            {
                return StarterOfferDuration;
            }

            var left = StarterOfferDuration - (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc));
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }

        /// <summary>"1 per day" offers (need moves, out of lives).</summary>
        public static bool IsAvailableToday(ShopProductNames product) => DailyCounter.IsBelow(DailyKey(product), 1);

        public static void MarkUsedToday(ShopProductNames product) => DailyCounter.Increment(DailyKey(product));

        private static string DailyKey(ShopProductNames product) => "offer-daily-" + product;

        /// <summary>Called on every lose: counts losses in a row on the same level.</summary>
        public static void RegisterLoss(int level)
        {
            var count = PlayerPrefs.GetInt(LossLevelKey, -1) == level ? PlayerPrefs.GetInt(LossCountKey, 0) + 1 : 1;
            PlayerPrefs.SetInt(LossLevelKey, level);
            PlayerPrefs.SetInt(LossCountKey, count);

            // Hard level offer: 2nd lose in a row on one level, once per level.
            if (count >= 2 && PlayerPrefs.GetInt(HardLevelShownKey, -1) != level)
            {
                PlayerPrefs.SetInt(HardLevelKey, level);
            }

            PlayerPrefs.Save();
        }

        public static void RegisterWin()
        {
            PlayerPrefs.SetInt(LossCountKey, 0);
            PlayerPrefs.DeleteKey(HardLevelKey);
            PlayerPrefs.Save();
        }

        /// <summary>The hard level offer is up while the player is stuck on that level.</summary>
        public static bool IsHardLevelAvailable()
        {
            var level = PlayerPrefs.GetInt(HardLevelKey, -1);
            return level > 0 && level == OpenLevel;
        }

        public static void MarkHardLevelUsed()
        {
            PlayerPrefs.SetInt(HardLevelShownKey, PlayerPrefs.GetInt(HardLevelKey, -1));
            PlayerPrefs.DeleteKey(HardLevelKey);
            PlayerPrefs.Save();
        }
    }
}

using System;
using System.Globalization;
using UnityEngine;

namespace Utils.Save
{
    /// <summary>A per-day counter in PlayerPrefs that resets at 00:00 UTC (daily ad/offer limits).</summary>
    public static class DailyCounter
    {
        private static string Today => DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        public static int Get(string key)
        {
            return PlayerPrefs.GetString(key + "-day", string.Empty) == Today ? PlayerPrefs.GetInt(key + "-count", 0) : 0;
        }

        public static void Increment(string key)
        {
            var value = Get(key) + 1;
            PlayerPrefs.SetString(key + "-day", Today);
            PlayerPrefs.SetInt(key + "-count", value);
            PlayerPrefs.Save();
        }

        public static bool IsBelow(string key, int limit) => Get(key) < limit;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Services
{
    /// <summary>
    /// The analytics events from Docs/GDD.md 13 in one place. The GameAnalytics SDK is not installed yet
    /// (only Resources/GameAnalytics/Settings.asset is in the project): plug a backend into <see cref="Sink"/>
    /// at startup, e.g. Sink = (name, fields) => GameAnalytics.NewDesignEvent(name, ...).
    /// Without a sink the events are only logged in the Editor / development builds.
    /// </summary>
    public static class GameAnalyticsEvents
    {
        public static Action<string, IReadOnlyDictionary<string, object>> Sink;

        private static void Send(string name, Dictionary<string, object> fields = null)
        {
            fields ??= new Dictionary<string, object>();

            try
            {
                Sink?.Invoke(name, fields);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var parts = new List<string>(fields.Count);
            foreach (var pair in fields)
            {
                parts.Add(pair.Key + "=" + pair.Value);
            }

            Debug.Log("[Analytics] " + name + " " + string.Join(", ", parts));
#endif
        }

        public static void LevelStart(int level) =>
            Send("level_start", new Dictionary<string, object> { { "level", level } });

        public static void LevelWin(int level, int movesLeft, int stars, int boostersUsed) =>
            Send("level_win", new Dictionary<string, object>
            {
                { "level", level }, { "moves_left", movesLeft }, { "stars", stars }, { "boosters", boostersUsed }
            });

        public static void LevelFail(int level, int boostersUsed) =>
            Send("level_fail", new Dictionary<string, object> { { "level", level }, { "boosters", boostersUsed } });

        /// <param name="source">"ad" or "gems".</param>
        public static void ContinueUsed(int level, string source, int gems = 0) =>
            Send("continue_used", new Dictionary<string, object> { { "level", level }, { "source", source }, { "gems", gems } });

        public static void AdRewardedShown(string placement) =>
            Send("ad_rewarded_shown", new Dictionary<string, object> { { "placement", placement } });

        public static void AdRewardedCompleted(string placement) =>
            Send("ad_rewarded_completed", new Dictionary<string, object> { { "placement", placement } });

        public static void AdInterstitialShown() => Send("ad_interstitial_shown");

        public static void IapPurchase(string productId, string price) =>
            Send("iap_purchase", new Dictionary<string, object> { { "product", productId }, { "price", price } });

        public static void GemsSpent(string what, int amount) =>
            Send("gems_spent", new Dictionary<string, object> { { "what", what }, { "amount", amount } });

        public static void BuildingBuilt(string island, string slot) =>
            Send("building_built", new Dictionary<string, object> { { "island", island }, { "slot", slot } });

        public static void IslandCompleted(string island) =>
            Send("island_completed", new Dictionary<string, object> { { "island", island } });
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace GameLocalization
{
    /// <summary>
    /// Thin wrapper over the Unity Localization string database (table "UI") for code-composed strings.
    /// Values are cached per locale; the cache is cleared when the selected locale changes.
    /// </summary>
    public static class L10n
    {
        public const string Table = "UI";

        private static readonly Dictionary<string, string> Cache = new();
        private static bool _subscribed;

        /// <summary>Raised after the selected locale changed (cache already cleared).</summary>
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Cache.Clear();
            ReverseMap.Clear();
            Changed = null;
            _subscribed = false;
        }

        public static string Get(string key) => Get(key, key);

        public static string Get(string key, string fallback)
        {
            if (string.IsNullOrEmpty(key))
                return fallback;

            if (!EnsureReady())
                return fallback;

            if (Cache.TryGetValue(key, out var cached))
                return cached;

            string value = null;
            try
            {
                var entry = LocalizationSettings.StringDatabase.GetTableEntry(Table, key).Entry;
                if (entry != null && !string.IsNullOrEmpty(entry.Value))
                    value = entry.Value;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[L10n] Lookup failed for '{key}': {e.Message}");
            }

            value ??= fallback;
            Cache[key] = value;
            return value;
        }

        public static string Format(string key, params object[] args)
        {
            var pattern = Get(key);
            try
            {
                return string.Format(pattern, args);
            }
            catch (FormatException)
            {
                return pattern;
            }
        }

        /// <summary>
        /// Translates English data text (config DisplayName, task title, ...) by looking up its key in the
        /// English table. A trailing number is kept ("Tree 2" -> "Дерево 2"). Unknown text is returned as is.
        /// </summary>
        public static string Translate(string english)
        {
            if (string.IsNullOrWhiteSpace(english) || !EnsureReady())
                return english;

            BuildReverseMap();
            var trimmed = english.Trim();
            if (ReverseMap.TryGetValue(trimmed, out var key))
                return Get(key, trimmed);

            var space = trimmed.LastIndexOf(' ');
            if (space > 0 && int.TryParse(trimmed.Substring(space + 1), out _)
                && ReverseMap.TryGetValue(trimmed.Substring(0, space), out key))
                return Get(key, trimmed.Substring(0, space)) + trimmed.Substring(space);

            return english;
        }

        private static readonly Dictionary<string, string> ReverseMap = new(StringComparer.OrdinalIgnoreCase);

        private static void BuildReverseMap()
        {
            if (ReverseMap.Count > 0)
                return;

            try
            {
                var en = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier("en"));
                var table = en != null ? LocalizationSettings.StringDatabase.GetTable(Table, en) : null;
                if (table == null)
                    return;

                foreach (var entry in table.Values)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.Value) && !ReverseMap.ContainsKey(entry.Value.Trim()))
                        ReverseMap[entry.Value.Trim()] = entry.Key;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[L10n] Could not build English lookup: {e.Message}");
            }
        }

        /// <summary>Code of the current locale, e.g. "en", "ru", "zh-Hans".</summary>
        public static string CurrentCode
        {
            get
            {
                if (!EnsureReady())
                    return "en";
                var locale = LocalizationSettings.SelectedLocale;
                return locale != null ? locale.Identifier.Code : "en";
            }
        }

        private static bool EnsureReady()
        {
            if (LocalizationSettings.Instance == null)
                return false;

            var init = LocalizationSettings.InitializationOperation;
            if (!init.IsDone)
                init.WaitForCompletion();

            if (!_subscribed)
            {
                _subscribed = true;
                LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            }

            return true;
        }

        private static void OnLocaleChanged(Locale _)
        {
            Cache.Clear();
            Changed?.Invoke();
        }
    }
}

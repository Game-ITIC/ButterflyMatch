using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameLocalization
{
    /// <summary>
    /// Keeps TMP_Settings.fallbackFontAssets ordered for the selected locale and refreshes visible text.
    /// In the Editor the original order is restored when leaving Play Mode.
    /// </summary>
    public static class LocaleFontSwitcher
    {
        private const string ConfigPath = "LocaleFontConfig";

        private static LocaleFontConfig _config;
        private static List<TMP_FontAsset> _originalFallbacks;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            _config = Resources.Load<LocaleFontConfig>(ConfigPath);
            if (_config == null || TMP_Settings.instance == null)
                return;

            _originalFallbacks = TMP_Settings.fallbackFontAssets != null
                ? new List<TMP_FontAsset>(TMP_Settings.fallbackFontAssets)
                : new List<TMP_FontAsset>();

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
            LocalizationSettings.SelectedLocaleChanged -= Apply;
            LocalizationSettings.SelectedLocaleChanged += Apply;

            var init = LocalizationSettings.InitializationOperation;
            if (!init.IsDone)
                init.WaitForCompletion();

            Apply(LocalizationSettings.SelectedLocale);
        }

        private static void Apply(Locale locale)
        {
            if (_config == null || TMP_Settings.instance == null)
                return;

            var code = locale != null ? locale.Identifier.Code : "en";
            var ordered = new List<TMP_FontAsset>(_config.fallbacks.Count + 1);
            var preferred = _config.GetPreferred(code);
            if (preferred != null)
                ordered.Add(preferred);

            foreach (var font in _config.fallbacks)
            {
                if (font != null && !ordered.Contains(font))
                    ordered.Add(font);
            }

            TMP_Settings.fallbackFontAssets = ordered;

            // Text already laid out keeps cached glyph lookups; force a re-layout.
            var texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var text in texts)
            {
                if (text != null)
                    text.SetAllDirty();
            }
        }

#if UNITY_EDITOR
        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode)
                return;

            LocalizationSettings.SelectedLocaleChanged -= Apply;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            if (_originalFallbacks != null && TMP_Settings.instance != null)
                TMP_Settings.fallbackFontAssets = _originalFallbacks;
        }
#endif
    }
}

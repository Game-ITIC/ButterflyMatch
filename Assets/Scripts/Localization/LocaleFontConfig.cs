using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GameLocalization
{
    /// <summary>
    /// Global TMP fallback fonts per locale. GameFont SDF (the main UI font) covers Latin-1 and Cyrillic;
    /// the fallbacks supply Turkish letters and CJK glyphs. The preferred font of a locale goes first
    /// so shared Han characters render in the right regional style.
    /// </summary>
    [CreateAssetMenu(menuName = "Configs/Locale Font Config", fileName = "LocaleFontConfig")]
    public class LocaleFontConfig : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string localeCode;
            public TMP_FontAsset font;
        }

        [Tooltip("Default fallback order (used for every locale not listed in Preferred).")]
        public List<TMP_FontAsset> fallbacks = new();

        [Tooltip("Font moved to the front of the fallback list for a given locale code.")]
        public List<Entry> preferred = new();

        public TMP_FontAsset GetPreferred(string code)
        {
            foreach (var entry in preferred)
            {
                if (entry != null && entry.font != null
                    && string.Equals(entry.localeCode, code, StringComparison.OrdinalIgnoreCase))
                    return entry.font;
            }

            return null;
        }
    }
}

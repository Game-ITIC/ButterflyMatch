using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using UnityEngine.TextCore.LowLevel;

namespace GameLocalization.EditorTools
{
    /// <summary>
    /// Source of truth for UI strings is Assets/Content/Localization/Source/UI_strings.txt
    /// ('|' separated: key|en|ru|...). Import it into the "UI" String Table Collection, then bind
    /// static TMP labels in prefabs/scenes with LocalizeStringEvent.
    /// </summary>
    public static class L10nEditorTools
    {
        public const string SourcePath = "Assets/Content/Localization/Source/UI_strings.txt";
        public const string TablesFolder = "Assets/Content/Localization/Tables";

        // Old English texts in prefabs that map to a key with a corrected English value.
        private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            { "HOW YO PLAY", "how_to_play" },
            { "Slide three or more fruits of the same color to win", "tutorial_match" },
        };

        // Labels whose text is written by code at runtime: a LocalizeStringEvent would fight it.
        private static readonly HashSet<string> ExcludedNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Play Button Label", "Play Level Label", "TimeLives", "CountHelth", "LifesTimer", "NameRegions"
        };

        // Shop cards (Pack Card, Starter Bundle, ShopItemView, Shop Content Panel) belong to the shop redesign.
        private static readonly string[] ExcludedPathParts =
        {
            "RewardPopup/Panel/Description", "BuildingCardView", "BuildingCatalogPanel/Window/Title",
            "Pack Card", "Starter Bundle Featured Card", "ShopItemView", "Shop Content Panel"
        };

        private static readonly string[] ExcludedComponentTypes =
        {
            "Counter_", "MapLevelNumber", "MovesLabel", "InAppView", "InAppProductView", "BuildingCardView",
            "RewardPopup", "TaskView", "DailyCardView", "RegionCardView"
        };

        [MenuItem("Tools/Localization/Import UI strings")]
        public static void ImportMenu() => Debug.Log(ImportStrings());

        [MenuItem("Tools/Localization/Bind static labels")]
        public static void BindMenu() => Debug.Log(BindStaticLabels(false));

        [MenuItem("Tools/Localization/Check tables and glyphs")]
        public static void CheckMenu() => Debug.Log(CheckCompleteness() + "\n" + CheckGlyphs());

        public static List<string[]> ReadSource(out string[] header)
        {
            var lines = File.ReadAllLines(SourcePath, Encoding.UTF8)
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#")).ToList();
            header = lines[0].Split('|');
            var rows = new List<string[]>();
            for (var i = 1; i < lines.Count; i++)
                rows.Add(lines[i].Split('|'));
            return rows;
        }

        public static StringTableCollection GetCollection()
        {
            return LocalizationEditorSettings.GetStringTableCollection(L10n.Table);
        }

        public static string ImportStrings()
        {
            var rows = ReadSource(out var header);
            var collection = GetCollection();
            if (collection == null)
            {
                Directory.CreateDirectory(TablesFolder);
                AssetDatabase.Refresh();
                collection = LocalizationEditorSettings.CreateStringTableCollection(L10n.Table, TablesFolder);
            }

            var report = new StringBuilder();
            foreach (var locale in LocalizationEditorSettings.GetLocales())
            {
                if (collection.GetTable(locale.Identifier) == null)
                    collection.AddNewTable(locale.Identifier);
            }

            var bad = 0;
            foreach (var row in rows)
            {
                if (row.Length != header.Length)
                {
                    bad++;
                    report.AppendLine($"Row '{row[0]}' has {row.Length} columns, expected {header.Length}");
                    continue;
                }

                var key = row[0].Trim();
                var shared = collection.SharedData.GetEntry(key) ?? collection.SharedData.AddKey(key);
                for (var c = 1; c < header.Length; c++)
                {
                    var code = header[c].Trim();
                    var table = collection.GetTable(new LocaleIdentifier(code)) as StringTable;
                    if (table == null)
                    {
                        report.AppendLine($"No table for locale '{code}'");
                        continue;
                    }

                    var value = row[c].Trim();
                    var entry = table.GetEntry(shared.Id);
                    if (entry == null)
                        table.AddEntry(shared.Id, value);
                    else
                        entry.Value = value;
                    EditorUtility.SetDirty(table);
                }
            }

            EditorUtility.SetDirty(collection);
            EditorUtility.SetDirty(collection.SharedData);
            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);
            AssetDatabase.SaveAssets();
            report.Insert(0, $"Imported {rows.Count - bad} keys x {header.Length - 1} locales into '{L10n.Table}'.\n");
            return report.ToString();
        }

        private static Dictionary<string, string> BuildTextToKey()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in ReadSource(out _))
            {
                var en = Normalize(row[1]);
                if (!map.ContainsKey(en))
                    map[en] = row[0].Trim();
            }

            foreach (var alias in Aliases)
                map[Normalize(alias.Key)] = alias.Value;
            return map;
        }

        private static Dictionary<string, string> BuildKeyToEnglish()
        {
            return ReadSource(out _).ToDictionary(r => r[0].Trim(), r => r[1].Trim());
        }

        private static string Normalize(string s)
        {
            return string.Join(" ", (s ?? string.Empty).Replace("\\n", " ")
                .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private static string PathOf(Transform t)
        {
            var parts = new List<string>();
            while (t != null)
            {
                parts.Add(t.name);
                t = t.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static bool IsExcluded(TMP_Text text)
        {
            if (ExcludedNames.Contains(text.name.Trim()))
                return true;

            var path = PathOf(text.transform);
            if (ExcludedPathParts.Any(path.Contains))
                return true;

            foreach (var behaviour in text.GetComponents<MonoBehaviour>())
            {
                if (behaviour != null && ExcludedComponentTypes.Contains(behaviour.GetType().Name))
                    return true;
            }

            // Shop cards are owned by the in-app shop presenter.
            foreach (var behaviour in text.GetComponentsInParent<MonoBehaviour>(true))
            {
                if (behaviour == null)
                    continue;
                var typeName = behaviour.GetType().Name;
                if (typeName == "InAppView" || typeName == "InAppProductView")
                    return true;
            }

            return false;
        }

        /// <summary>Adds/updates LocalizeStringEvent on one label. Returns true when something changed.</summary>
        public static bool Bind(TMP_Text text, StringTableCollection collection, string key, string english)
        {
            var shared = collection.SharedData.GetEntry(key);
            if (shared == null)
                return false;

            var changed = false;
            var lse = text.GetComponent<LocalizeStringEvent>();
            if (lse == null)
            {
                lse = Undo.AddComponent<LocalizeStringEvent>(text.gameObject);
                changed = true;
            }

            var reference = lse.StringReference;
            if (reference == null || reference.IsEmpty || reference.TableEntryReference.KeyId != shared.Id
                || reference.TableReference.TableCollectionNameGuid != collection.SharedData.TableCollectionNameGuid)
            {
                lse.StringReference = new LocalizedString(collection.SharedData.TableCollectionNameGuid, shared.Id);
                changed = true;
            }

            var evt = lse.OnUpdateString;
            var wired = false;
            for (var i = evt.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                if (evt.GetPersistentTarget(i) == text && evt.GetPersistentMethodName(i) == "set_text" && !wired)
                {
                    wired = true;
                    if (evt.GetPersistentListenerState(i) != UnityEventCallState.EditorAndRuntime)
                    {
                        evt.SetPersistentListenerState(i, UnityEventCallState.EditorAndRuntime);
                        changed = true;
                    }
                }
                else
                {
                    UnityEventTools.RemovePersistentListener(evt, i);
                    changed = true;
                }
            }

            if (!wired)
            {
                var setText = (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), text, "set_text");
                UnityEventTools.AddPersistentListener(evt, setText);
                evt.SetPersistentListenerState(evt.GetPersistentEventCount() - 1, UnityEventCallState.EditorAndRuntime);
                changed = true;
            }

            if (text.text != english)
            {
                text.text = english;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(lse);
                EditorUtility.SetDirty(text);
            }

            return changed;
        }

        private static int BindIn(GameObject root, StringTableCollection collection, Dictionary<string, string> textToKey,
            Dictionary<string, string> keyToEnglish, string context, StringBuilder log, bool dryRun)
        {
            var count = 0;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!textToKey.TryGetValue(Normalize(text.text), out var key) || IsExcluded(text))
                    continue;

                // Nested prefab instance with the same text: its own prefab asset gets the binding.
                if (PrefabUtility.IsPartOfPrefabInstance(text) && text.GetComponent<LocalizeStringEvent>() == null)
                {
                    var source = PrefabUtility.GetCorrespondingObjectFromSource(text);
                    if (source != null && textToKey.TryGetValue(Normalize(source.text), out var sourceKey) && sourceKey == key)
                        continue;
                }

                if (dryRun)
                {
                    log.AppendLine($"  {context}: {PathOf(text.transform)} -> {key}");
                    count++;
                    continue;
                }

                if (Bind(text, collection, key, keyToEnglish[key]))
                {
                    log.AppendLine($"  {context}: {PathOf(text.transform)} -> {key}");
                    count++;
                }
            }

            return count;
        }

        public static string BindStaticLabels(bool dryRun)
        {
            var collection = GetCollection();
            if (collection == null)
                return "No 'UI' collection. Run Import first.";

            var textToKey = BuildTextToKey();
            var keyToEnglish = BuildKeyToEnglish();
            var log = new StringBuilder();
            var total = 0;

            var prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Content", "Assets/JuiceFresh" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.Contains("/Plugins/"))
                .ToList();

            foreach (var path in prefabPaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null || asset.GetComponentsInChildren<TMP_Text>(true).Length == 0)
                    continue;

                var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var n = BindIn(contents, collection, textToKey, keyToEnglish, Path.GetFileNameWithoutExtension(path), log, dryRun);
                    if (n > 0 && !dryRun)
                        PrefabUtility.SaveAsPrefabAsset(contents, path);
                    total += n;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            var setup = EditorSceneManager.GetSceneManagerSetup();
            // Only the live scenes (legacy OLD/ and JuiceFresh demo scenes are left untouched).
            foreach (var buildScene in EditorBuildSettings.scenes.Where(s => s.enabled
                         && s.path.StartsWith("Assets/_Scenes/") && !s.path.Contains("/OLD/")))
            {
                var scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
                var n = 0;
                foreach (var root in scene.GetRootGameObjects())
                    n += BindIn(root, collection, textToKey, keyToEnglish, scene.name, log, dryRun);
                if (n > 0 && !dryRun)
                    EditorSceneManager.SaveScene(scene);
                total += n;
            }

            if (setup.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(setup);

            AssetDatabase.SaveAssets();
            return $"{(dryRun ? "[dry run] would bind" : "Bound")} {total} labels:\n{log}";
        }

        public static string CheckCompleteness()
        {
            var gaps = new List<string>();
            var checkedCount = 0;
            foreach (var col in LocalizationEditorSettings.GetStringTableCollections())
            {
                foreach (var key in col.SharedData.Entries)
                {
                    foreach (var table in col.StringTables)
                    {
                        checkedCount++;
                        var entry = table.GetEntry(key.Id);
                        if (entry == null || string.IsNullOrWhiteSpace(entry.Value))
                            gaps.Add($"{col.TableCollectionName} / {table.LocaleIdentifier.Code} / {key.Key}");
                    }
                }
            }

            if (checkedCount == 0)
                return "INCONCLUSIVE: no table entries found.";
            return gaps.Count == 0
                ? $"COMPLETE: {checkedCount} entries checked, no gaps"
                : $"GAPS ({gaps.Count} of {checkedCount} checked):\n  " + string.Join("\n  ", gaps);
        }

        /// <summary>Every character of every translation must exist in the main font or a global fallback.</summary>
        public static string CheckGlyphs()
        {
            var main = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Content/Fonts/FredokaSDF Thick.asset");
            var fallbacks = TMP_Settings.fallbackFontAssets ?? new List<TMP_FontAsset>();
            var collection = GetCollection();
            if (main == null || collection == null)
                return "Glyph check: main font or collection missing.";

            var missing = new SortedDictionary<string, HashSet<char>>();
            foreach (var table in collection.StringTables)
            {
                foreach (var entry in table.Values)
                {
                    foreach (var ch in entry.Value ?? string.Empty)
                    {
                        if (char.IsWhiteSpace(ch) || main.HasCharacter(ch, false, false))
                            continue;
                        if (fallbacks.Any(f => f != null && HasGlyph(f, ch)))
                            continue;
                        var code = table.LocaleIdentifier.Code;
                        if (!missing.TryGetValue(code, out var set))
                            missing[code] = set = new HashSet<char>();
                        set.Add(ch);
                    }
                }
            }

            if (missing.Count == 0)
                return $"GLYPHS OK: all characters covered (fallbacks: {string.Join(", ", fallbacks.Where(f => f).Select(f => f.name))})";
            return "MISSING GLYPHS:\n  " + string.Join("\n  ", missing.Select(m => $"{m.Key}: {new string(m.Value.ToArray())}"));
        }

        private static readonly Dictionary<TMP_FontAsset, HashSet<uint>> SourceCoverage = new();

        private static bool HasGlyph(TMP_FontAsset font, char ch)
        {
            if (font.HasCharacter(ch, false, false))
                return true;
            if (font.atlasPopulationMode != AtlasPopulationMode.Dynamic || font.sourceFontFile == null)
                return false;

            if (!SourceCoverage.TryGetValue(font, out var set))
                SourceCoverage[font] = set = new HashSet<uint>();

            if (set.Contains(ch))
                return true;
            if (FontEngine.LoadFontFace(font.sourceFontFile, 90) != FontEngineError.Success)
                return false;
            if (FontEngine.TryGetGlyphIndex(ch, out var index) && index != 0)
            {
                set.Add(ch);
                return true;
            }

            return false;
        }
    }
}

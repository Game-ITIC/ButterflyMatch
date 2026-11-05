#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace UI.EditorTools
{
    /// <summary>
    /// Rebakes the one font the whole UI uses. Every label points at GameFont SDF, so changing the look of the
    /// game's text means changing SourceFontPath and running this; no prefab or scene has to change.
    /// Run it again when UI_strings.txt gets characters the atlas doesn't have (Check tables and glyphs reports them).
    /// </summary>
    public static class GameFontBuilder
    {
        public const string FontAssetPath = "Assets/Content/Fonts/GameFont SDF.asset";
        public const string StickerMaterialPath = "Assets/Content/Fonts/GameFont Sticker.mat";
        public const string SourceFontPath = "Assets/Content/Fonts/Nunito-Black.ttf";

        private const string StringsPath = "Assets/Content/Localization/Source/UI_strings.txt";
        // Padding is 19% of the sampling size: sticker outlines and underlays are measured against it, so a lower ratio thins them.
        private const int SamplingSize = 52;
        private const int Padding = 10;
        private const int AtlasSize = 1024;

        // The font has tens of thousands of kerning pairs; only the ones the eye can see are worth keeping in memory.
        private const float MinKerningEm = 0.03f;

        // Label sizes were laid out for a line height of 1.21 em. Nunito's own 1.36 em would shrink auto-sized labels and
        // truncate short ones. Glyphs aren't affected, only where lines sit, and this keeps capitals centered in the line.
        private const float AscentEm = 0.975f;
        private const float DescentEm = -0.235f;

        // Nunito is spaced for body text. Game labels read better a little tighter. In 1/100 em.
        private const float Tracking = -2f;

        // key|en|ru|es|pt-BR|de|fr|it|tr|id|ja|ko|zh-Hans: ja, ko and zh-Hans come from the fallback fonts in LocaleFontConfig.
        private const int LastBakedColumn = 9;
        private const string Extras = "…—–×•·№«»„“”‘’∞ĞğİıŞş";

        [MenuItem("Tools/UI/Rebuild Game Font")]
        public static void RebuildFromMenu() => Debug.Log(Rebuild());

        public static string Rebuild()
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            var target = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (source == null || target == null)
            {
                return $"[GameFontBuilder] Missing {SourceFontPath} or {FontAssetPath}.";
            }

            var characters = CollectCharacters();
            var baked = TMP_FontAsset.CreateFontAsset(source, SamplingSize, Padding, GlyphRenderMode.SDFAA, AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, false);
            baked.TryAddCharacters(characters, out var missing, true);
            var minKerning = SamplingSize * MinKerningEm;
            baked.fontFeatureTable.glyphPairAdjustmentRecords.RemoveAll(
                pair => Mathf.Abs(pair.firstAdjustmentRecord.glyphValueRecord.xAdvance) < minKerning);
            var kerningCount = baked.fontFeatureTable.glyphPairAdjustmentRecords.Count;

            var atlas = CopyAtlas(baked.atlasTexture, Path.GetFileNameWithoutExtension(FontAssetPath) + " Atlas");
            var material = target.material;
            var normalWeight = baked.normalStyle;
            var glyphCount = baked.glyphTable.Count;

            // Copied into the existing asset so its GUID and material id stay: that's what every label references.
            EditorUtility.CopySerialized(baked, target);
            target.name = Path.GetFileNameWithoutExtension(FontAssetPath);
            material.name = target.name + " Material";
            target.material = material;

            // The font is already Black. Faux bold on top of it blurs letters and spreads them apart, and 110 labels have the flag.
            target.boldStyle = 0f;
            target.boldSpacing = 0f;
            target.normalSpacingOffset = Tracking;
            ApplyLineMetrics(target);
            ReplaceAtlas(target, atlas);
            target.atlasPopulationMode = AtlasPopulationMode.Static;

            PointMaterial(material, atlas, normalWeight, target.boldStyle);
            material.SetFloat("_FaceDilate", 0f);
            var sticker = AssetDatabase.LoadAssetAtPath<Material>(StickerMaterialPath);
            if (sticker != null)
            {
                PointMaterial(sticker, atlas, normalWeight, target.boldStyle);
                EditorUtility.SetDirty(sticker);
            }

            EditorUtility.SetDirty(material);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            target.ReadFontAssetDefinition();
            TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, target);

            Object.DestroyImmediate(baked.material);
            Object.DestroyImmediate(baked.atlasTexture);
            Object.DestroyImmediate(baked);

            var note = string.IsNullOrEmpty(missing) ? "none" : missing;
            return $"[GameFontBuilder] {source.name}: {glyphCount} glyphs from {characters.Length} characters, {kerningCount} kerning pairs. Not in the font (fallback fonts draw them): {note}";
        }

        private static string CollectCharacters()
        {
            var set = new SortedSet<char>();
            for (var c = ' '; c <= '~'; c++)
            {
                set.Add(c);
            }

            for (var c = '¡'; c <= 'ÿ'; c++)
            {
                set.Add(c);
            }

            for (var c = 'А'; c <= 'я'; c++)
            {
                set.Add(c);
            }

            set.Add('Ё');
            set.Add('ё');
            foreach (var c in Extras)
            {
                set.Add(c);
            }

            foreach (var line in File.ReadAllLines(StringsPath, Encoding.UTF8).Skip(1))
            {
                var columns = line.Split('|');
                for (var i = 1; i <= LastBakedColumn && i < columns.Length; i++)
                {
                    foreach (var c in columns[i])
                    {
                        set.Add(c);
                        set.Add(char.ToUpperInvariant(c));
                    }
                }
            }

            return new string(set.ToArray());
        }

        private static Texture2D CopyAtlas(Texture2D source, string name)
        {
            var copy = new Texture2D(source.width, source.height, TextureFormat.Alpha8, false) { name = name };
            copy.LoadRawTextureData(source.GetRawTextureData());
            copy.Apply(false, false);
            return copy;
        }

        private static void ApplyLineMetrics(TMP_FontAsset target)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty("m_FaceInfo.m_AscentLine").floatValue = SamplingSize * AscentEm;
            serialized.FindProperty("m_FaceInfo.m_DescentLine").floatValue = SamplingSize * DescentEm;
            serialized.FindProperty("m_FaceInfo.m_LineHeight").floatValue = SamplingSize * (AscentEm - DescentEm);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ReplaceAtlas(TMP_FontAsset target, Texture2D atlas)
        {
            foreach (var old in AssetDatabase.LoadAllAssetsAtPath(FontAssetPath).OfType<Texture2D>().ToArray())
            {
                AssetDatabase.RemoveObjectFromAsset(old);
                Object.DestroyImmediate(old, true);
            }

            AssetDatabase.AddObjectToAsset(atlas, target);
            target.atlasTextures = new[] { atlas };

            // The font is static: nothing writes to the atlas at runtime, so it needs no CPU copy.
            var serialized = new SerializedObject(atlas);
            serialized.FindProperty("m_IsReadable").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PointMaterial(Material material, Texture2D atlas, float normalWeight, float boldWeight)
        {
            material.SetTexture("_MainTex", atlas);
            material.SetFloat("_TextureWidth", atlas.width);
            material.SetFloat("_TextureHeight", atlas.height);
            material.SetFloat("_GradientScale", Padding + 1);
            material.SetFloat("_WeightNormal", normalWeight);
            material.SetFloat("_WeightBold", boldWeight);
        }
    }
}
#endif

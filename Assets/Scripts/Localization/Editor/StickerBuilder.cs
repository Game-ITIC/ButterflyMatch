using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameLocalization.EditorTools
{
    /// <summary>
    /// Replaces baked English text images with a localized TMP "sticker" in the game's candy style
    /// (gradient face, white outline, purple underlay). The image stays but is disabled so tweens still run.
    /// </summary>
    public static class StickerBuilder
    {
        public const string MaterialPath = "Assets/Content/Fonts/GameFont Sticker.mat";
        public const string FontPath = "Assets/Content/Fonts/GameFont SDF.asset";
        public const string ChildName = "Sticker Text";

        public enum Style { Pink, Red, Orange }

        public static Material GetOrCreateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
                return material;

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            material = new Material(font.material) { name = "GameFont Sticker" };
            material.EnableKeyword("OUTLINE_ON");
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor("_OutlineColor", Color.white);
            material.SetFloat("_OutlineWidth", 0.32f);
            material.SetFloat("_FaceDilate", 0.18f);
            material.SetColor("_UnderlayColor", new Color(0.30f, 0.12f, 0.56f, 1f));
            material.SetFloat("_UnderlayOffsetX", 0.35f);
            // Further down, the underlay reads the neighbouring glyph in the atlas and draws a thin line above the letters.
            material.SetFloat("_UnderlayOffsetY", -0.40f);
            material.SetFloat("_UnderlayDilate", 0.45f);
            material.SetFloat("_UnderlaySoftness", 0.05f);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static VertexGradient Gradient(Style style)
        {
            Color top, bottom;
            switch (style)
            {
                case Style.Red:
                    ColorUtility.TryParseHtmlString("#FF7D98", out top);
                    ColorUtility.TryParseHtmlString("#E3163F", out bottom);
                    break;
                case Style.Orange:
                    ColorUtility.TryParseHtmlString("#FFC93F", out top);
                    ColorUtility.TryParseHtmlString("#FF8A00", out bottom);
                    break;
                default:
                    ColorUtility.TryParseHtmlString("#FF8BEF", out top);
                    ColorUtility.TryParseHtmlString("#C23BEA", out bottom);
                    break;
            }

            return new VertexGradient(top, top, bottom, bottom);
        }

        /// <summary>
        /// Adds (or updates) the sticker text under <paramref name="image"/>. <paramref name="extraDown"/> grows the
        /// text area downward (in the image's local units) to also cover a second image that is hidden.
        /// Pass bind=false when a script sets the text (e.g. MOVES/TIME).
        /// </summary>
        public static TMP_Text Apply(Image image, string key, string english, Style style, bool bind = true, float extraDown = 0f,
            float maxFontSize = 400f)
        {
            var parent = (RectTransform)image.transform;
            var existing = parent.Find(ChildName);
            var go = existing != null ? existing.gameObject : new GameObject(ChildName, typeof(RectTransform));
            go.layer = image.gameObject.layer;
            go.transform.SetParent(parent, false);

            // Undo non-uniform parent scale so glyphs are not stretched.
            var s = parent.localScale;
            var k = Mathf.Min(Mathf.Abs(s.x), Mathf.Abs(s.y));
            if (k <= 0f) k = 1f;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = new Vector3(k / s.x, k / s.y, 1f);
            var rect = parent.rect;
            rt.sizeDelta = new Vector2(rect.width * s.x / k, (rect.height + extraDown) * s.y / k);
            rt.anchoredPosition = new Vector2(0f, -extraDown * 0.5f);

            var text = go.GetComponent<TextMeshProUGUI>();
            if (text == null) text = go.AddComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            text.fontSharedMaterial = GetOrCreateMaterial();
            text.text = english;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10;
            text.fontSizeMax = maxFontSize;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.color = Color.white;
            text.enableVertexGradient = true;
            text.colorGradient = Gradient(style);
            text.characterSpacing = 4f;
            text.raycastTarget = false;
            text.margin = new Vector4(rt.sizeDelta.x * 0.04f, rt.sizeDelta.y * 0.06f, rt.sizeDelta.x * 0.04f, rt.sizeDelta.y * 0.06f);

            var sticker = go.GetComponent<StickerText>();
            if (sticker == null) sticker = go.AddComponent<StickerText>();
            var so = new SerializedObject(sticker);
            so.FindProperty("_source").objectReferenceValue = image;
            so.ApplyModifiedPropertiesWithoutUndo();

            image.enabled = false;

            if (bind)
                L10nEditorTools.Bind(text, L10nEditorTools.GetCollection(), key, english);

            EditorUtility.SetDirty(text);
            EditorUtility.SetDirty(image);
            return text;
        }

        public static string ApplyInPrefab(string prefabPath, string objectPath, string key, string english, Style style,
            float extraDown = 0f, string hidePath = null, bool bind = true, float maxFontSize = 400f)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var target = string.IsNullOrEmpty(objectPath) ? root.transform : root.transform.Find(objectPath);
                var image = target != null ? target.GetComponent<Image>() : null;
                if (image == null)
                    return $"MISSING {prefabPath}:{objectPath}";

                var text = Apply(image, key, english, style, bind, extraDown, maxFontSize);
                if (!string.IsNullOrEmpty(hidePath))
                {
                    var hide = root.transform.Find(hidePath)?.GetComponent<Image>();
                    if (hide != null)
                        hide.enabled = false;
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return $"OK {System.IO.Path.GetFileNameWithoutExtension(prefabPath)}:{objectPath} -> {key} ({text.rectTransform.sizeDelta})";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}

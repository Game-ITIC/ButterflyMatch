#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace UI.EditorTools
{
    /// <summary>
    /// Checks UI prefabs and the open scenes against the rules in Docs/UI.md: readable text, tappable buttons,
    /// meaningful names, no scale hacks, no missing scripts, thin scene overrides.
    /// Writes Logs/UiLint.txt and logs a summary. Read-only: it never changes an asset.
    /// </summary>
    public static class UiLint
    {
        public const string PrefabFolder = "Assets/Content/Prefabs/UI";

        // Everything is measured in real pixels on a 1080x2160 phone, because each canvas scales differently (see Docs/UI.md).
        public static readonly Vector2 ReferenceScreen = new(1080f, 2160f);
        public const float MinFontPx = 32f;
        public const float MinAutoSizePx = 22f;
        public const float MinTapPx = 127f;
        public const int MaxSceneOverrides = 12;

        // Prefabs have no canvas of their own; this is the factor of the canvas they are placed under.
        private const float MenuCanvasFactor = 1.06f;
        private const float GamePopupCanvasFactor = 0.703f;
        private const float GameHudCanvasFactor = 0.88f;

        private static readonly Regex AutoNumbered = new(@"\(\d+\)$", RegexOptions.Compiled);
        private static readonly HashSet<string> DefaultNames = new() { "GameObject", "Image", "Text", "Text (TMP)", "Button", "Panel", "New Text" };

        [MenuItem("Tools/UI/Lint UI Prefabs and Open Scenes")]
        public static void RunFromMenu()
        {
            var report = Run();
            var path = Path.Combine("Logs", "UiLint.txt");
            Directory.CreateDirectory("Logs");
            File.WriteAllText(path, report.Text);
            Debug.Log($"[UiLint] {report.Summary}. Details: {path}");
        }

        public readonly struct Report
        {
            public readonly string Text;
            public readonly string Summary;

            public Report(string text, string summary)
            {
                Text = text;
                Summary = summary;
            }
        }

        public static Report Run()
        {
            var counts = new Dictionary<string, int>();
            var body = new StringBuilder();

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/_Unused/"))
                {
                    continue;
                }

                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Check(root.transform, path.Substring(PrefabFolder.Length + 1), body, counts);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            for (var i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.GetComponentInChildren<Canvas>(true) == null)
                    {
                        continue;
                    }

                    Check(root.transform, scene.name, body, counts);
                    CheckOverrides(root.transform, scene.name, body, counts);
                }
            }

            var summary = new StringBuilder();
            var total = 0;
            foreach (var pair in counts)
            {
                summary.Append(pair.Key).Append(": ").Append(pair.Value).Append("  ");
                total += pair.Value;
            }

            return new Report(body.ToString(), total == 0 ? "no issues" : $"{total} issues  {summary}");
        }

        private static void Check(Transform root, string owner, StringBuilder body, Dictionary<string, int> counts)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                // Nested prefabs report under their own asset; scene walks stay in the scene's own objects.
                var path = PathTo(transform, root);
                CheckName(transform, owner, path, body, counts);
                CheckMissingScripts(transform, owner, path, body, counts);
                CheckScale(transform, owner, path, body, counts);
            }

            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                CheckText(text, owner, PathTo(text.transform, root), body, counts);
            }

            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                CheckButton(button, owner, PathTo(button.transform, root), body, counts);
            }
        }

        private static void CheckName(Transform transform, string owner, string path, StringBuilder body, Dictionary<string, int> counts)
        {
            var name = transform.name;
            if (name != name.Trim())
            {
                Add("name-space", owner, path, "name has a leading or trailing space", body, counts);
            }
            else if (AutoNumbered.IsMatch(name))
            {
                Add("name-numbered", owner, path, "auto-numbered name", body, counts);
            }
            else if (DefaultNames.Contains(name))
            {
                Add("name-default", owner, path, "default name says nothing about the role", body, counts);
            }
        }

        private static void CheckMissingScripts(Transform transform, string owner, string path, StringBuilder body, Dictionary<string, int> counts)
        {
            var missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
            if (missing > 0)
            {
                Add("missing-script", owner, path, $"{missing} missing script(s)", body, counts);
            }
        }

        private static void CheckScale(Transform transform, string owner, string path, StringBuilder body, Dictionary<string, int> counts)
        {
            if (transform is not RectTransform || transform.parent == null || transform.childCount == 0)
            {
                return;
            }

            // Scale on a container hides the real size from anyone reading the prefab. Icons and tween targets are fine.
            var scale = transform.localScale;
            if (Mathf.Abs(scale.x - 1f) > 0.01f || Mathf.Abs(scale.y - 1f) > 0.01f)
            {
                if (scale != Vector3.zero)
                {
                    Add("scale-hack", owner, path, $"container scaled {scale.x:0.##}", body, counts);
                }
            }
        }

        private static void CheckText(TMP_Text text, string owner, string path, StringBuilder body, Dictionary<string, int> counts)
        {
            var scale = EffectiveScale(text.transform) * CanvasFactor(text.transform, owner);
            var size = (text.enableAutoSizing ? text.fontSizeMax : text.fontSize) * scale;
            // Rounded: 30 on the menu canvas is 31.8 px, which is a 32 px label, not a finding.
            if (Mathf.Round(size) < MinFontPx)
            {
                var hidden = IsSwitchedOff(text.transform) ? " (switched off in the prefab)" : "";
                Add("small-text", owner, path, $"font {size:0} px < {MinFontPx:0}{hidden}", body, counts);
            }

            if (text.enableAutoSizing && text.fontSizeMin * scale < MinAutoSizePx)
            {
                Add("small-autosize", owner, path, $"auto size floor {text.fontSizeMin * scale:0} px < {MinAutoSizePx:0}", body, counts);
            }
        }

        // The root doesn't count: popups are stored switched off and turned on when they open.
        private static bool IsSwitchedOff(Transform transform)
        {
            for (var current = transform; current.parent != null; current = current.parent)
            {
                if (!current.gameObject.activeSelf)
                {
                    return true;
                }
            }

            return false;
        }

        private static void CheckButton(Button button, string owner, string path, StringBuilder body, Dictionary<string, int> counts)
        {
            var width = 0f;
            var height = 0f;
            foreach (var graphic in button.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic.raycastTarget)
                {
                    continue;
                }

                var scale = EffectiveScale(graphic.transform) * CanvasFactor(graphic.transform, owner);
                var rect = graphic.rectTransform.rect;
                width = Mathf.Max(width, rect.width * scale);
                height = Mathf.Max(height, rect.height * scale);
            }

            if (Mathf.Min(width, height) < MinTapPx && width > 0f)
            {
                Add("small-tap", owner, path, $"tap zone {width:0}x{height:0} px < {MinTapPx:0} (add a UIHitArea child)", body, counts);
            }
        }

        private static void CheckOverrides(Transform root, string owner, StringBuilder body, Dictionary<string, int> counts)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                var go = transform.gameObject;
                if (!PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                {
                    continue;
                }

                var overrides = 0;
                var modifications = PrefabUtility.GetPropertyModifications(go);
                if (modifications != null)
                {
                    foreach (var modification in modifications)
                    {
                        if (modification.target != null && !PrefabUtility.IsDefaultOverride(modification))
                        {
                            overrides++;
                        }
                    }
                }

                if (overrides > MaxSceneOverrides)
                {
                    Add("thick-instance", owner, PathTo(transform, root), $"{overrides} overrides: apply them to the prefab or the prefab stops being the truth", body, counts);
                }
            }
        }

        // Scale of the object inside its own canvas tree, ignoring the canvas itself (runtime CanvasScaler output).
        private static float EffectiveScale(Transform transform)
        {
            var scale = 1f;
            for (var current = transform; current != null; current = current.parent)
            {
                if (current.GetComponent<Canvas>() != null && current.GetComponent<Canvas>().isRootCanvas)
                {
                    break;
                }

                // Popups rest at scale 0 until their tween opens them; that says nothing about their size.
                var local = Mathf.Abs(current.localScale.x);
                if (local > 0f)
                {
                    scale *= local;
                }
            }

            return scale;
        }

        // Canvas units to pixels on the reference phone: the real CanvasScaler if there is one, else by prefab folder.
        private static float CanvasFactor(Transform transform, string owner)
        {
            var scaler = transform.GetComponentInParent<CanvasScaler>(true);
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                return ScalerFactor(scaler);
            }

            if (owner.StartsWith("Game/HUD"))
            {
                return GameHudCanvasFactor;
            }

            return owner.StartsWith("Game/") ? GamePopupCanvasFactor : MenuCanvasFactor;
        }

        private static float ScalerFactor(CanvasScaler scaler)
        {
            var reference = scaler.referenceResolution;
            switch (scaler.screenMatchMode)
            {
                case CanvasScaler.ScreenMatchMode.Expand:
                    return Mathf.Min(ReferenceScreen.x / reference.x, ReferenceScreen.y / reference.y);
                case CanvasScaler.ScreenMatchMode.Shrink:
                    return Mathf.Max(ReferenceScreen.x / reference.x, ReferenceScreen.y / reference.y);
                default:
                    var logWidth = Mathf.Log(ReferenceScreen.x / reference.x, 2f);
                    var logHeight = Mathf.Log(ReferenceScreen.y / reference.y, 2f);
                    return Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, scaler.matchWidthOrHeight));
            }
        }

        private static string PathTo(Transform transform, Transform root)
        {
            var path = transform.name;
            for (var current = transform; current != root && current.parent != null; current = current.parent)
            {
                path = current.parent.name + "/" + path;
            }

            return path;
        }

        private static void Add(string rule, string owner, string path, string message, StringBuilder body, Dictionary<string, int> counts)
        {
            counts.TryGetValue(rule, out var count);
            counts[rule] = count + 1;
            body.Append('[').Append(rule).Append("] ").Append(owner).Append(": ").Append(path).Append(" - ").AppendLine(message);
        }
    }
}
#endif

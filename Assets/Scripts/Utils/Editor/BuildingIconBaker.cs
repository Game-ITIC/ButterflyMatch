#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Configs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UI.EditorTools
{
    /// <summary>
    /// Renders each building of an island slot into Assets/Content/UI/BuildingIcons/{name}.png and assigns it as the icon,
    /// so the building catalog shows the building itself instead of the island picture.
    /// Most buildings are one part of an island model; the parts are found the way the game finds them.
    /// Re-run after changing a building model or adding a region. Idempotent.
    /// </summary>
    public static class BuildingIconBaker
    {
        public const string Folder = "Assets/Content/UI/BuildingIcons";

        // The catalog draws icons at about 117 px on a 1080x2160 phone. All icons stay loaded with the region config, so keep them small.
        private const int Size = 128;
        private const float ModelRadius = 10f;
        private const float MinCoverage = 0.15f;
        private static readonly Vector3 FarAway = new(5000f, 0f, 5000f);
        private static readonly Color Ambient = new(0.72f, 0.70f, 0.76f);

        [MenuItem("Tools/UI/Bake Building Icons")]
        public static void BakeAll()
        {
            // Buildings no island slot uses never reach the catalog, so they get no icon.
            var placements = Placements();
            Directory.CreateDirectory(Folder);
            var done = 0;
            try
            {
                foreach (var pair in placements)
                {
                    EditorUtility.DisplayProgressBar("Building icons", pair.Key.name, (float)done++ / placements.Count);
                    Bake(pair.Key, pair.Value);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildingIconBaker] Baked {done} icons into {Folder}.");
        }

        /// <summary>Where a building sits: its island and the slot that says which parts are this building.</summary>
        public readonly struct Placement
        {
            public readonly IslandSO Island;
            public readonly BuildingSlot Slot;

            public Placement(IslandSO island, BuildingSlot slot)
            {
                Island = island;
                Slot = slot;
            }
        }

        public static Dictionary<BuildingSO, Placement> Placements()
        {
            var result = new Dictionary<BuildingSO, Placement>();
            foreach (var guid in AssetDatabase.FindAssets("t:IslandSO"))
            {
                var island = AssetDatabase.LoadAssetAtPath<IslandSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (island.Slots == null)
                {
                    continue;
                }

                foreach (var slot in island.Slots)
                {
                    if (slot != null && slot.Building != null)
                    {
                        result[slot.Building] = new Placement(island, slot);
                    }
                }
            }

            return result;
        }

        private static void Bake(BuildingSO building, Placement placement)
        {
            var png = Render(building, placement);
            if (png == null)
            {
                Debug.LogWarning($"[BuildingIconBaker] {building.name} has no model; icon left as is.", building);
                return;
            }

            var path = $"{Folder}/{building.name}.png";
            File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = Size;
                importer.SaveAndReimport();
            }

            var serialized = new SerializedObject(building);
            serialized.FindProperty("icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Three-quarter view, soft warm light, transparent background.
        public static byte[] Render(BuildingSO building, Placement placement)
        {
            // Trees and lanterns spawn from their own prefab; every other building is a part of the island model.
            var slot = placement.Slot;
            var islandModel = placement.Island != null ? placement.Island.IslandPrefab : null;
            var spawned = slot == null || slot.IsTree || slot.SlotId.IndexOf("lantern", StringComparison.OrdinalIgnoreCase) >= 0;
            var isPart = !spawned && islandModel != null;
            var model = isPart ? islandModel : building.Prefab;
            if (model == null)
            {
                return null;
            }

            var go = Object.Instantiate(model);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetPositionAndRotation(FarAway, Quaternion.Euler(0f, 200f, 0f));

            // Spawned prefabs may rest at scale 0: the game scales them up when they appear.
            if (go.transform.localScale.sqrMagnitude < 0.0001f)
            {
                go.transform.localScale = Vector3.one;
            }

            var steps = PlayBuildClip(go);
            ShowOnlyBuilding(go, building, isPart ? slot : null, steps);

            // Some models are authored tiny: the clip planes and float precision this far from the origin need a real size.
            var bounds = BoundsOf(go);
            if (bounds.extents.sqrMagnitude < 1e-10f)
            {
                // Nothing to draw: the mesh is missing from the model.
                Object.DestroyImmediate(go);
                return null;
            }

            go.transform.localScale *= ModelRadius / bounds.extents.magnitude;
            bounds = BoundsOf(go);

            var ambientMode = RenderSettings.ambientMode;
            var ambientLight = RenderSettings.ambientLight;
            var ambientProbe = RenderSettings.ambientProbe;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Ambient;

            // URP reads the probe, not ambientLight.
            var flatProbe = new SphericalHarmonicsL2();
            flatProbe.AddAmbientLight(Ambient);
            RenderSettings.ambientProbe = flatProbe;

            var cameraObject = new GameObject("IconCamera") { hideFlags = HideFlags.HideAndDontSave };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.fieldOfView = 30f;
            var radius = bounds.extents.magnitude;
            var distance = radius / Mathf.Sin(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.95f;
            camera.nearClipPlane = Mathf.Max(0.01f, distance - radius * 2f);
            camera.farClipPlane = distance + radius * 3f;
            camera.transform.position = bounds.center + Quaternion.Euler(28f, 0f, 0f) * Vector3.back * distance;
            camera.transform.LookAt(bounds.center);

            var lightObject = new GameObject("IconLight") { hideFlags = HideFlags.HideAndDontSave };
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.88f);
            light.intensity = 1.35f;
            lightObject.transform.rotation = Quaternion.Euler(44f, 322f, 8f);

            var target = RenderTexture.GetTemporary(Size, Size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 8);
            camera.targetTexture = target;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target);

            var png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(lightObject);
            Object.DestroyImmediate(go);
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientLight = ambientLight;
            RenderSettings.ambientProbe = ambientProbe;
            return png;
        }

        // The game's rule (IslandPieceRig.CaptureParts): a part belongs to the first clip step whose end frame moves it from frame 0.
        // Leaves the model at the end of the clip, where every part is built.
        private static Dictionary<Transform, int> PlayBuildClip(GameObject go)
        {
            var steps = new Dictionary<Transform, int>();
            var config = go.GetComponentInChildren<BuildingsAnimationConfig>(true);
            if (config == null || config.animationClip == null || config.animator == null || config.data == null)
            {
                return steps;
            }

            var clip = config.animationClip;
            var root = config.animator.gameObject;
            clip.SampleAnimation(root, 0f);
            var parts = root.GetComponentsInChildren<Transform>(true);
            var positions = new Vector3[parts.Length];
            var rotations = new Quaternion[parts.Length];
            var scales = new Vector3[parts.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                positions[i] = parts[i].localPosition;
                rotations[i] = parts[i].localRotation;
                scales[i] = parts[i].localScale;
            }

            for (var step = 0; step < config.data.Count; step++)
            {
                clip.SampleAnimation(root, config.data[step].endFrame / clip.frameRate);
                for (var i = 0; i < parts.Length; i++)
                {
                    var moved = (parts[i].localScale - scales[i]).sqrMagnitude > 0.01f
                        || (parts[i].localPosition - positions[i]).sqrMagnitude > 0.0004f
                        || Quaternion.Angle(parts[i].localRotation, rotations[i]) > 1f;
                    if (moved && parts[i] != root.transform && !steps.ContainsKey(parts[i]))
                    {
                        steps[parts[i]] = step;
                    }
                }
            }

            clip.SampleAnimation(root, clip.length);
            return steps;
        }

        // Turns off every renderer that isn't this building.
        private static void ShowOnlyBuilding(GameObject go, BuildingSO building, BuildingSlot slot, Dictionary<Transform, int> steps)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            var kept = new List<Renderer>();
            foreach (var renderer in renderers)
            {
                // Particles and helper quads would inflate the framing; only real meshes count.
                renderer.enabled = renderer is not ParticleSystemRenderer && Belongs(renderer.transform, go.transform, slot, steps);
                if (renderer.enabled)
                {
                    kept.Add(renderer);
                }
            }

            if (kept.Count == 0)
            {
                // The slot names nothing in this model: better the whole model than an empty icon.
                Debug.LogWarning($"[BuildingIconBaker] {building.name}: no part found for its slot; drawing the whole model.", building);
                foreach (var renderer in renderers)
                {
                    renderer.enabled = renderer is not ParticleSystemRenderer;
                    if (renderer.enabled)
                    {
                        kept.Add(renderer);
                    }
                }
            }

            foreach (var renderer in kept)
            {
                Reveal(renderer.transform, go.transform);
            }

            FocusOnBiggestPiece(kept);
        }

        private static bool Belongs(Transform part, Transform root, BuildingSlot slot, Dictionary<Transform, int> steps)
        {
            if (slot == null)
            {
                return true;
            }

            for (var current = part; current != null && current != root; current = current.parent)
            {
                if (slot.NamesAPart)
                {
                    if (slot.Names(current.name))
                    {
                        return true;
                    }

                    continue;
                }

                // Trees and lanterns have slots of their own, whatever step moves them.
                if (current.name.StartsWith("Tree", StringComparison.Ordinal)
                    || current.name.StartsWith("fonar", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (steps.TryGetValue(current, out var step))
                {
                    return step == slot.AnimationStep;
                }
            }

            return false;
        }

        // Not every clip reaches a part: some models keep them deactivated or at scale 0 until the game shows them.
        private static void Reveal(Transform part, Transform root)
        {
            for (var current = part; current != null && current != root; current = current.parent)
            {
                current.gameObject.SetActive(true);
                if (current.localScale.sqrMagnitude < 0.0001f)
                {
                    current.localScale = Vector3.one;
                }
            }
        }

        // Scattered pieces (palms, lamps) would come out as dots, so the icon shows the biggest piece and what touches it.
        private static void FocusOnBiggestPiece(List<Renderer> kept)
        {
            var biggest = kept[0];
            var covered = 0f;
            var all = kept[0].bounds;
            foreach (var renderer in kept)
            {
                all.Encapsulate(renderer.bounds);
                var size = renderer.bounds.extents.sqrMagnitude;
                covered += size;
                if (size > biggest.bounds.extents.sqrMagnitude)
                {
                    biggest = renderer;
                }
            }

            if (covered >= all.extents.sqrMagnitude * MinCoverage)
            {
                return;
            }

            var core = biggest.bounds;
            core.Expand(core.size.magnitude * 0.25f);
            foreach (var renderer in kept)
            {
                renderer.enabled = core.Intersects(renderer.bounds);
            }
        }

        private static Bounds BoundsOf(GameObject go)
        {
            var bounds = new Bounds(go.transform.position, Vector3.zero);
            var first = true;
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled)
                {
                    continue;
                }

                if (first)
                {
                    bounds = renderer.bounds;
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return bounds;
        }
    }
}
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Configs;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Utils.Editor
{
    /// <summary>
    /// Builds the "North" region islands (Blender exports in Assets/Content/Models/Regions/North) into
    /// region prefabs that follow the Asia island pattern: root with BuildingsAnimationConfig,
    /// static island mesh, animated buildings (one scale-in clip, step frame ranges) and a menu icon.
    /// Re-run after re-exporting the FBX files from ArtSource/North.
    /// </summary>
    public static class NorthRegionBuilder
    {
        private const string RootDir = "Assets/Content/Models/Regions/North";
        private const string RegionConfigPath = "Assets/Content/Configs/RegionConfig.asset";
        private const string MaterialTemplatePath = "Assets/Content/Models/Regions/Asia/Japan/Japan_island_Mat.mat";
        private const int Fps = 24;
        private const int PopFrames = 16;
        private const int StepGapFrames = 12;

        private struct IslandDef
        {
            public string Id;
            public string Display;
            public int StarCost;
            public float Yaw;
        }

        private static readonly IslandDef[] Islands =
        {
            new IslandDef { Id = "VikingCamp", Display = "Norway", StarCost = 25, Yaw = -12f },
            new IslandDef { Id = "FrostCamp", Display = "Lapland", StarCost = 30, Yaw = -12f },
            new IslandDef { Id = "FrostKeep", Display = "Iceland", StarCost = 35, Yaw = -12f },
        };

        // Placement under the menu "World" transform, matching where the Asia islands sit in front of the camera.
        private static readonly Vector3 RootLocalPosition = new Vector3(-7.9f, 1.0f, 0.5f);
        private const float WorldYaw = 18.7f;
        private const float RootScale = 1.05f;

        [MenuItem("Tools/Regions/Build North Region Islands")]
        public static void BuildAllMenu()
        {
            BuildAll();
        }

        public static string BuildAll()
        {
            var log = new System.Text.StringBuilder();
            var built = new List<(IslandDef def, GameObject prefab, Sprite icon)>();
            foreach (var def in Islands)
            {
                var prefab = BuildIsland(def, log);
                if (prefab == null) continue;
                var icon = RenderIcon(def, prefab);
                built.Add((def, prefab, icon));
            }

            AddToRegionConfig(built, log);
            AssetDatabase.SaveAssets();
            return log.ToString();
        }

        private static GameObject BuildIsland(IslandDef def, System.Text.StringBuilder log)
        {
            var dir = $"{RootDir}/{def.Id}";
            var islandFbx = $"{dir}/{def.Id}_Island.fbx";
            var bldFbx = $"{dir}/{def.Id}_Buildings.fbx";
            if (!File.Exists(islandFbx) || !File.Exists(bldFbx))
            {
                log.AppendLine($"[{def.Id}] missing FBX, skipped");
                return null;
            }

            ConfigureModel(islandFbx);
            ConfigureModel(bldFbx);
            var islandTex = ConfigureTexture($"{dir}/{def.Id}_Island_BaseMap.png");
            var bldTex = ConfigureTexture($"{dir}/{def.Id}_Buildings_BaseMap.png");
            var islandMat = CreateMaterial($"{dir}/{def.Id}_Island_Mat.mat", islandTex);
            var bldMat = CreateMaterial($"{dir}/{def.Id}_Buildings_Mat.mat", bldTex);

            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject($"{def.Id}_Island_Pref");
                SceneManager.MoveGameObjectToScene(root, preview);

                var island = InstantiateUnpacked(islandFbx, root.transform, $"{def.Id}_island");
                AssignMaterial(island, islandMat);

                var buildings = InstantiateUnpacked(bldFbx, root.transform, def.Id);
                AssignMaterial(buildings, bldMat);

                var steps = new SortedDictionary<int, List<Transform>>();
                foreach (Transform child in buildings.transform)
                {
                    if (child.name.Length < 4 || child.name[0] != 'S' || !int.TryParse(child.name.Substring(1, 2), out var step))
                    {
                        continue;
                    }

                    if (!steps.TryGetValue(step, out var list)) steps[step] = list = new List<Transform>();
                    list.Add(child);
                }

                var newClip = BuildClip(steps, buildings.transform, out var data);
                var clipPath = $"{dir}/{def.Id}_Build.anim";
                // Update existing assets in place so their GUIDs (and references to them) survive a rebuild.
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip == null)
                {
                    AssetDatabase.CreateAsset(newClip, clipPath);
                    clip = newClip;
                }
                else
                {
                    clip.ClearCurves();
                    EditorUtility.CopySerialized(newClip, clip);
                    clip.name = Path.GetFileNameWithoutExtension(clipPath);
                    EditorUtility.SetDirty(clip);
                    Object.DestroyImmediate(newClip);
                }

                var ctrlPath = $"{dir}/{def.Id}_Build.controller";
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
                if (controller == null)
                {
                    controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(ctrlPath, clip);
                }
                else
                {
                    foreach (var st in controller.layers[0].stateMachine.states) st.state.motion = clip;
                    EditorUtility.SetDirty(controller);
                }
                // Hold frame 0 (nothing built) until RegionUpgradeService drives the clip through its PlayableGraph.
                foreach (var st in controller.layers[0].stateMachine.states) st.state.speed = 0f;

                var animator = buildings.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                foreach (var list in steps.Values)
                foreach (var t in list)
                    t.localScale = Vector3.zero;

                var cfg = root.AddComponent<BuildingsAnimationConfig>();
                cfg.animator = animator;
                cfg.animationClip = clip;
                cfg.data = data;

                root.transform.localPosition = RootLocalPosition;
                root.transform.localRotation = Quaternion.Euler(0f, 180f - WorldYaw + def.Yaw, 0f);
                root.transform.localScale = Vector3.one * RootScale;

                var prefabPath = $"{dir}/{def.Id}_Island_Pref.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                // Reload from disk so the in-memory prefab does not keep references to rebuilt sub-assets.
                AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                log.AppendLine($"[{def.Id}] prefab {prefabPath}: {steps.Count} steps, {steps.Values.Sum(l => l.Count)} parts, clip {clip.length:F2}s");
                return prefab;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        private static AnimationClip BuildClip(SortedDictionary<int, List<Transform>> steps, Transform animRoot,
            out List<BuildingsAnimationData> data)
        {
            var clip = new AnimationClip { frameRate = Fps, name = animRoot.name + "_Build" };
            data = new List<BuildingsAnimationData>();
            var frame = 1;

            foreach (var kv in steps)
            {
                var items = kv.Value.OrderBy(t => t.name).ToList();
                var n = items.Count;
                var stagger = n > 1 ? Mathf.Min(4f, 24f / (n - 1)) : 0f;
                var start = frame;
                var end = start + Mathf.CeilToInt(stagger * (n - 1)) + PopFrames;

                for (var j = 0; j < n; j++)
                {
                    var s0 = (start + stagger * j) / Fps;
                    var s1 = s0 + PopFrames * 0.6f / Fps;
                    var s2 = s0 + (float)PopFrames / Fps;
                    var curve = new AnimationCurve(
                        new Keyframe(0f, 0f, 0f, 0f),
                        new Keyframe(s0, 0f, 0f, 0f),
                        new Keyframe(s1, 1.12f, 0f, 0f),
                        new Keyframe(s2, 1f, 0f, 0f));
                    var path = AnimationUtility.CalculateTransformPath(items[j], animRoot);
                    clip.SetCurve(path, typeof(Transform), "m_LocalScale.x", curve);
                    clip.SetCurve(path, typeof(Transform), "m_LocalScale.y", curve);
                    clip.SetCurve(path, typeof(Transform), "m_LocalScale.z", curve);
                }

                data.Add(new BuildingsAnimationData { startFrame = start, endFrame = end });
                frame = end + StepGapFrames;
            }

            return clip;
        }

        private static GameObject InstantiateUnpacked(string fbxPath, Transform parent, string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent.gameObject.scene);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go;
        }

        private static void AssignMaterial(GameObject go, Material mat)
        {
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = new Material[r.sharedMaterials.Length == 0 ? 1 : r.sharedMaterials.Length];
                for (var i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
            }
        }

        private static void ConfigureModel(string path)
        {
            if (AssetImporter.GetAtPath(path) is not ModelImporter mi) return;
            var dirty = false;
            void Set<T>(T current, T value, System.Action<T> setter)
            {
                if (EqualityComparer<T>.Default.Equals(current, value)) return;
                setter(value);
                dirty = true;
            }

            Set(mi.globalScale, 1f, v => mi.globalScale = v);
            Set(mi.useFileScale, true, v => mi.useFileScale = v);
            Set(mi.materialImportMode, ModelImporterMaterialImportMode.None, v => mi.materialImportMode = v);
            Set(mi.importAnimation, false, v => mi.importAnimation = v);
            Set(mi.importCameras, false, v => mi.importCameras = v);
            Set(mi.importLights, false, v => mi.importLights = v);
            Set(mi.importBlendShapes, false, v => mi.importBlendShapes = v);
            Set(mi.animationType, ModelImporterAnimationType.None, v => mi.animationType = v);
            Set(mi.isReadable, false, v => mi.isReadable = v);
            Set(mi.meshCompression, ModelImporterMeshCompression.Low, v => mi.meshCompression = v);
            if (dirty) mi.SaveAndReimport();
        }

        private static Texture2D ConfigureTexture(string path)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter ti)
            {
                var changed = ti.maxTextureSize != 2048 || ti.textureType != TextureImporterType.Default || !ti.mipmapEnabled ||
                              ti.alphaSource != TextureImporterAlphaSource.None;
                ti.textureType = TextureImporterType.Default;
                ti.maxTextureSize = 2048;
                ti.mipmapEnabled = true;
                ti.alphaSource = TextureImporterAlphaSource.None;
                ti.textureCompression = TextureImporterCompression.Compressed;
                ti.sRGBTexture = true;
                var android = ti.GetPlatformTextureSettings("Android");
                if (!android.overridden || android.format != TextureImporterFormat.ASTC_6x6)
                {
                    android.overridden = true;
                    android.maxTextureSize = 2048;
                    android.format = TextureImporterFormat.ASTC_6x6;
                    ti.SetPlatformTextureSettings(android);
                    changed = true;
                }

                if (changed) ti.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material CreateMaterial(string path, Texture2D tex)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var template = AssetDatabase.LoadAssetAtPath<Material>(MaterialTemplatePath);
                mat = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Sprite RenderIcon(IslandDef def, GameObject prefab)
        {
            const int size = 512;
            var path = $"{RootDir}/{def.Id}/{def.Id}_Icon.png";
            var far = new Vector3(5000f, 0f, 5000f);
            var go = (GameObject)Object.Instantiate(prefab);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.SetPositionAndRotation(far, Quaternion.Euler(0f, 180f + def.Yaw, 0f));
            go.transform.localScale = Vector3.one;
            var cfg = go.GetComponent<BuildingsAnimationConfig>();
            if (cfg != null && cfg.animationClip != null && cfg.animator != null)
            {
                cfg.animationClip.SampleAnimation(cfg.animator.gameObject, cfg.animationClip.length);
            }

            var bounds = new Bounds(far, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);

            var camGo = new GameObject("IconCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 300f;
            var dirToCam = Quaternion.Euler(28f, 0f, 0f) * Vector3.back;
            var radius = bounds.extents.magnitude;
            var dist = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.68f;
            cam.transform.position = bounds.center + dirToCam * dist;
            cam.transform.LookAt(bounds.center);

            var lightGo = new GameObject("IconLight") { hideFlags = HideFlags.HideAndDontSave };
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.93f, 0.85f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(44f, 322f, 8f);

            var rt = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(lightGo);
            Object.DestroyImmediate(go);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(path) is TextureImporter ti)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.maxTextureSize = 512;
                ti.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void AddToRegionConfig(List<(IslandDef def, GameObject prefab, Sprite icon)> built,
            System.Text.StringBuilder log)
        {
            var config = AssetDatabase.LoadAssetAtPath<RegionConfig>(RegionConfigPath);
            if (config == null)
            {
                log.AppendLine("RegionConfig not found");
                return;
            }

            var so = new SerializedObject(config);
            var regions = so.FindProperty("regions");
            foreach (var (def, prefab, icon) in built)
            {
                SerializedProperty entry = null;
                for (var i = 0; i < regions.arraySize; i++)
                {
                    var e = regions.GetArrayElementAtIndex(i);
                    if (e.FindPropertyRelative("regionName").stringValue == def.Display)
                    {
                        entry = e;
                        break;
                    }
                }

                if (entry == null)
                {
                    regions.arraySize++;
                    entry = regions.GetArrayElementAtIndex(regions.arraySize - 1);
                    log.AppendLine($"RegionConfig: added '{def.Display}' at index {regions.arraySize - 1}");
                }
                else
                {
                    log.AppendLine($"RegionConfig: updated '{def.Display}'");
                }

                entry.FindPropertyRelative("regionName").stringValue = def.Display;
                entry.FindPropertyRelative("regionIcon").objectReferenceValue = icon;
                entry.FindPropertyRelative("regionPrefab").objectReferenceValue = prefab;
                entry.FindPropertyRelative("isComingSoon").boolValue = false;
                entry.FindPropertyRelative("starCost").intValue = def.StarCost;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }
    }
}
#endif

using System;
using System.Collections.Generic;
using Configs;
using Models;
using Systems.CurrencySystem;
using Systems.CurrencySystem.Interfaces;
using UnityEngine;

namespace Services
{
    public class IslandBuildService
    {
        private readonly RegionSO _region;
        private readonly IslandBuildProgressStore _store;
        private readonly ICurrencyService _currencyService;
        private readonly RegionService _regionService;
        private readonly RegionUpgradeService _regionUpgradeService;
        private readonly RegionModel _regionModel;
        private readonly List<PartPose> _parts = new();
        private readonly List<GameObject> _spawned = new();
        private readonly Dictionary<string, LocalPose> _treePoses = new();
        private int _capturedInstanceId;
        private Transform _animatorRoot;

        public IslandBuildService(
            RegionSO region,
            IslandBuildProgressStore store,
            ICurrencyService currencyService,
            RegionService regionService,
            RegionUpgradeService regionUpgradeService,
            RegionModel regionModel)
        {
            _region = region;
            _store = store;
            _currencyService = currencyService;
            _regionService = regionService;
            _regionUpgradeService = regionUpgradeService;
            _regionModel = regionModel;
        }

        public event Action Changed;

        public RegionSO Region => _region;

        public IslandSO ActiveIsland
        {
            get
            {
                var selected = _region != null ? _region.FindIsland(_store.ActiveIslandId) : null;
                if (selected != null && !selected.IsComingSoon)
                {
                    return selected;
                }

                return _region != null ? _region.FirstPlayable() : null;
            }
        }

        public void EnsureActiveIslandSaved()
        {
            var island = ActiveIsland;
            if (island != null)
            {
                _store.SetActiveIsland(island.IslandId);
            }
        }

        public void SelectIsland(IslandSO island)
        {
            if (island == null || island.IsComingSoon)
            {
                return;
            }

            _store.SetActiveIsland(island.IslandId);
            _regionService?.LoadPrefab(island.IslandPrefab);
            Restore();
            Changed?.Invoke();
        }

        public bool IsBuilt(BuildingSlot slot)
        {
            var island = ActiveIsland;
            return island != null && slot != null && _store.IsBuilt(island.IslandId, slot.SlotId);
        }

        public bool CanSelect(IslandSO island, BuildingSlot slot)
        {
            if (island == null || slot == null || slot.Building == null || !island.Contains(slot))
            {
                return false;
            }

            if (_store.IsBuilt(island.IslandId, slot.SlotId))
            {
                return false;
            }

            return slot.RequirementsMet(island, _store.GetBuilt(island.IslandId));
        }

        public bool CanAfford(BuildingSlot slot)
        {
            if (slot == null)
            {
                return false;
            }

            var stars = _currencyService?.GetCurrency(CurrencyType.Star)?.Value ?? 0f;
            return stars >= slot.StarCost;
        }

        public int CountBuilt(IslandSO island)
        {
            if (island == null)
            {
                return 0;
            }

            return island.CountBuilt(_store.GetBuilt(island.IslandId));
        }

        public bool TryBuild(BuildingSlot slot)
        {
            var island = ActiveIsland;
            if (!CanSelect(island, slot) || !CanAfford(slot))
            {
                return false;
            }

            if (_currencyService != null && !_currencyService.SpendCurrency(CurrencyType.Star, slot.StarCost))
            {
                return false;
            }

            _store.MarkBuilt(island.IslandId, slot.SlotId);
            Place(slot);
            Changed?.Invoke();
            return true;
        }

        public void Restore()
        {
            var island = ActiveIsland;
            if (island == null)
            {
                return;
            }

            ClearSpawned();
            Prepare();
            HidePieces();
            PlaceBuilt(island);
            _regionUpgradeService?.HoldPose();
        }

        private void PlaceBuilt(IslandSO island)
        {
            var slots = island != null ? island.Slots : null;
            var built = island != null ? _store.GetBuilt(island.IslandId) : null;
            if (slots == null || built == null)
            {
                return;
            }

            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot != null && built.Contains(slot.SlotId))
                {
                    Place(slot);
                }
            }
        }

        private void Place(BuildingSlot slot)
        {
            if (slot == null || slot.Building == null)
            {
                return;
            }

            Prepare();
            if (slot.IsTree)
            {
                SpawnPrefab(slot);
                return;
            }

            RevealPiece(slot);
            _regionUpgradeService?.HoldPose();
        }

        private void SpawnPrefab(BuildingSlot slot)
        {
            var anchor = FindAnchor(slot.ObjectName);
            var prefab = slot.Building != null ? slot.Building.Prefab : null;
            if (anchor == null || prefab == null)
            {
                return;
            }

            var pose = new LocalPose(anchor);
            if (_treePoses.TryGetValue(anchor.name, out var saved) && saved.Scale.sqrMagnitude > 0.0001f)
            {
                pose = saved;
            }

            var spawned = UnityEngine.Object.Instantiate(prefab, anchor.parent);
            spawned.name = slot.SlotId;
            spawned.SetActive(true);
            pose.Write(spawned.transform);
            CopyMaterials(anchor, spawned.transform);
            anchor.gameObject.SetActive(false);
            _spawned.Add(spawned);
        }

        private static void CopyMaterials(Transform source, Transform spawned)
        {
            var sourceRenderers = source.GetComponentsInChildren<MeshRenderer>(true);
            var spawnedRenderers = spawned.GetComponentsInChildren<MeshRenderer>(true);
            for (var i = 0; i < spawnedRenderers.Length; i++)
            {
                var spawnedRenderer = spawnedRenderers[i];
                for (var j = 0; j < sourceRenderers.Length; j++)
                {
                    if (!string.Equals(sourceRenderers[j].name, spawnedRenderer.name, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    spawnedRenderer.sharedMaterials = sourceRenderers[j].sharedMaterials;
                    spawnedRenderer.enabled = true;
                    break;
                }
            }
        }

        private void RevealPiece(BuildingSlot slot)
        {
            var lantern = KindOf(slot) == BuildKind.Lantern;
            for (var i = 0; i < _parts.Count; i++)
            {
                var part = _parts[i];
                if (part.Transform == null || !PieceMatches(part, slot, lantern))
                {
                    continue;
                }

                part.Shown.Write(part.Transform);
                part.Transform.gameObject.SetActive(true);
            }
        }

        private bool PieceMatches(PartPose part, BuildingSlot slot, bool lantern)
        {
            if (!string.IsNullOrEmpty(slot.ObjectName))
            {
                return Matches(part.Transform, slot.ObjectName);
            }

            if (lantern)
            {
                return part.Lantern;
            }

            return !part.Lantern && part.Step == slot.AnimationStep;
        }

        private void Prepare()
        {
            var instance = _regionService != null ? _regionService.ActiveInstance : null;
            if (instance == null)
            {
                return;
            }

            if (_capturedInstanceId != instance.GetInstanceID())
            {
                _parts.Clear();
                _treePoses.Clear();
            }

            RememberTrees(instance.transform);
            if (EnsureGraph())
            {
                CaptureParts();
            }

            HideTrees(instance.transform);
            _regionUpgradeService?.HoldPose();
        }

        private void RememberTrees(Transform root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var tree = transforms[i];
                if (!IsTreeRoot(tree))
                {
                    continue;
                }

                var live = new LocalPose(tree);
                if (live.Scale.sqrMagnitude > 0.0001f)
                {
                    _treePoses[tree.name] = live;
                }
            }
        }

        private void HideTrees(Transform root)
        {
            var built = BuiltTreeNames();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var tree = transforms[i];
                if (!IsTreeRoot(tree) || built.Contains(tree.name))
                {
                    continue;
                }

                tree.gameObject.SetActive(false);
            }
        }

        private HashSet<string> BuiltTreeNames()
        {
            var names = new HashSet<string>();
            var island = ActiveIsland;
            var slots = island != null ? island.Slots : null;
            var built = island != null ? _store.GetBuilt(island.IslandId) : null;
            if (slots == null || built == null)
            {
                return names;
            }

            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot != null && slot.IsTree && !string.IsNullOrEmpty(slot.ObjectName) && built.Contains(slot.SlotId))
                {
                    names.Add(slot.ObjectName);
                }
            }

            return names;
        }

        private void HidePieces()
        {
            for (var i = 0; i < _parts.Count; i++)
            {
                var part = _parts[i];
                if (part.Transform != null)
                {
                    part.Hidden.Write(part.Transform);
                }
            }
        }

        private void CaptureParts()
        {
            if (_parts.Count > 0)
            {
                return;
            }

            var animator = _regionUpgradeService != null ? _regionUpgradeService.animator : null;
            var clipSteps = _regionService != null && _regionService.ActiveRegion != null
                ? _regionService.ActiveRegion.data
                : null;
            if (animator == null || clipSteps == null || clipSteps.Count == 0)
            {
                return;
            }

            _animatorRoot = animator.transform;
            _regionUpgradeService.SetWriting(true);
            _regionUpgradeService.SeekFrame(0);
            var transforms = animator.GetComponentsInChildren<Transform>(true);
            var baseline = new Dictionary<int, LocalPose>(transforms.Length);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || transform == animator.transform || IsTreeRoot(transform))
                {
                    continue;
                }

                baseline[transform.GetInstanceID()] = new LocalPose(transform);
            }

            var assigned = new HashSet<int>();
            for (var step = 0; step < clipSteps.Count; step++)
            {
                _regionUpgradeService.SeekFrame(clipSteps[step].endFrame);
                for (var i = 0; i < transforms.Length; i++)
                {
                    var transform = transforms[i];
                    if (transform == null || transform == animator.transform || IsTreeRoot(transform))
                    {
                        continue;
                    }

                    var id = transform.GetInstanceID();
                    if (assigned.Contains(id) || !baseline.TryGetValue(id, out var hidden) || !Moved(transform, hidden))
                    {
                        continue;
                    }

                    assigned.Add(id);
                    _parts.Add(new PartPose(
                        transform,
                        step,
                        IsFonar(transform, animator.transform),
                        hidden,
                        new LocalPose(transform)));
                }
            }

            ClaimLanternStep();
        }

        private void ClaimLanternStep()
        {
            for (var i = 0; i < _parts.Count; i++)
            {
                if (_parts[i].Lantern)
                {
                    return;
                }
            }

            var lantern = FindSlot(BuildKind.Lantern);
            if (lantern == null || lantern.AnimationStep < 0)
            {
                return;
            }

            for (var i = 0; i < _parts.Count; i++)
            {
                if (_parts[i].Step == lantern.AnimationStep)
                {
                    _parts[i].Lantern = true;
                }
            }
        }

        private Transform FindAnchor(string objectName)
        {
            var instance = _regionService != null ? _regionService.ActiveInstance : null;
            if (instance == null || string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            var transforms = instance.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform != null && string.Equals(transform.name, objectName, StringComparison.OrdinalIgnoreCase))
                {
                    return transform;
                }
            }

            return null;
        }

        private bool Matches(Transform transform, string objectKey)
        {
            if (transform == null || string.IsNullOrEmpty(objectKey))
            {
                return false;
            }

            var current = transform;
            while (current != null && current != _animatorRoot)
            {
                if (string.Equals(current.name, objectKey, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private void ClearSpawned()
        {
            for (var i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] != null)
                {
                    UnityEngine.Object.Destroy(_spawned[i]);
                }
            }

            _spawned.Clear();
        }

        private bool EnsureGraph()
        {
            var instance = _regionService != null ? _regionService.ActiveInstance : null;
            if (_regionUpgradeService == null || _regionModel?.RegionService?.ActiveRegion == null || instance == null)
            {
                return false;
            }

            if (_capturedInstanceId != instance.GetInstanceID() || !_regionUpgradeService.IsReady)
            {
                _parts.Clear();
                _regionUpgradeService.Initialize(_regionModel);
                _capturedInstanceId = instance.GetInstanceID();
            }

            return _regionUpgradeService.IsReady;
        }

        private BuildingSlot FindSlot(BuildKind kind)
        {
            var slots = ActiveIsland != null ? ActiveIsland.Slots : null;
            if (slots == null)
            {
                return null;
            }

            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot != null && KindOf(slot) == kind)
                {
                    return slot;
                }
            }

            return null;
        }

        private static BuildKind KindOf(BuildingSlot slot)
        {
            var id = slot != null && slot.Building != null ? slot.Building.BuildingId : slot != null ? slot.SlotId : string.Empty;
            if (string.IsNullOrEmpty(id))
            {
                return BuildKind.House;
            }

            if (id.IndexOf("lantern", StringComparison.OrdinalIgnoreCase) >= 0
                || id.IndexOf("fonar", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return BuildKind.Lantern;
            }

            return BuildKind.House;
        }

        private static bool IsTreeRoot(Transform tree)
        {
            if (tree == null || tree.parent == null || !tree.name.StartsWith("Tree", StringComparison.Ordinal))
            {
                return false;
            }

            return !tree.parent.name.StartsWith("Tree", StringComparison.Ordinal);
        }

        private static bool IsFonar(Transform transform, Transform root)
        {
            while (transform != null && transform != root)
            {
                if (transform.name.StartsWith("fonar", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                transform = transform.parent;
            }

            return false;
        }

        private static bool Moved(Transform transform, LocalPose pose)
        {
            if ((transform.localScale - pose.Scale).sqrMagnitude > 0.01f)
            {
                return true;
            }

            if ((transform.localPosition - pose.Position).sqrMagnitude > 0.0004f)
            {
                return true;
            }

            return Quaternion.Angle(transform.localRotation, pose.Rotation) > 1f;
        }

        private enum BuildKind
        {
            House,
            Lantern
        }

        private struct LocalPose
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;

            public LocalPose(Transform transform)
            {
                Position = transform.localPosition;
                Rotation = transform.localRotation;
                Scale = transform.localScale;
            }

            public void Write(Transform transform)
            {
                transform.localPosition = Position;
                transform.localRotation = Rotation;
                transform.localScale = Scale;
            }
        }

        private sealed class PartPose
        {
            public PartPose(Transform transform, int step, bool lantern, LocalPose hidden, LocalPose shown)
            {
                Transform = transform;
                Step = step;
                Lantern = lantern;
                Hidden = hidden;
                Shown = shown;
            }

            public Transform Transform { get; }
            public int Step { get; }
            public bool Lantern { get; set; }
            public LocalPose Hidden { get; }
            public LocalPose Shown { get; }
        }
    }
}

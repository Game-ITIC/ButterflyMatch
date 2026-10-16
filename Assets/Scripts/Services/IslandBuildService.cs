using System;
using System.Collections.Generic;
using Configs;
using Cysharp.Threading.Tasks;
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
        private readonly Dictionary<int, Vector3> _treeScales = new();
        private readonly List<PartPose> _parts = new();
        private readonly HashSet<int> _shown = new();
        private int _treeInstanceId;
        private int _capturedInstanceId;
        private int _playVersion;
        private int _treeAnim;
        private bool _playing;
        private bool _playingLantern;
        private int _playingStep;
        private string _playingKey;
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
            PlayPurchased(slot);
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

            _playVersion++;
            _treeAnim++;
            _regionUpgradeService?.CancelPlayback();
            if (EnsureGraph())
            {
                CaptureParts();
                BindShown(island);
                _playing = false;
                _regionUpgradeService.HoldPose();
                ApplyMask();
            }

            ApplyTrees(null);
        }

        private void PlayPurchased(BuildingSlot slot)
        {
            var kind = KindOf(slot);
            if (kind == BuildKind.Tree)
            {
                ApplyTrees(slot.ObjectName);
                return;
            }

            if (kind == BuildKind.Lantern)
            {
                PlaySteps(null, true, true, null).Forget();
                return;
            }

            if (!string.IsNullOrEmpty(slot.ObjectName))
            {
                PlaySteps(null, false, false, slot.ObjectName).Forget();
                return;
            }

            PlaySteps(new List<int> { slot.AnimationStep }, false, false, null).Forget();
        }

        private async UniTask PlaySteps(List<int> steps, bool lantern, bool hideWholeGroup, string objectKey)
        {
            var version = ++_playVersion;
            _regionUpgradeService?.CancelPlayback();
            if (!EnsureGraph())
            {
                return;
            }

            CaptureParts();
            if (!string.IsNullOrEmpty(objectKey))
            {
                steps = StepsForKey(objectKey);
            }
            else if (hideWholeGroup)
            {
                steps = StepsFor(lantern);
            }

            if (steps == null || steps.Count == 0)
            {
                return;
            }

            BindShown(ActiveIsland);
            _playingKey = objectKey;
            if (!string.IsNullOrEmpty(objectKey))
            {
                HideKey(objectKey);
            }
            else if (hideWholeGroup)
            {
                SetGroupShown(lantern, false);
            }
            else
            {
                HideStep(lantern, steps[0]);
            }
            _regionUpgradeService.SetWriting(true);
            try
            {
                for (var i = 0; i < steps.Count; i++)
                {
                    if (version != _playVersion)
                    {
                        return;
                    }

                    var stepIndex = steps[i];
                    var step = ClipStep(stepIndex);
                    if (step == null)
                    {
                        continue;
                    }

                    _playing = true;
                    _playingLantern = lantern;
                    _playingStep = stepIndex;
                    _regionUpgradeService.JumpToFrame(step.startFrame);
                    ApplyMask();
                    await _regionUpgradeService.PlayToFrame(step.endFrame, () =>
                    {
                        if (version == _playVersion)
                        {
                            ApplyMask();
                        }
                    });

                    if (version != _playVersion)
                    {
                        return;
                    }

                    if (!string.IsNullOrEmpty(objectKey))
                    {
                        RevealKey(objectKey, stepIndex);
                    }
                    else
                    {
                        RevealStep(lantern, stepIndex);
                    }
                }
            }
            finally
            {
                if (version == _playVersion)
                {
                    _playing = false;
                    _playingKey = null;
                    _regionUpgradeService.HoldPose();
                    ApplyMask();
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
                if (transform == null || transform == animator.transform)
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
                    if (transform == null || transform == animator.transform)
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
            Debug.Log($"[IslandBuildService] Captured {_parts.Count} animated parts on {animator.gameObject.name}.");
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

        private void BindShown(IslandSO island)
        {
            _shown.Clear();
            if (IsKindBuilt(island, BuildKind.Lantern))
            {
                SetGroupShown(true, true);
            }

            var slots = island != null ? island.Slots : null;
            if (slots == null)
            {
                return;
            }

            var built = _store.GetBuilt(island.IslandId);
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null || KindOf(slot) == BuildKind.Tree || built == null || !built.Contains(slot.SlotId))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(slot.ObjectName))
                {
                    RevealKey(slot.ObjectName, -1);
                }
                else if (KindOf(slot) == BuildKind.House && slot.AnimationStep >= 0)
                {
                    RevealStep(false, slot.AnimationStep);
                }
            }
        }

        private void SetGroupShown(bool lantern, bool shown)
        {
            for (var i = 0; i < _parts.Count; i++)
            {
                var part = _parts[i];
                if (part.Lantern != lantern || part.Transform == null)
                {
                    continue;
                }

                var id = part.Transform.GetInstanceID();
                if (shown)
                {
                    _shown.Add(id);
                }
                else
                {
                    _shown.Remove(id);
                }
            }
        }

        private void HideStep(bool lantern, int step)
        {
            for (var i = 0; i < _parts.Count; i++)
            {
                var part = _parts[i];
                if (part.Lantern != lantern || part.Step != step || part.Transform == null)
                {
                    continue;
                }

                _shown.Remove(part.Transform.GetInstanceID());
            }
        }

        private void RevealStep(bool lantern, int step)
        {
            for (var i = 0; i < _parts.Count; i++)
            {
                var part = _parts[i];
                if (part.Lantern != lantern || part.Step != step || part.Transform == null)
                {
                    continue;
                }

                _shown.Add(part.Transform.GetInstanceID());
            }
        }

        private void HideKey(string objectKey)
        {
            RevealKey(objectKey, -1, false);
        }

        private void RevealKey(string objectKey, int step)
        {
            RevealKey(objectKey, step, true);
        }

        private void RevealKey(string objectKey, int step, bool shown)
        {
            for (var i = 0; i < _parts.Count; i++)
            {
                var part = _parts[i];
                if (part.Transform == null || (step >= 0 && part.Step != step) || !Matches(part.Transform, objectKey))
                {
                    continue;
                }

                var id = part.Transform.GetInstanceID();
                if (shown)
                {
                    _shown.Add(id);
                }
                else
                {
                    _shown.Remove(id);
                }
            }
        }

        private List<int> StepsForKey(string objectKey)
        {
            var steps = new List<int>();
            for (var i = 0; i < _parts.Count; i++)
            {
                var part = _parts[i];
                if (part.Transform == null || steps.Contains(part.Step) || !Matches(part.Transform, objectKey))
                {
                    continue;
                }

                steps.Add(part.Step);
            }

            steps.Sort();
            return steps;
        }

        private bool PartFollowsClip(PartPose part)
        {
            if (!string.IsNullOrEmpty(_playingKey))
            {
                return Matches(part.Transform, _playingKey);
            }

            return part.Lantern == _playingLantern;
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

        private void ApplyMask()
        {
            for (var i = 0; i < _parts.Count; i++)
            {
                var part = _parts[i];
                var transform = part.Transform;
                if (transform == null)
                {
                    continue;
                }

                if (_playing && part.Step == _playingStep && PartFollowsClip(part))
                {
                    continue;
                }

                var pose = _shown.Contains(transform.GetInstanceID()) ? part.Shown : part.Hidden;
                pose.Write(transform);
            }
        }

        private List<int> StepsFor(bool lantern)
        {
            var steps = new List<int>();
            for (var i = 0; i < _parts.Count; i++)
            {
                var part = _parts[i];
                if (part.Lantern != lantern || steps.Contains(part.Step))
                {
                    continue;
                }

                steps.Add(part.Step);
            }

            steps.Sort();
            return steps;
        }

        private BuildingsAnimationData ClipStep(int step)
        {
            var steps = _regionService != null && _regionService.ActiveRegion != null
                ? _regionService.ActiveRegion.data
                : null;
            if (steps == null || step < 0 || step >= steps.Count)
            {
                return null;
            }

            return steps[step];
        }

        private bool IsKindBuilt(IslandSO island, BuildKind kind)
        {
            var slots = island != null ? island.Slots : null;
            if (slots == null)
            {
                return false;
            }

            var built = _store.GetBuilt(island.IslandId);
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot != null && KindOf(slot) == kind && built != null && built.Contains(slot.SlotId))
                {
                    return true;
                }
            }

            return false;
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

            if (id.IndexOf("tree", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return BuildKind.Tree;
            }

            if (id.IndexOf("lantern", StringComparison.OrdinalIgnoreCase) >= 0
                || id.IndexOf("fonar", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return BuildKind.Lantern;
            }

            return BuildKind.House;
        }

        private void ApplyTrees(string animateName)
        {
            var island = ActiveIsland;
            var instance = _regionService != null ? _regionService.ActiveInstance : null;
            if (instance == null || island == null)
            {
                return;
            }

            if (_treeInstanceId != instance.GetInstanceID())
            {
                _treeScales.Clear();
                _treeInstanceId = instance.GetInstanceID();
            }

            var shown = ShownTreeNames(island);
            var transforms = instance.GetComponentsInChildren<Transform>(true);
            Transform growing = null;
            var growingScale = Vector3.zero;
            for (var i = 0; i < transforms.Length; i++)
            {
                var tree = transforms[i];
                if (!IsTreeRoot(tree))
                {
                    continue;
                }

                if (!_treeScales.TryGetValue(tree.GetInstanceID(), out var scale) || scale.sqrMagnitude <= 0.0001f)
                {
                    scale = tree.localScale;
                    if (scale.sqrMagnitude <= 0.0001f)
                    {
                        continue;
                    }

                    _treeScales[tree.GetInstanceID()] = scale;
                }

                if (!shown.Contains(tree.name))
                {
                    tree.localScale = Vector3.zero;
                    continue;
                }

                if (!string.IsNullOrEmpty(animateName) && string.Equals(tree.name, animateName, StringComparison.Ordinal))
                {
                    growing = tree;
                    growingScale = scale;
                    tree.localScale = Vector3.zero;
                    continue;
                }

                tree.localScale = scale;
            }

            if (growing != null)
            {
                AnimateTree(growing, growingScale).Forget();
            }
        }

        private HashSet<string> ShownTreeNames(IslandSO island)
        {
            var names = new HashSet<string>();
            var slots = island != null ? island.Slots : null;
            var built = island != null ? _store.GetBuilt(island.IslandId) : null;
            if (slots == null || built == null)
            {
                return names;
            }

            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null || KindOf(slot) != BuildKind.Tree || string.IsNullOrEmpty(slot.ObjectName) || !built.Contains(slot.SlotId))
                {
                    continue;
                }

                names.Add(slot.ObjectName);
            }

            return names;
        }

        private async UniTask AnimateTree(Transform tree, Vector3 scale)
        {
            var version = ++_treeAnim;
            const float duration = 0.7f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (version != _treeAnim || tree == null)
                {
                    return;
                }

                elapsed += Time.deltaTime;
                tree.localScale = Vector3.Lerp(Vector3.zero, scale, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration)));
                await UniTask.Yield();
            }

            if (version == _treeAnim && tree != null)
            {
                tree.localScale = scale;
            }
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
                _shown.Clear();
                _regionUpgradeService.Initialize(_regionModel);
                _capturedInstanceId = instance.GetInstanceID();
                _treeInstanceId = 0;
            }

            return _regionUpgradeService.IsReady;
        }

        private enum BuildKind
        {
            House,
            Lantern,
            Tree
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

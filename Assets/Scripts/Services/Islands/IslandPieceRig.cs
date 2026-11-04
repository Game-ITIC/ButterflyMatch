using System;
using System.Collections.Generic;
using Configs;
using UnityEngine;

namespace Services.Islands
{
    /// <summary>
    /// The building pieces of the active island model. Once per model instance it records, for every clip step,
    /// which transforms the step moves and their hidden / built poses, then writes each piece in its pose
    /// (built pieces shown, the rest hidden, the piece being animated left to the clip).
    /// </summary>
    internal sealed class IslandPieceRig
    {
        private readonly RegionService _regionService;
        private readonly RegionUpgradeService _regionUpgradeService;
        private readonly Func<IslandSO> _activeIsland;

        private readonly List<PartPose> _parts = new();
        private readonly Dictionary<string, LocalPose> _treePoses = new();
        private readonly HashSet<int> _shown = new();
        private int _capturedInstanceId;
        private Transform _animatorRoot;

        // The piece the build animation is playing right now: ApplyMask leaves it to the clip.
        private bool _playing;
        private bool _playingLantern;
        private int _playingStep;
        private BuildingSlot _playingSlot;

        public IslandPieceRig(RegionService regionService, RegionUpgradeService regionUpgradeService, Func<IslandSO> activeIsland)
        {
            _regionService = regionService;
            _regionUpgradeService = regionUpgradeService;
            _activeIsland = activeIsland;
        }

        /// <summary>Captures the pieces of a new model instance and hides its trees (they are spawned per slot).</summary>
        public void Prepare()
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
                _shown.Clear();
            }

            RememberTrees(instance.transform);
            if (EnsureGraph())
            {
                CaptureParts();
            }

            HideTrees(instance.transform);
            _regionUpgradeService?.HoldPose();
        }

        public bool EnsureGraph()
        {
            var instance = _regionService != null ? _regionService.ActiveInstance : null;
            if (_regionUpgradeService == null || _regionService?.ActiveRegion == null || instance == null)
            {
                return false;
            }

            if (_capturedInstanceId != instance.GetInstanceID() || !_regionUpgradeService.IsReady)
            {
                _parts.Clear();
                _regionUpgradeService.Initialize(_regionService);
                _capturedInstanceId = instance.GetInstanceID();
            }

            return _regionUpgradeService.IsReady;
        }

        #region Shown pieces

        public void ShowPieces(List<int> ids) => _shown.UnionWith(ids);

        public void HidePieces(List<int> ids) => _shown.ExceptWith(ids);

        public void ClearShown() => _shown.Clear();

        public void BeginPlaying(BuildingSlot slot)
        {
            _playingSlot = slot;
            _playingLantern = IslandModelRules.KindOf(slot) == BuildKind.Lantern;
        }

        public void PlayStep(int step)
        {
            _playing = true;
            _playingStep = step;
        }

        public void StopPlaying()
        {
            _playing = false;
            _playingSlot = null;
        }

        public void ApplyMask()
        {
            for (var i = 0; i < _parts.Count; i++)
            {
                var part = _parts[i];
                var transform = part.Transform;
                if (transform == null)
                {
                    continue;
                }

                if (_playing && _playingSlot != null && part.Step == _playingStep
                    && PieceMatches(part, _playingSlot, _playingLantern))
                {
                    continue;
                }

                var pose = _shown.Contains(transform.GetInstanceID()) ? part.Shown : part.Hidden;
                pose.Write(transform);
            }
        }

        #endregion

        #region Slot pieces

        public List<int> PieceIds(BuildingSlot slot) => CollectIds(slot, -1);

        public List<int> StepPieceIds(BuildingSlot slot, int step) => CollectIds(slot, step);

        // step < 0: every step.
        private List<int> CollectIds(BuildingSlot slot, int step)
        {
            var ids = new List<int>();
            var lantern = IslandModelRules.KindOf(slot) == BuildKind.Lantern;
            for (var i = 0; i < _parts.Count; i++)
            {
                var part = _parts[i];
                if (part.Transform != null && (step < 0 || part.Step == step) && PieceMatches(part, slot, lantern))
                {
                    ids.Add(part.Transform.GetInstanceID());
                }
            }

            return ids;
        }

        /// <summary>The clip steps that build the slot, in order.</summary>
        public List<int> PieceSteps(BuildingSlot slot)
        {
            var steps = new List<int>();
            var lantern = IslandModelRules.KindOf(slot) == BuildKind.Lantern;
            for (var i = 0; i < _parts.Count; i++)
            {
                var part = _parts[i];
                if (part.Transform != null && PieceMatches(part, slot, lantern) && !steps.Contains(part.Step))
                {
                    steps.Add(part.Step);
                }
            }

            steps.Sort();
            return steps;
        }

        public BuildingsAnimationData ClipStep(int step)
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

        private bool PieceMatches(PartPose part, BuildingSlot slot, bool lantern)
        {
            if (slot.NamesAPart)
            {
                return NamesPart(part.Transform, slot);
            }

            if (lantern)
            {
                return part.Lantern;
            }

            return !part.Lantern && part.Step == slot.AnimationStep;
        }

        private bool NamesPart(Transform transform, BuildingSlot slot)
        {
            var current = transform;
            while (current != null && current != _animatorRoot)
            {
                if (slot.Names(current.name))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        #endregion

        #region Trees and anchors

        /// <summary>The object of the active model named <paramref name="objectName"/> (case-insensitive).</summary>
        public Transform FindAnchor(string objectName)
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

        /// <summary>The pose a tree had in the model before it was hidden (its anchor may be scaled to zero).</summary>
        public bool TryGetTreePose(string treeName, out LocalPose pose)
        {
            return _treePoses.TryGetValue(treeName, out pose) && pose.Scale.sqrMagnitude > 0.0001f;
        }

        private void RememberTrees(Transform root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var tree = transforms[i];
                if (!IslandModelRules.IsTreeRoot(tree))
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

        private static void HideTrees(Transform root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var tree = transforms[i];
                if (IslandModelRules.IsTreeRoot(tree))
                {
                    tree.gameObject.SetActive(false);
                }
            }
        }

        #endregion

        #region Capture

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
                if (transform == null || transform == animator.transform || IslandModelRules.IsTreeRoot(transform))
                {
                    continue;
                }

                baseline[transform.GetInstanceID()] = new LocalPose(transform);
            }

            // A piece belongs to the first step whose end frame moves it away from frame 0.
            var assigned = new HashSet<int>();
            for (var step = 0; step < clipSteps.Count; step++)
            {
                _regionUpgradeService.SeekFrame(clipSteps[step].endFrame);
                for (var i = 0; i < transforms.Length; i++)
                {
                    var transform = transforms[i];
                    if (transform == null || transform == animator.transform || IslandModelRules.IsTreeRoot(transform))
                    {
                        continue;
                    }

                    var id = transform.GetInstanceID();
                    if (assigned.Contains(id) || !baseline.TryGetValue(id, out var hidden)
                        || !IslandModelRules.Moved(transform, hidden))
                    {
                        continue;
                    }

                    assigned.Add(id);
                    _parts.Add(new PartPose(
                        transform,
                        step,
                        IslandModelRules.IsFonar(transform, animator.transform),
                        hidden,
                        new LocalPose(transform)));
                }
            }

            ClaimLanternStep();
        }

        // Models without "fonar*" objects: the pieces of the lantern slot's step are the lantern.
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

        private BuildingSlot FindSlot(BuildKind kind)
        {
            var island = _activeIsland();
            var slots = island != null ? island.Slots : null;
            if (slots == null)
            {
                return null;
            }

            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot != null && IslandModelRules.KindOf(slot) == kind)
                {
                    return slot;
                }
            }

            return null;
        }

        #endregion
    }
}

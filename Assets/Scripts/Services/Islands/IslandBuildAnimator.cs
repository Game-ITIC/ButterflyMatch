using System;
using System.Collections.Generic;
using Configs;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Services.Islands
{
    /// <summary>
    /// Plays the build animations one after another: pieces through the island clip, trees by growing them.
    /// A slot that cannot be shown is handed back through the refund callback. <see cref="Cancel"/> drops the
    /// queue and makes running animations stop at their next step.
    /// </summary>
    internal sealed class IslandBuildAnimator
    {
        private readonly IslandPieceRig _rig;
        private readonly IslandTreeSpawner _trees;
        private readonly RegionUpgradeService _regionUpgradeService;
        private readonly Action<BuildingSlot> _refund;

        private readonly Queue<BuildingSlot> _queue = new();
        private int _version;
        private bool _running;

        public IslandBuildAnimator(
            IslandPieceRig rig,
            IslandTreeSpawner trees,
            RegionUpgradeService regionUpgradeService,
            Action<BuildingSlot> refund)
        {
            _rig = rig;
            _trees = trees;
            _regionUpgradeService = regionUpgradeService;
            _refund = refund;
        }

        public void Cancel()
        {
            _version++;
            _running = false;
            _queue.Clear();
        }

        public void Enqueue(BuildingSlot slot)
        {
            if (slot == null || slot.Building == null)
            {
                return;
            }

            _queue.Enqueue(slot);
            if (!_running)
            {
                _running = true;
                RunQueue(_version).Forget();
            }
        }

        private async UniTask RunQueue(int version)
        {
            try
            {
                while (version == _version && _queue.Count > 0)
                {
                    var slot = _queue.Dequeue();
                    _rig.Prepare();
                    if (slot.IsTree)
                    {
                        await GrowTree(slot, version);
                    }
                    else
                    {
                        await PlayPiece(slot, version);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (version == _version)
                {
                    _running = false;
                }
            }
        }

        private async UniTask PlayPiece(BuildingSlot slot, int version)
        {
            var ids = _rig.PieceIds(slot);
            var steps = _rig.PieceSteps(slot);
            if (steps.Count == 0 || !_rig.EnsureGraph())
            {
                if (version == _version)
                {
                    _refund(slot);
                }

                return;
            }

            _rig.HidePieces(ids);
            _rig.BeginPlaying(slot);
            _regionUpgradeService.SetWriting(true);
            try
            {
                for (var i = 0; i < steps.Count; i++)
                {
                    var stepIndex = steps[i];
                    var step = _rig.ClipStep(stepIndex);
                    if (step == null)
                    {
                        continue;
                    }

                    _rig.PlayStep(stepIndex);
                    _regionUpgradeService.JumpToFrame(step.startFrame);
                    _rig.ApplyMask();
                    await _regionUpgradeService.PlayToFrame(step.endFrame, () =>
                    {
                        if (version == _version)
                        {
                            _rig.ApplyMask();
                        }
                    });

                    if (version != _version)
                    {
                        return;
                    }

                    _rig.ShowPieces(_rig.StepPieceIds(slot, stepIndex));
                }
            }
            finally
            {
                if (version == _version)
                {
                    _rig.StopPlaying();
                    _rig.ShowPieces(ids);
                    _regionUpgradeService.HoldPose();
                    _rig.ApplyMask();
                }
            }
        }

        private async UniTask GrowTree(BuildingSlot slot, int version)
        {
            var spawned = await _trees.Grow(slot, () => version != _version);
            if (!spawned && version == _version)
            {
                _refund(slot);
            }
        }
    }
}

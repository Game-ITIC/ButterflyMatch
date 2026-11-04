using System;
using System.Collections.Generic;
using Configs;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Services.Islands
{
    /// <summary>
    /// Tree slots: the slot's prefab is spawned in place of the model's tree anchor (same pose and materials)
    /// and grows in with a scale tween when it is built.
    /// </summary>
    internal sealed class IslandTreeSpawner
    {
        private const float GrowSeconds = 0.9f;

        private readonly IslandPieceRig _rig;
        private readonly List<GameObject> _spawned = new();

        public IslandTreeSpawner(IslandPieceRig rig)
        {
            _rig = rig;
        }

        /// <summary>True when the tree can be placed: the anchor and prefab exist and the tree has a real size.</summary>
        public bool CanPlace(BuildingSlot slot)
        {
            var anchor = _rig.FindAnchor(slot.ObjectName);
            if (anchor == null || slot.Building.Prefab == null)
            {
                return false;
            }

            var scale = anchor.localScale;
            if (_rig.TryGetTreePose(anchor.name, out var saved))
            {
                scale = saved.Scale;
            }

            return scale.sqrMagnitude > 0.0001f;
        }

        public GameObject Spawn(BuildingSlot slot)
        {
            var anchor = _rig.FindAnchor(slot.ObjectName);
            var prefab = slot.Building != null ? slot.Building.Prefab : null;
            if (anchor == null || prefab == null)
            {
                return null;
            }

            var pose = new LocalPose(anchor);
            if (_rig.TryGetTreePose(anchor.name, out var saved))
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
            return spawned;
        }

        /// <summary>Spawns the tree and grows it in. False when it could not be spawned.</summary>
        public async UniTask<bool> Grow(BuildingSlot slot, Func<bool> cancelled)
        {
            var spawned = Spawn(slot);
            if (spawned == null)
            {
                return false;
            }

            var target = spawned.transform.localScale;
            spawned.transform.localScale = Vector3.zero;
            var tween = spawned.transform
                .DOScale(target, GrowSeconds)
                .SetEase(Ease.OutBack)
                .SetTarget(spawned);
            while (tween.IsActive() && !tween.IsComplete())
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
                if (cancelled() || spawned == null)
                {
                    tween.Kill();
                    return true;
                }
            }

            return true;
        }

        public void Clear()
        {
            for (var i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i] != null)
                {
                    _spawned[i].transform.DOKill();
                    UnityEngine.Object.Destroy(_spawned[i]);
                }
            }

            _spawned.Clear();
        }

        // The prefab keeps the model's look: renderers with the same name get the anchor's materials.
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
    }
}

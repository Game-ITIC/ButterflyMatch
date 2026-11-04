using System;
using Configs;
using UnityEngine;

namespace Services.Islands
{
    internal enum BuildKind
    {
        House,
        Lantern
    }

    /// <summary>Local position, rotation and scale of a transform.</summary>
    internal struct LocalPose
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

    /// <summary>A building piece of the island model: which clip step builds it and its hidden / built poses.</summary>
    internal sealed class PartPose
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

    /// <summary>Naming rules of the island models (trees, lanterns) and pose comparison.</summary>
    internal static class IslandModelRules
    {
        public static BuildKind KindOf(BuildingSlot slot)
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

        /// <summary>A "Tree*" object whose parent is not a tree itself (the root of one tree).</summary>
        public static bool IsTreeRoot(Transform tree)
        {
            if (tree == null || tree.parent == null || !tree.name.StartsWith("Tree", StringComparison.Ordinal))
            {
                return false;
            }

            return !tree.parent.name.StartsWith("Tree", StringComparison.Ordinal);
        }

        public static bool IsFonar(Transform transform, Transform root)
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

        public static bool Moved(Transform transform, LocalPose pose)
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
    }
}

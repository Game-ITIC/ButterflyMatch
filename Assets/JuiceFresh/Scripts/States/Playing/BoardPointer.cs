using UnityEngine;

namespace JuiceFresh.States.Playing
{
    /// <summary>What is under the player's finger: a piece (layer "Item") or a cell (layer "Default").</summary>
    public sealed class BoardPointer
    {
        private readonly Camera _camera;
        private readonly int _itemMask = 1 << LayerMask.NameToLayer("Item");
        private readonly int _squareMask = 1 << LayerMask.NameToLayer("Default");

        public BoardPointer(Camera camera)
        {
            _camera = camera;
        }

        public bool IsPressed => Input.GetMouseButton(0);

        public bool WasReleased => Input.GetMouseButtonUp(0);

        public Item ItemUnderPointer() => ComponentUnderPointer<Item>(_itemMask);

        public Square SquareUnderPointer() => ComponentUnderPointer<Square>(_squareMask);

        private T ComponentUnderPointer<T>(int mask) where T : Component
        {
            Vector2 worldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(worldPos, mask);
            return hit != null ? hit.GetComponent<T>() : null;
        }
    }
}

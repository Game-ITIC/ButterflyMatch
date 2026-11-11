using System.Collections.Generic;
using UnityEngine;

namespace GameTutorials
{
    /// <summary>
    /// Darkens every board sprite except the shown pieces and their cells. Tinting, not an overlay: the chain
    /// line, selection glow and counter sit on the board's own sorting orders and stay visible.
    /// </summary>
    public sealed class BoardDimmer
    {
        private const float Brightness = 0.35f;

        private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly HashSet<Transform> _kept = new HashSet<Transform>();

        public void Dim(Transform board, IEnumerable<Item> keep)
        {
            Restore();
            if (board == null)
                return;

            _kept.Clear();
            foreach (Item item in keep)
            {
                if (item == null)
                    continue;

                _kept.Add(item.transform);
                if (item.square != null)
                    _kept.Add(item.square.transform);
            }

            foreach (SpriteRenderer renderer in board.GetComponentsInChildren<SpriteRenderer>())
            {
                if (IsKept(renderer.transform, board))
                    continue;

                Color color = renderer.color;
                _renderers.Add(renderer);
                _colors.Add(color);
                renderer.color = new Color(color.r * Brightness, color.g * Brightness, color.b * Brightness, color.a);
            }
        }

        public void Restore()
        {
            for (int i = 0; i < _renderers.Count; i++)
            {
                // Pieces of the played chain are destroyed by then.
                if (_renderers[i] != null)
                    _renderers[i].color = _colors[i];
            }

            _renderers.Clear();
            _colors.Clear();
            _kept.Clear();
        }

        private bool IsKept(Transform t, Transform board)
        {
            for (; t != null && t != board; t = t.parent)
            {
                if (_kept.Contains(t))
                    return true;
            }

            return false;
        }
    }
}

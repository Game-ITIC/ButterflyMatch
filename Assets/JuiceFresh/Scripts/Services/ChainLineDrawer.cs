using System.Collections.Generic;

namespace JuiceFresh.Scripts
{
    /// <summary>Draws the line through the pieces of the chain the player is dragging.</summary>
    public sealed class ChainLineDrawer
    {
        private readonly Line _line;

        public ChainLineDrawer(Line line)
        {
            _line = line;
        }

        public void Draw(List<Item> chain)
        {
            if (_line == null)
                return;

            if (chain.Count == 0)
            {
                _line.SetVertexCount(0);
                return;
            }

            _line.SetVertexCount(chain.Count);

            int point = 0;
            foreach (Item item in chain)
            {
                if (item == null)
                    continue;

                _line.AddPoint(item.transform.position, point);
                point++;
            }
        }
    }
}

using System.Collections.Generic;

namespace JuiceFresh.States.Playing
{
    /// <summary>
    /// Checks whether the board still has a chain of 3: three plain pieces of one colour in a straight line
    /// (horizontal, vertical or diagonal). When none is left the board is regenerated.
    /// </summary>
    public sealed class PossibleMovesDetector
    {
        private const int MinChain = 3;

        // The hint shows a good chain, not the longest one: searching further costs a lot on a one-colour board.
        private const int EnoughForHint = 5;
        private const int MaxSearchNodes = 2000;

        private readonly LevelManager _levelManager;
        private readonly List<Item> _path = new List<Item>();
        private readonly List<Item> _bestPath = new List<Item>();
        private int _searchNodes;

        public PossibleMovesDetector(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }

        public bool HasPossibleMoves()
        {
            for (int row = 0; row < _levelManager.maxRows; row++)
            {
                for (int col = 0; col < _levelManager.maxCols; col++)
                {
                    Square start = _levelManager.GetSquare(col, row);
                    if (start?.item != null && start.item.currentType == ItemsTypes.NONE &&
                        HasLineThrough(col, row, start.item.color))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Finds a chain the player can drag right now, by the rules of the drag: 8 neighbours, one colour,
        /// no ingredients. Starts at a random cell so that the hint does not always point at the same corner.
        /// </summary>
        public bool TryFindChain(List<Item> chain)
        {
            chain.Clear();
            _bestPath.Clear();
            _searchNodes = 0;

            int cells = _levelManager.maxRows * _levelManager.maxCols;
            int first = UnityEngine.Random.Range(0, cells);
            for (int i = 0; i < cells && _bestPath.Count < EnoughForHint && _searchNodes < MaxSearchNodes; i++)
            {
                int cell = (first + i) % cells;
                Item start = ChainableItem(cell % _levelManager.maxCols, cell / _levelManager.maxCols);
                if (start != null)
                    Extend(start, start.color);
            }

            if (_bestPath.Count < MinChain)
                return false;

            chain.AddRange(_bestPath);
            return true;
        }

        private void Extend(Item item, int color)
        {
            _path.Add(item);
            _searchNodes++;

            if (_path.Count > _bestPath.Count)
            {
                _bestPath.Clear();
                _bestPath.AddRange(_path);
            }

            int col = item.square.col;
            int row = item.square.row;
            for (int dRow = -1; dRow <= 1; dRow++)
            {
                for (int dCol = -1; dCol <= 1; dCol++)
                {
                    if (_bestPath.Count >= EnoughForHint || _searchNodes >= MaxSearchNodes)
                        break;

                    Item next = ChainableItem(col + dCol, row + dRow);
                    if (next != null && next.color == color && !_path.Contains(next))
                        Extend(next, color);
                }
            }

            _path.RemoveAt(_path.Count - 1);
        }

        private Item ChainableItem(int col, int row)
        {
            if (col < 0 || row < 0 || col >= _levelManager.maxCols || row >= _levelManager.maxRows)
                return null;

            Square square = _levelManager.GetSquare(col, row);
            Item item = square != null ? square.item : null;
            if (item == null || item.destroying || item.currentType == ItemsTypes.INGREDIENT)
                return null;

            // Colour bombs carry a service colour (555).
            return item.color >= 0 && item.color < item.items.Length ? item : null;
        }

        private bool HasLineThrough(int col, int row, int color)
        {
            return LineLength(col, row, 1, 0, color) >= MinChain   // horizontal
                || LineLength(col, row, 0, 1, color) >= MinChain   // vertical
                || LineLength(col, row, 1, 1, color) >= MinChain   // diagonal "\"
                || LineLength(col, row, 1, -1, color) >= MinChain; // diagonal "/"
        }

        // Pieces of the colour on the line through (col,row), in both directions, plus the start piece.
        private int LineLength(int col, int row, int dCol, int dRow, int color) =>
            1 + CountMatches(col, row, dCol, dRow, color) + CountMatches(col, row, -dCol, -dRow, color);

        private int CountMatches(int startCol, int startRow, int dCol, int dRow, int color)
        {
            int count = 0;
            int c = startCol + dCol;
            int r = startRow + dRow;

            while (true)
            {
                Square square = _levelManager.GetSquare(c, r);
                if (square == null || square.item == null)
                    break;

                if (square.item.color != color || square.item.currentType == ItemsTypes.INGREDIENT)
                    break;

                count++;
                c += dCol;
                r += dRow;
            }

            return count;
        }
    }
}

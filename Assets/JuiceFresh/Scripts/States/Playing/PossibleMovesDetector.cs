namespace JuiceFresh.States.Playing
{
    /// <summary>
    /// Checks whether the board still has a chain of 3: three plain pieces of one colour in a straight line
    /// (horizontal, vertical or diagonal). When none is left the board is regenerated.
    /// </summary>
    public sealed class PossibleMovesDetector
    {
        private const int MinChain = 3;

        private readonly LevelManager _levelManager;

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

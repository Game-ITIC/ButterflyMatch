using System.Collections.Generic;
using UnityEngine;

namespace JuiceFresh.Board
{
    /// <summary>
    /// Thriving blocks grow onto one neighbouring plain square after a move in which none was destroyed.
    /// </summary>
    public class ThrivingBlockSpreader
    {
        private readonly LevelManager _levelManager;

        public ThrivingBlockSpreader(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }

        public void SpreadAfterMove()
        {
            if (!_levelManager.thrivingBlockDestroyed)
                TrySpreadOnce();

            _levelManager.thrivingBlockDestroyed = false;
        }

        private void TrySpreadOnce()
        {
            for (int col = 0; col < _levelManager.maxCols; col++)
            {
                for (int row = _levelManager.maxRows - 1; row >= 0; row--)
                {
                    Square square = _levelManager.GetSquare(col, row);
                    if (square == null || square.type != SquareTypes.THRIVING)
                        continue;

                    List<Square> around = _levelManager.GetSquaresAround(square);
                    foreach (Square sq in around)
                    {
                        if (sq.CanFallInto() && Random.Range(0, 5) == 0 && sq.type == SquareTypes.EMPTY &&
                            sq.item != null && sq.item.currentType == ItemsTypes.NONE)
                        {
                            _levelManager.CreateObstacles(sq.col, sq.row, sq.gameObject, SquareTypes.THRIVING);
                            return;
                        }
                    }
                }
            }
        }
    }
}

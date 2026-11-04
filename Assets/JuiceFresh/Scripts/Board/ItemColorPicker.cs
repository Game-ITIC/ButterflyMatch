using System.Collections.Generic;
using UnityEngine;

namespace JuiceFresh.Board
{
    /// <summary>
    /// Chooses the colour of a new piece, preferring colours its neighbours don't have.
    /// </summary>
    public static class ItemColorPicker
    {
        public static int Pick(LevelManager lm, int col, int row, int exceptColor)
        {
            List<int> remainColors = AvailableColors(lm, col, row, exceptColor);

            // With no colour left this can still return exceptColor (the legacy "(c++) % n" retry was a no-op).
            return remainColors.Count > 0
                ? remainColors[Random.Range(0, remainColors.Count)]
                : Random.Range(0, lm.colorLimit);
        }

        private static List<int> AvailableColors(LevelManager lm, int col, int row, int exceptColor)
        {
            List<int> remainColors = new List<int>();

            for (int i = 0; i < lm.colorLimit; i++)
            {
                bool canGen = true;

                // LEGACY: GetSquare takes (col, row), but these lookups pass (row, col), so they look at the mirrored
                // square, and the bounds checks are off by one. Real neighbours were never checked. Left as is because
                // fixing it changes how boards are generated; it needs a bot check before it can change.
                if (col > 1)
                {
                    Square neighbor = lm.GetSquare(row, col - 1);
                    if (neighbor != null && neighbor.item != null && neighbor.CanGoInto() && neighbor.item.color == i)
                        canGen = false;
                }

                if (col < lm.maxCols - 1)
                {
                    Square neighbor = lm.GetSquare(row, col + 1);
                    if (neighbor != null && neighbor.item != null && neighbor.CanGoOut() && neighbor.item.color == i)
                        canGen = false;
                }

                if (row < lm.maxRows)
                {
                    Square neighbor = lm.GetSquare(row + 1, col);
                    if (neighbor != null && neighbor.item != null && neighbor.CanGoOut() && neighbor.item.color == i)
                        canGen = false;
                }

                if (canGen && i != exceptColor)
                    remainColors.Add(i);
            }

            return remainColors;
        }
    }
}

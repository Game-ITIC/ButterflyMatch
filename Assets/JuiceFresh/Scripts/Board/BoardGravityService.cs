using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace JuiceFresh.Board
{
    /// <summary>
    /// Gravity steps of a cascade pass: straight falls, refill from the top, diagonal slides into holes
    /// and waiting until every piece has landed.
    /// </summary>
    public class BoardGravityService
    {
        // Each FallOut moves a piece one square, so 20 sweeps cover the tallest board.
        private const int FallSweeps = 20;

        private readonly LevelManager _levelManager;

        public BoardGravityService(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }

        /// <summary>Moves pieces down in the square links; the visuals follow in <see cref="StartFallingAll"/>.</summary>
        public void ApplyFalls()
        {
            for (int i = 0; i < FallSweeps; i++)
            {
                for (int col = 0; col < _levelManager.maxCols; col++)
                {
                    for (int row = _levelManager.maxRows - 1; row >= 0; row--)
                    {
                        Square square = _levelManager.GetSquare(col, row);
                        if (square != null)
                            square.FallOut();
                    }
                }
            }
        }

        /// <summary>Starts the fall animation of every piece that is not at its square yet.</summary>
        public void StartFallingAll()
        {
            for (int col = 0; col < _levelManager.maxCols; col++)
            {
                for (int row = _levelManager.maxRows - 1; row >= 0; row--)
                {
                    Square square = _levelManager.GetSquare(col, row);
                    if (square != null && !square.IsNone() && square.item != null)
                        square.item.StartFalling();
                }
            }
        }

        public void Refill()
        {
            _levelManager.GenerateNewItems();
        }

        /// <summary>Lets pieces slide diagonally into empty squares. True when any piece moved.</summary>
        public bool SlideIntoHoles()
        {
            bool moved = false;
            for (int col = 0; col < _levelManager.maxCols; col++)
            {
                for (int row = _levelManager.maxRows - 1; row >= 0; row--)
                {
                    Square square = _levelManager.GetSquare(col, row);
                    if (square != null && !square.IsNone() && square.item != null && square.item.GetNearEmptySquares())
                        moved = true;
                }
            }

            return moved;
        }

        /// <summary>
        /// Fills empty squares under a blocker (no refill from the top). Runs after the cascade settled, when
        /// every possible diagonal slide has already happened, so such a square is starved: it used to stay
        /// empty until a striped blast broke the blocker. The new piece appears in place. True when any was filled.
        /// </summary>
        public bool FillStarvedHoles()
        {
            bool filled = false;
            for (int col = 0; col < _levelManager.maxCols; col++)
            {
                for (int row = 0; row < _levelManager.maxRows; row++)
                {
                    Square square = _levelManager.GetSquare(col, row);
                    if (square == null || square.IsNone() || !square.CanGoInto() || square.item != null)
                        continue;

                    // A diagonal neighbour that could feed it may itself have been filled just now in this
                    // sweep (L225), with no slide pass left to move its piece in, so don't wait for feeders.
                    if (!square.IsHaveSolidAbove())
                        continue;

                    Item item = square.GenItem(false);
                    if (item != null)
                    {
                        item.PlayAppearAnimation();
                        filled = true;
                    }
                }
            }

            return filled;
        }

        /// <summary>
        /// True while the pass is not really over: a piece is still being destroyed, or an open square that
        /// refills from the top is empty (something destroyed it after this pass refilled).
        /// </summary>
        public bool HasUnsettledSquares()
        {
            for (int col = 0; col < _levelManager.maxCols; col++)
            {
                for (int row = 0; row < _levelManager.maxRows; row++)
                {
                    Square square = _levelManager.GetSquare(col, row);
                    if (square == null || square.IsNone())
                        continue;

                    if (square.item == null)
                    {
                        if (square.CanGoInto() && !square.IsHaveSolidAbove())
                            return true;
                    }
                    else if (square.item.destroying)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool AllItemsLanded()
        {
            // Read-only loop: nothing here can add or remove pieces, so the live list is safe.
            var items = Item.Active;
            for (int i = 0; i < items.Count; i++)
            {
                Item item = items[i];
                if (item == null || item.falling)
                    return false;
            }

            return true;
        }

        public async UniTask WaitAllLanded(int pollMs, CancellationToken cancellationToken)
        {
            while (!AllItemsLanded())
                await UniTask.Delay(pollMs, cancellationToken: cancellationToken);
        }
    }
}

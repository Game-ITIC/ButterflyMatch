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

        public bool AllItemsLanded()
        {
            GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
            foreach (GameObject go in items)
            {
                Item item = go.GetComponent<Item>();
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

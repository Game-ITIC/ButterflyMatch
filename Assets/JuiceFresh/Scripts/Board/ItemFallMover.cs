using System.Collections;
using UnityEngine;

namespace JuiceFresh.Board
{
    /// <summary>
    /// Moves a piece between squares: the square links, the fall animation and diagonal slides into holes.
    /// </summary>
    public class ItemFallMover
    {
        private const float StartSpeed = 10f;
        private const float Acceleration = 0.2f;
        private const float MinAnimatedDistance = 0.5f;
        // The piece counts as landed after 30% of the way so the next cascade step can start early.
        private const float LandedFraction = 0.3f;

        private readonly Item _item;

        public ItemFallMover(Item item)
        {
            _item = item;
        }

        public void MoveLinkTo(Square target)
        {
            target.item = _item;
            _item.square.item = null;
            _item.square = target;
        }

        public IEnumerator FallTo(Square target, bool animate)
        {
            _item.falling = true;
            Transform transform = _item.transform;
            float startTime = Time.time;
            Vector3 startPos = transform.position;
            float speed = StartSpeed;

            float distance = Vector3.Distance(startPos, target.transform.position);
            float fracJourney = 0;

            if (distance > MinAnimatedDistance)
            {
                while (fracJourney < 1)
                {
                    speed += Acceleration;
                    float distCovered = (Time.time - startTime) * speed;
                    fracJourney = distCovered / distance;
                    transform.position = Vector3.Lerp(startPos, target.transform.position + Vector3.back * 0.2f, fracJourney);
                    yield return new WaitForFixedUpdate();
                    if (fracJourney > LandedFraction)
                        _item.falling = false;
                }
            }

            if (distance > MinAnimatedDistance && animate)
            {
                _item.Animation.PlayLanding();
                SoundBase.Instance.PlaySound(SoundBase.Instance.drop[Random.Range(0, SoundBase.Instance.drop.Length)]);
            }

            _item.falling = false;
            _item.justCreatedItem = false;
        }

        /// <summary>Slides the piece to an empty square diagonally below it (right first). True when it moved.</summary>
        public bool SlideIntoNearHole()
        {
            if (!_item.square.CanGoOut())
                return false;

            bool moved = TrySlide(1);
            if (TrySlide(-1))
                moved = true;

            return moved;
        }

        private bool TrySlide(int dCol)
        {
            LevelManager lm = LevelManager.Instance;
            Square from = _item.square;
            bool inBounds = from.row < lm.maxRows - 1 && (dCol > 0 ? from.col < lm.maxCols : from.col > 0);
            if (!inBounds || _item.falling)
                return false;

            // safe lookup clamps at the board edge (the right edge then checks the square straight below).
            Square target = lm.GetSquare(from.col + dCol, from.row + 1, true);
            if (!target.CanFallInto() || target.item != null)
                return false;

            from.item = null;
            target.item = _item;
            _item.square = target;
            _item.StartFalling();
            return true;
        }
    }
}

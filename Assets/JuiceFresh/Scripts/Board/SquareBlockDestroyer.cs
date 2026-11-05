using UnityEngine;

namespace JuiceFresh.Board
{
    /// <summary>
    /// Destroys the top block layer of a square (jelly block, cage, solid or thriving block) when the piece
    /// on it is destroyed. Plain squares also break the solid / thriving blocks next to them.
    /// </summary>
    public class SquareBlockDestroyer
    {
        private readonly Square _square;

        public SquareBlockDestroyer(Square square)
        {
            _square = square;
        }

        public void DestroyBlock()
        {
            SquareTypes type = _square.type;
            if (type == SquareTypes.UNDESTROYABLE)
                return;

            if (type != SquareTypes.SOLIDBLOCK && type != SquareTypes.THRIVING)
                DestroyAdjacentSolidBlocks();

            if (_square.block.Count <= 0)
                return;

            DestroyTopLayer();
            RemoveTopLayer();
        }

        private void DestroyAdjacentSolidBlocks()
        {
            foreach (Square sq in _square.GetAllNeghbors())
            {
                if (sq.type == SquareTypes.SOLIDBLOCK || sq.type == SquareTypes.THRIVING)
                    sq.DestroyBlock();
            }
        }

        private void DestroyTopLayer()
        {
            LevelManager lm = LevelManager.THIS;
            GameObject top = _square.block[_square.block.Count - 1];
            Vector3 position = _square.transform.position;

            switch (_square.type)
            {
                case SquareTypes.BLOCK:
                    top.GetComponent<Square>().SetFrontLayer(false);
                    lm.CheckCollectedTarget(top.gameObject);
                    lm.PopupScore(lm.scoreForBlock, position, 0);
                    lm.TargetBlocks--;
                    top.GetComponent<SpriteRenderer>().enabled = false;
                    break;

                case SquareTypes.WIREBLOCK:
                    if (_square.cageHP > 0)
                        return;

                    lm.TargetCages--;
                    lm.PopupScore(lm.scoreForWireBlock, position, 0);
                    break;

                case SquareTypes.SOLIDBLOCK:
                    lm.PopupScore(lm.scoreForSolidBlock, position, 0);
                    ThrowBrick(top);
                    break;

                case SquareTypes.THRIVING:
                    lm.PopupScore(lm.scoreForThrivingBlock, position, 0);
                    top.SetActive(false);
                    GameObject boom = Object.Instantiate(_square.boomPrefab, position, Quaternion.identity);
                    Object.Destroy(boom, 1);
                    break;

                default:
                    ThrowBrick(top);
                    break;
            }
        }

        private void ThrowBrick(GameObject top)
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.block_destroy);

            if (_square.type == SquareTypes.THRIVING)
                return;

            top.GetComponent<Animation>().Play("BrickRotate");
            top.GetComponent<SpriteRenderer>().sortingOrder = 4;
            top.AddComponent<Rigidbody2D>();
            top.GetComponent<Rigidbody2D>().AddRelativeForce(
                new Vector2(Random.insideUnitCircle.x * Random.Range(30, 200), Random.Range(100, 150)),
                ForceMode2D.Force);
        }

        private void RemoveTopLayer()
        {
            GameObject top = _square.block[_square.block.Count - 1];
            Object.Destroy(top, 1.5f);
            _square.block.Remove(top);

            _square.type = _square.block.Count > 0
                ? _square.block[_square.block.Count - 1].GetComponent<Square>().type
                : SquareTypes.EMPTY;

            // The double layer is gone: the jar under it comes to the front.
            if (_square.type == SquareTypes.BLOCK)
                _square.block[_square.block.Count - 1].GetComponent<Square>().SetFrontLayer(true);
        }
    }
}

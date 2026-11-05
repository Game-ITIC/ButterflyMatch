using JuiceFresh;
using Models;
using UnityEngine;

namespace JuiceFresh.Scripts
{
    public class BoardFactoryService
    {
        // World units the board moves down while the booster row is hidden: half of the row's height.
        private const float NoBoostersShift = 1.6f;

        private readonly LevelManager _levelManager;

        public BoardFactoryService(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }

        public Vector3 GenerateLevel()
        {
            bool chessColor = false;
            float sqWidth = 1.6f;
            float halfSquare = sqWidth / 2;
            Vector3 fieldPos = new Vector3(-_levelManager.maxCols * sqWidth / 2 + halfSquare,
                _levelManager.maxRows / 1.4f, -10);

            for (int row = 0; row < _levelManager.maxRows; row++)
            {
                if (_levelManager.maxCols % 2 == 0)
                    chessColor = !chessColor;

                for (int col = 0; col < _levelManager.maxCols; col++)
                {
                    CreateSquare(col, row, chessColor);
                    chessColor = !chessColor;
                }
            }

            float yOffset = 0;
            if (_levelManager.target == Target.COLLECT)
                yOffset = 0.3f;

            // Before the first booster opens, the row under the board is empty.
            if (!BoosterUnlocks.IsUnlocked(BoostType.Shovel, _levelManager.currentLevel))
                yOffset -= NoBoostersShift;

            return new Vector3(fieldPos.x, fieldPos.y + yOffset, _levelManager.GameField.localPosition.z);
        }

        void CreateSquare(int col, int row, bool chessColor = false)
        {
            Vector2 cellPosition = _levelManager.firstSquarePosition +
                                   new Vector2(col * _levelManager.squareWidth, -row * _levelManager.squareHeight);

            GameObject square = Object.Instantiate(_levelManager.squarePrefab, cellPosition, Quaternion.identity);

            if (chessColor)
                square.GetComponent<SpriteRenderer>().sprite = _levelManager.squareSprite1;

            square.transform.SetParent(_levelManager.GameField);
            square.transform.localPosition = cellPosition;
            _levelManager.squaresArray[row * _levelManager.maxCols + col] = square.GetComponent<Square>();
            square.GetComponent<Square>().row = row;
            square.GetComponent<Square>().col = col;
            square.GetComponent<Square>().type = SquareTypes.EMPTY;

            SquareBlocks[] levelSquaresFile = _levelManager.LevelSquaresFile;
            if (levelSquaresFile[row * _levelManager.maxCols + col].block == SquareTypes.EMPTY)
            {
                CreateObstacles(col, row, square, SquareTypes.NONE);
            }
            else if (levelSquaresFile[row * _levelManager.maxCols + col].block == SquareTypes.NONE)
            {
                square.GetComponent<SpriteRenderer>().enabled = false;
                square.GetComponent<Square>().type = SquareTypes.NONE;
            }
            else if (levelSquaresFile[row * _levelManager.maxCols + col].block == SquareTypes.BLOCK)
            {
                GameObject block = Object.Instantiate(_levelManager.blockPrefab, cellPosition, Quaternion.identity);
                block.transform.SetParent(square.transform);
                block.transform.localPosition = new Vector3(0, 0, -0.01f);
                square.GetComponent<Square>().block.Add(block);
                square.GetComponent<Square>().type = SquareTypes.BLOCK;
                block.GetComponent<Square>().type = SquareTypes.BLOCK;
                block.GetComponent<Square>().SetFrontLayer(true);

                CreateObstacles(col, row, square, SquareTypes.NONE);
            }
            else if (levelSquaresFile[row * _levelManager.maxCols + col].block == SquareTypes.DOUBLEBLOCK)
            {
                GameObject block = Object.Instantiate(_levelManager.blockPrefab, cellPosition, Quaternion.identity);
                block.transform.SetParent(square.transform);
                block.transform.localPosition = new Vector3(0, 0, -0.01f);
                square.GetComponent<Square>().block.Add(block);
                square.GetComponent<Square>().type = SquareTypes.BLOCK;
                block.GetComponent<Square>().type = SquareTypes.BLOCK;

                block = Object.Instantiate(_levelManager.blockPrefab, cellPosition, Quaternion.identity);
                block.transform.SetParent(square.transform);
                block.transform.localPosition = new Vector3(0, 0, -0.01f);
                square.GetComponent<Square>().block.Add(block);
                square.GetComponent<Square>().type = SquareTypes.BLOCK;
                block.GetComponent<Square>().type = SquareTypes.BLOCK;

                block.GetComponent<SpriteRenderer>().sprite = _levelManager.doubleBlock;
                block.GetComponent<SpriteRenderer>().sortingOrder = 1;
                // The double layer is a plain tile, it has no jar in front.
                block.GetComponent<Square>().frontLayer = null;

                CreateObstacles(col, row, square, SquareTypes.NONE);
            }
        }

        public void CreateObstacles(int col, int row, GameObject square, SquareTypes type)
        {
            Vector2 cellPosition = _levelManager.firstSquarePosition +
                                   new Vector2(col * _levelManager.squareWidth, -row * _levelManager.squareHeight);
            SquareBlocks[] levelSquaresFile = _levelManager.LevelSquaresFile;

            if ((levelSquaresFile[row * _levelManager.maxCols + col].obstacle == SquareTypes.WIREBLOCK &&
                 type == SquareTypes.NONE) ||
                type == SquareTypes.WIREBLOCK)
            {
                GameObject block = Object.Instantiate(_levelManager.wireBlockPrefab, cellPosition, Quaternion.identity);
                block.transform.SetParent(square.transform);
                block.transform.localPosition = new Vector3(0, 0, -0.5f);
                square.GetComponent<Square>().block.Add(block);
                square.GetComponent<Square>().type = SquareTypes.WIREBLOCK;
                block.GetComponent<SpriteRenderer>().sortingOrder = 3;
                block.GetComponent<Square>().type = SquareTypes.WIREBLOCK;
                square.GetComponent<Square>().SetCage(_levelManager.CageHp);
            }
            else if ((levelSquaresFile[row * _levelManager.maxCols + col].obstacle == SquareTypes.SOLIDBLOCK &&
                      type == SquareTypes.NONE) ||
                     type == SquareTypes.SOLIDBLOCK)
            {
                GameObject block = Object.Instantiate(_levelManager.solidBlockPrefab, cellPosition, Quaternion.identity);
                block.transform.SetParent(square.transform);
                block.transform.localPosition = new Vector3(0, 0, -0.5f);
                square.GetComponent<Square>().block.Add(block);
                block.GetComponent<SpriteRenderer>().sortingOrder = 3;
                square.GetComponent<Square>().type = SquareTypes.SOLIDBLOCK;
                block.GetComponent<Square>().type = SquareTypes.SOLIDBLOCK;
            }
            else if ((levelSquaresFile[row * _levelManager.maxCols + col].obstacle == SquareTypes.DOUBLESOLIDBLOCK &&
                      type == SquareTypes.NONE) ||
                     type == SquareTypes.DOUBLESOLIDBLOCK)
            {
                GameObject block = Object.Instantiate(_levelManager.solidBlockPrefab, cellPosition, Quaternion.identity);
                block.transform.SetParent(square.transform);
                block.transform.localPosition = new Vector3(0, 0, -0.5f);
                square.GetComponent<Square>().block.Add(block);
                block.GetComponent<SpriteRenderer>().sortingOrder = 3;
                square.GetComponent<Square>().type = SquareTypes.SOLIDBLOCK;
                block.GetComponent<Square>().type = SquareTypes.SOLIDBLOCK;

                block = Object.Instantiate(_levelManager.solidBlockPrefab, cellPosition, Quaternion.identity);
                block.transform.SetParent(square.transform);
                block.transform.localPosition = new Vector3(0, 0, -0.5f);
                square.GetComponent<Square>().block.Add(block);
                block.GetComponent<SpriteRenderer>().sprite = _levelManager.doubleSolidBlock;
                block.GetComponent<SpriteRenderer>().sortingOrder = 4;
                square.GetComponent<Square>().type = SquareTypes.SOLIDBLOCK;
                block.GetComponent<Square>().type = SquareTypes.SOLIDBLOCK;
            }
            else if ((levelSquaresFile[row * _levelManager.maxCols + col].obstacle == SquareTypes.UNDESTROYABLE &&
                      type == SquareTypes.NONE) ||
                     type == SquareTypes.UNDESTROYABLE)
            {
                GameObject block = Object.Instantiate(_levelManager.undesroyableBlockPrefab, cellPosition,
                    Quaternion.identity);
                block.transform.SetParent(square.transform);
                block.transform.localPosition = new Vector3(0, 0, -0.5f);
                square.GetComponent<Square>().block.Add(block);
                square.GetComponent<Square>().type = SquareTypes.UNDESTROYABLE;
                block.GetComponent<Square>().type = SquareTypes.UNDESTROYABLE;
            }
            else if ((levelSquaresFile[row * _levelManager.maxCols + col].obstacle == SquareTypes.THRIVING &&
                      type == SquareTypes.NONE) ||
                     type == SquareTypes.THRIVING)
            {
                GameObject block = Object.Instantiate(_levelManager.thrivingBlockPrefab, cellPosition,
                    Quaternion.identity);
                block.transform.SetParent(square.transform);
                block.transform.localPosition = new Vector3(0, 0, -0.5f);
                block.GetComponent<SpriteRenderer>().sortingOrder = 3;
                if (square.GetComponent<Square>().item != null)
                    Object.Destroy(square.GetComponent<Square>().item.gameObject);
                square.GetComponent<Square>().block.Add(block);
                square.GetComponent<Square>().type = SquareTypes.THRIVING;
                block.GetComponent<Square>().type = SquareTypes.THRIVING;
            }
        }
    }
}

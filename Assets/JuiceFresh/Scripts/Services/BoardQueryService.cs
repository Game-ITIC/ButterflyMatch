using System.Collections.Generic;
using JuiceFresh;
using UnityEngine;

namespace JuiceFresh.Scripts
{
    public class BoardQueryService
    {
        private readonly LevelManager _levelManager;

        public BoardQueryService(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }

        public Square GetSquare(int col, int row, bool safe = false)
        {
            if (!safe)
            {
                if (row >= _levelManager.maxRows || col >= _levelManager.maxCols || row < 0 || col < 0)
                    return null;
                return _levelManager.squaresArray[row * _levelManager.maxCols + col];
            }

            row = Mathf.Clamp(row, 0, _levelManager.maxRows - 1);
            col = Mathf.Clamp(col, 0, _levelManager.maxCols - 1);
            return _levelManager.squaresArray[row * _levelManager.maxCols + col];
        }

        public List<Item> GetRow(int row)
        {
            List<Item> itemsList = new List<Item>();

            for (int col = 0; col < _levelManager.maxCols; col++)
                itemsList.Add(GetSquare(col, row, true).item);

            return itemsList;
        }

        public List<Square> GetRowSquare(int row)
        {
            List<Square> itemsList = new List<Square>();

            for (int col = 0; col < _levelManager.maxCols; col++)
                itemsList.Add(GetSquare(col, row, true));

            return itemsList;
        }

        public List<Item> GetColumn(int col)
        {
            List<Item> itemsList = new List<Item>();

            for (int row = 0; row < _levelManager.maxRows; row++)
                itemsList.Add(GetSquare(col, row, true).item);

            return itemsList;
        }

        public List<Square> GetColumnSquare(int col)
        {
            List<Square> itemsList = new List<Square>();

            for (int row = 0; row < _levelManager.maxRows; row++)
                itemsList.Add(GetSquare(col, row, true));

            return itemsList;
        }

        public List<Item> GetRandomItems(int count)
        {
            List<Item> list = new List<Item>();
            List<Item> list2 = new List<Item>();
            if (count <= 0)
                return list2;

            var items = Item.Active;
            if (items.Count < count)
                count = items.Count;

            for (int i = 0; i < items.Count; i++)
            {
                Item itemComponent = items[i];
                if (!itemComponent.destroying &&
                    itemComponent.currentType == ItemsTypes.NONE &&
                    itemComponent.nextType == ItemsTypes.NONE &&
                    itemComponent.square.type != SquareTypes.WIREBLOCK)
                {
                    list.Add(itemComponent);
                }
            }

            if (list.Count == 0)
                return list2;

            count = Mathf.Min(count, list.Count);

            while (list2.Count < count)
            {
                Item newItem = list[Random.Range(0, list.Count)];

                if (list2.IndexOf(newItem) < 0)
                    list2.Add(newItem);
            }

            return list2;
        }

        public List<Item> GetAllExtraItems()
        {
            List<Item> list = new List<Item>();
            var items = Item.Active;

            for (int i = 0; i < items.Count; i++)
            {
                Item item = items[i];
                if (item.currentType != ItemsTypes.NONE)
                    list.Add(item);
            }

            return list;
        }

        public List<Item> GetItemsAround(Square square)
        {
            int col = square.col;
            int row = square.row;
            List<Item> itemsList = new List<Item>();

            for (int r = row - 1; r <= row + 1; r++)
            {
                for (int c = col - 1; c <= col + 1; c++)
                    itemsList.Add(GetSquare(c, r, true).item);
            }

            return itemsList;
        }

        public List<Square> GetSquaresAround(Square square)
        {
            int col = square.col;
            int row = square.row;
            List<Square> itemsList = new List<Square>();

            for (int r = row - 1; r <= row + 1; r++)
            {
                for (int c = col - 1; c <= col + 1; c++)
                    itemsList.Add(GetSquare(c, r, true));
            }

            return itemsList;
        }

        public List<Item> GetItems()
        {
            List<Item> itemsList = new List<Item>();

            for (int row = 0; row < _levelManager.maxRows; row++)
            {
                for (int col = 0; col < _levelManager.maxCols; col++)
                {
                    Square square = GetSquare(col, row);
                    if (square != null && square.item != null)
                        itemsList.Add(GetSquare(col, row, true).item);
                }
            }

            return itemsList;
        }

        public List<Square> GetSquares()
        {
            List<Square> itemsList = new List<Square>();

            for (int row = 0; row < _levelManager.maxRows; row++)
            {
                for (int col = 0; col < _levelManager.maxCols; col++)
                {
                    Square square = GetSquare(col, row);
                    if (square != null)
                        itemsList.Add(square);
                }
            }

            return itemsList;
        }

        public List<Square> GetBottomRow()
        {
            List<Square> itemsList = new List<Square>();

            for (int col = 0; col < _levelManager.maxCols; col++)
            {
                for (int row = _levelManager.maxRows - 1; row >= 0; row--)
                {
                    Square square = GetSquare(col, row, true);

                    if (square.type != SquareTypes.NONE)
                    {
                        itemsList.Add(square);
                        break;
                    }
                }
            }

            return itemsList;
        }

        public List<Item> GetIngredients(int i = -1)
        {
            List<Item> list = new List<Item>();
            var items = Item.Active;

            for (int n = 0; n < items.Count; n++)
            {
                Item itemComponent = items[n];
                if (i > -1)
                {
                    if (itemComponent.currentType == ItemsTypes.INGREDIENT &&
                        itemComponent.color == 1000 + i)
                    {
                        list.Add(itemComponent);
                    }
                }
                else if (itemComponent.currentType == ItemsTypes.INGREDIENT)
                {
                    list.Add(itemComponent);
                }
            }

            return list;
        }
    }
}

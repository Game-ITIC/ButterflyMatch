using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace JuiceFresh.Scripts
{
    /// <summary>
    /// Bulk operations on the pieces of the board: filling empty cells, clearing the board,
    /// recolouring by colour and resetting highlights / idle animations.
    /// </summary>
    public sealed class BoardItemsService
    {
        private readonly LevelManager _levelManager;

        public BoardItemsService(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }

        /// <summary>Spawns a piece into every empty cell that can hold one.</summary>
        /// <param name="falling">False: fill every empty cell at once (level start, regeneration).</param>
        public void GenerateNewItems(bool falling = true)
        {
            for (int col = 0; col < _levelManager.maxCols; col++)
            {
                for (int row = _levelManager.maxRows - 1; row >= 0; row--)
                {
                    Square square = _levelManager.GetSquare(col, row);
                    if (square == null || square.IsNone() || !square.CanGoInto() || square.item != null)
                        continue;

                    // While falling, a cell under a solid block is filled by the gravity pass, not from the top.
                    if (!falling || !square.IsHaveSolidAbove())
                        square.GenItem(falling);
                }
            }
        }

        /// <summary>Removes every piece except collectable ingredients.</summary>
        public void DestroyItems(bool withoutEffects = false)
        {
            // DestroyItem can spawn or re-enter before its first yield, so iterate a snapshot.
            List<Item> items = ListPool<Item>.Get();
            try
            {
                items.AddRange(Item.Active);
                for (int i = 0; i < items.Count; i++)
                {
                    Item item = items[i];
                    if (item == null || item.currentType == ItemsTypes.INGREDIENT)
                        continue;

                    if (withoutEffects)
                        item.SmoothDestroy();
                    else
                        item.DestroyItem();
                }
            }
            finally
            {
                ListPool<Item>.Release(items);
            }
        }

        /// <summary>Colour bomb combo: every piece of <paramref name="color"/> becomes <paramref name="nextType"/>.</summary>
        public void SetTypeByColor(int color, ItemsTypes nextType)
        {
            // ChangeType calls into piece code, so iterate a snapshot like the old tag array.
            List<Item> items = ListPool<Item>.Get();
            try
            {
                items.AddRange(Item.Active);
                for (int i = 0; i < items.Count; i++)
                {
                    Item item = items[i];
                    if (item.color != color)
                        continue;

                    if (nextType == ItemsTypes.HORIZONTAL_STRIPPED || nextType == ItemsTypes.VERTICAL_STRIPPED)
                        item.nextType = (ItemsTypes)Random.Range(1, 3);
                    else
                        item.nextType = nextType;

                    item.ChangeType();
                    if (nextType == ItemsTypes.NONE)
                        _levelManager.destroyAnyway.Add(item);
                }
            }
            finally
            {
                ListPool<Item>.Release(items);
            }
        }

        /// <summary>Turns off the booster highlight of every cell.</summary>
        public void ClearHighlight()
        {
            _levelManager.highlightedItems.Clear();

            for (int col = 0; col < _levelManager.maxCols; col++)
            {
                for (int row = 0; row < _levelManager.maxRows; row++)
                {
                    Square square = _levelManager.GetSquare(col, row);
                    if (square == null)
                        continue;

                    square.SetActiveCage(false);
                    square.HighLight(false);
                }
            }
        }

        /// <summary>Restarts the idle animation of every piece that is not being destroyed.</summary>
        public void ResumeAmbientAnimations()
        {
            foreach (Item item in _levelManager.GetItems())
            {
                if (item != null && !item.destroying)
                    item.SleepItem();
            }
        }
    }
}

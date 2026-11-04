using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JuiceFresh.Board
{
    /// <summary>
    /// Explosions of special pieces: striped row/column, package (3x3) and colour bomb.
    /// </summary>
    public class ItemSpecialDestroyer
    {
        private readonly Item _item;

        public ItemSpecialDestroyer(Item item)
        {
            _item = item;
        }

        public IEnumerator DestroyStriped(bool boost, bool horizontal)
        {
            _item.stripeFired = true;
            SoundBase.Instance.PlayLimitSound(SoundBase.Instance.strippedExplosion);
            LevelManager.THIS.StrippedShow(_item.gameObject, horizontal);

            List<Square> line = horizontal
                ? LevelManager.THIS.GetRowSquare(_item.square.row)
                : LevelManager.THIS.GetColumnSquare(_item.square.col);

            foreach (Square sq in line)
            {
                if (sq == null)
                    continue;

                Item other = sq.item;
                if (other != _item && other != null)
                {
                    if (other.currentType != ItemsTypes.CHOCOBOMB &&
                        other.currentType != ItemsTypes.INGREDIENT &&
                        sq.square.CheckDamage(5) &&
                        !other.extraChecked)
                    {
                        // A striped piece in the line fires both lines right now (it used to fire across now
                        // and its own line only after its destroy animation, when the board had refilled).
                        if (other.currentType == ItemsTypes.HORIZONTAL_STRIPPED ||
                            other.currentType == ItemsTypes.VERTICAL_STRIPPED)
                        {
                            other.extraChecked = true;
                            other.DestroyVertical();
                            other.DestroyHorizontal();
                        }
                        else
                        {
                            other.DestroyItem(true, "", false, true);
                        }
                    }
                }
                else if (other == null &&
                         (sq.square.type == SquareTypes.BLOCK ||
                          sq.square.type == SquareTypes.SOLIDBLOCK ||
                          sq.square.type == SquareTypes.THRIVING))
                {
                    sq.square.DestroyBlock();
                }
            }

            yield return new WaitForFixedUpdate();

            if (!boost)
                _item.DestroyItem(true, "", false, true);
        }

        public void DestroyPackage()
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.destroyPackage);

            List<Item> around = LevelManager.THIS.GetItemsAround(_item.square);
            foreach (Item other in around)
            {
                if (other != null && other.currentType != ItemsTypes.CHOCOBOMB && other.currentType != ItemsTypes.INGREDIENT)
                    other.DestroyItem(true, "destroy_package");
            }

            SoundBase.Instance.PlaySound(SoundBase.Instance.explosion);
            _item.currentType = ItemsTypes.NONE;
            _item.DestroyItem(true);
        }

        public void DestroyColor(int color)
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.colorBombExpl);

            GameObject[] pieces = GameObject.FindGameObjectsWithTag("Item");
            foreach (GameObject go in pieces)
            {
                Item other = go.GetComponent<Item>();
                if (other.color == color)
                    other.DestroyItem(true, "", true);
            }
        }

        /// <summary>Colour bomb combined with another piece.</summary>
        public void CheckChocoBomb(Item item1, Item item2)
        {
            if (item1.currentType == ItemsTypes.INGREDIENT || item2.currentType == ItemsTypes.INGREDIENT)
                return;

            if (item1.currentType != ItemsTypes.CHOCOBOMB)
                return;

            if (item2.currentType == ItemsTypes.NONE)
                DestroyColor(item2.color);
            else if (item2.currentType == ItemsTypes.HORIZONTAL_STRIPPED || item2.currentType == ItemsTypes.VERTICAL_STRIPPED)
                LevelManager.THIS.SetTypeByColor(item2.color, ItemsTypes.HORIZONTAL_STRIPPED);
            else if (item2.currentType == ItemsTypes.PACKAGE)
                LevelManager.THIS.SetTypeByColor(item2.color, ItemsTypes.PACKAGE);
            else if (item2.currentType == ItemsTypes.CHOCOBOMB)
                LevelManager.THIS.DestroyDoubleBomb(_item.square.col);

            item1.DestroyItem();
        }
    }
}

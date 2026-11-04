using UnityEngine;

namespace JuiceFresh.Board
{
    /// <summary>
    /// Picks the sprite of a piece from its colour, type and state (neutral / selected).
    /// </summary>
    public class ItemSpriteView
    {
        private readonly Item _item;

        public ItemSpriteView(Item item)
        {
            _item = item;
        }

        private SpriteRenderer Renderer => _item.sprRenderer;

        public void SetColorSprite(int color)
        {
            Renderer.sprite = _item.items[color];
        }

        /// <summary>Sprite of a special type the piece is about to become (<see cref="Item.nextType"/>).</summary>
        public void ApplyPendingTypeSprite()
        {
            ItemsTypes next = _item.nextType;
            int color = _item.color;
            if (next == ItemsTypes.HORIZONTAL_STRIPPED)
                Renderer.sprite = _item.stripedItems[color].horizontal;
            else if (next == ItemsTypes.VERTICAL_STRIPPED)
                Renderer.sprite = _item.stripedItems[color].vertical;
            else if (next == ItemsTypes.PACKAGE)
                Renderer.sprite = _item.packageItems[color];
            else if (next == ItemsTypes.CHOCOBOMB)
                Renderer.sprite = _item.ChocoBombItems[0];
        }

        /// <summary>Sprite set when a type change completes; striped pieces keep the stripe effect instead.</summary>
        public void ApplyChangedTypeSprite(ItemsTypes type)
        {
            if (type == ItemsTypes.PACKAGE)
                Renderer.sprite = _item.packageItems[_item.color];
            else if (type == ItemsTypes.CHOCOBOMB)
                Renderer.sprite = _item.ChocoBombItems[0];
            else if (type == ItemsTypes.BOMB)
                Renderer.sprite = _item.bombItems[_item.color];
        }

        public void SetIngredientSprite(int ingredientIndex)
        {
            Renderer.sprite = LevelManager.THIS.ingrTarget[ingredientIndex].sprite;
        }

        public void SetAwake()
        {
            SetStateSprite(_item.itemsAnimation);
        }

        public void SetNeutral()
        {
            SetStateSprite(_item.items);
        }

        private void SetStateSprite(Sprite[] plainSprites)
        {
            switch (_item.currentType)
            {
                case ItemsTypes.INGREDIENT:
                    SetIngredientSpriteSafe();
                    break;
                case ItemsTypes.PACKAGE:
                    SetSpriteIfAvailable(_item.packageItems, _item.color);
                    break;
                case ItemsTypes.CHOCOBOMB:
                    SetSpriteIfAvailable(_item.ChocoBombItems, 0);
                    break;
                default:
                    SetSpriteIfAvailable(plainSprites, _item.color);
                    break;
            }
        }

        private void SetIngredientSpriteSafe()
        {
            int targetIndex = _item.color - 1000;
            if (LevelManager.THIS == null || targetIndex < 0 || targetIndex >= LevelManager.THIS.ingrTarget.Count)
                return;

            Renderer.sprite = LevelManager.THIS.ingrTarget[targetIndex].sprite;
        }

        private void SetSpriteIfAvailable(Sprite[] sprites, int spriteIndex)
        {
            if (Renderer == null || sprites == null || spriteIndex < 0 || spriteIndex >= sprites.Length)
                return;

            Renderer.sprite = sprites[spriteIndex];
        }
    }
}

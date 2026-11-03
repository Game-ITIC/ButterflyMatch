using System.Collections.Generic;
using JuiceFresh;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceFresh.Scripts
{
    public class CollectableTargetUIBuilder
    {
        private readonly LevelManager _levelManager;

        public CollectableTargetUIBuilder(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }

        public void Build(GameObject parentTransform, Target tar, bool forDialog = true)
        {
            tar = _levelManager.target;
            GameObject ingrPrefab = Resources.Load("Prefabs/CollectGUIObj") as GameObject;

            parentTransform.SetActive(true);
            RectTransform containerRect = parentTransform.GetComponent<RectTransform>();
            int spritesLength = (Resources.Load("Prefabs/Item") as GameObject).GetComponent<Item>().items.Length;
            Sprite[] spr = new Sprite[spritesLength];

            for (int i = 0; i < spritesLength; i++)
                spr[i] = (Resources.Load("Prefabs/Item") as GameObject).GetComponent<Item>().items[i];

            int num = _levelManager.NumIngredients;
            List<object> collectionItems = new List<object>();

            if (tar == Target.ITEMS)
            {
                for (int i = 0; i < num; i++)
                    collectionItems.Add(_levelManager.collectItems[i]);

                Sprite[] sprOld = spr;
                int ii = 0;

                for (int i = 0; i < _levelManager.collectItems.Length; i++)
                {
                    if (_levelManager.collectItems[i] != CollectItems.None)
                    {
                        spr[ii] = sprOld[(int)_levelManager.collectItems[i] - 1];
                        ii++;
                    }
                }
            }
            else if (tar == Target.COLLECT)
            {
                spr = _levelManager.ingrediendSprites;
                for (int i = 0; i < num; i++)
                    collectionItems.Add(_levelManager.ingrTarget[i]);
            }
            else if (tar == Target.BLOCKS)
            {
                num = 1;
                spr = new Sprite[]
                {
                    _levelManager.blockPrefab.GetComponent<SpriteRenderer>().sprite
                };
                for (int i = 0; i < num; i++)
                    collectionItems.Add(Ingredients.Ingredient1);
                _levelManager.ingrTarget.Add(new CollectedIngredients());
                _levelManager.ingrTarget[0].count = _levelManager.TargetBlocks;
            }
            else if (tar == Target.CAGES)
            {
                num = 1;
                spr = new Sprite[]
                {
                    _levelManager.wireBlockPrefab.GetComponent<SpriteRenderer>().sprite
                };
                for (int i = 0; i < num; i++)
                    collectionItems.Add(Ingredients.Ingredient1);
                _levelManager.ingrTarget.Add(new CollectedIngredients());
                _levelManager.ingrTarget[0].count = _levelManager.TargetCages;
            }
            else if (tar == Target.BOMBS)
            {
                num = 1;
                spr = new Sprite[]
                {
                    ingrPrefab.GetComponent<TargetGUI>().bomb
                };
                for (int i = 0; i < num; i++)
                    collectionItems.Add(Ingredients.Ingredient1);
                _levelManager.ingrTarget.Add(new CollectedIngredients());
                _levelManager.ingrTarget[0].count = 1;
            }
            else if (tar == Target.SCORE)
            {
                num = 1;
                spr = new Sprite[]
                {
                    ingrPrefab.GetComponent<TargetGUI>().star
                };
                for (int i = 0; i < num; i++)
                    collectionItems.Add(Ingredients.Ingredient1);
                _levelManager.ingrTarget.Add(new CollectedIngredients());
                _levelManager.ingrTarget[0].count = 1;
            }

            int f = 0;

            for (int i = 0; i < num; i++)
            {
                if (collectionItems[i] != (object)0 && _levelManager.ingrTarget[i].count > 0)
                    f++;
            }

            float offset = forDialog ? 200 : 100;

            containerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                (f - 1) * offset +
                ingrPrefab.transform.GetComponent<RectTransform>().rect.width / 2 * f -
                ingrPrefab.transform.GetComponent<RectTransform>().rect.width / 2 * (f - 2));

            int j = 0;

            for (int i = 0; i < num; i++)
            {
                if (collectionItems[i] != (object)0 && _levelManager.ingrTarget[i].count > 0)
                {
                    GameObject ingr = Object.Instantiate(ingrPrefab);
                    ingr.name = "Ingr" + i;
                    ingr.GetComponent<TargetGUI>().SetBack(forDialog);
                    _levelManager.listIngredientsGUIObjects.Add(ingr);
                    if (tar != Target.COLLECT)
                        ingr.transform.Find("Image").GetComponent<Image>().sprite = spr[j];
                    ingr.transform.Find("CountIngr").GetComponent<Counter_>().ingrTrackNumber = i;
                    ingr.transform.Find("CountIngr").GetComponent<Counter_>().totalCount =
                        _levelManager.ingrTarget[i].count;
                    ingr.transform.Find("CountIngrForMenu").GetComponent<Counter_>().totalCount =
                        _levelManager.ingrTarget[i].count;
                    if (tar == Target.SCORE)
                        ingr.transform.Find("CountIngrForMenu").GetComponent<Counter_>().totalCount =
                            (int)_levelManager.starsTargetCount;
                    else if (tar == Target.BLOCKS)
                        ingr.transform.Find("CountIngr").name = "TargetBlocks";
                    else if (tar == Target.CAGES)
                        ingr.transform.Find("CountIngr").name = "TargetCages";
                    else if (tar == Target.BOMBS)
                        ingr.transform.Find("CountIngr").name = "TargetBombs";

                    if (tar == Target.COLLECT)
                        ingr.GetComponent<TargetGUI>().SetSprite(_levelManager.ingrTarget[i].sprite);

                    ingr.transform.SetParent(parentTransform.transform, false);
                    ingr.transform.localScale = Vector3.one;

                    // 3D: a leftover z from the world-space instantiate pushed the icons behind the camera.
                    ingr.transform.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(
                        j * offset -
                        containerRect.rect.width / 2 +
                        ingr.transform.GetComponent<RectTransform>().rect.width / 2,
                        0,
                        0);
                    j++;
                }
            }
        }
    }
}

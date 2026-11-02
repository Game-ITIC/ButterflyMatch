using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using Services;
using TMPro;
using Unity.VisualScripting;

public enum BoostType
{
    Shovel,
    Energy,
    Bomb,
    ExtraMoves
}

public class BoostShop : MonoBehaviour
{
    public Sprite[] icons;
    public string[] descriptions;
    public int[] prices;
    public Image icon;
    public TMP_Text description;

    BoostType boostType;

    public List<BoostProduct> boostProducts = new List<BoostProduct>();

    public void SetBoost(BoostType boostType)
    {
        this.boostType = boostType;
        gameObject.SetActive(true);
        //icon.sprite = boostProducts[(int)_boostType].icon;
        //description.text = boostProducts[(int)_boostType].description;
        //for (int i = 0; i < 1; i++)
        //{
        //    transform.Find("Image/BuyBoost" + (i + 1) + "/Count").GetComponent<Text>().text = "x" + boostProducts[(int)_boostType].count[i];
        //    transform.Find("Image/BuyBoost" + (i + 1) + "/Price").GetComponent<Text>().text = "" + boostProducts[(int)_boostType].GemPrices[i];
        //}
        var boostProduct = boostProducts[(int)boostType];
        icon.sprite = boostProduct.icon;
        description.text = GameLocalization.L10n.Get("boost_desc_" + boostType.ToString().ToLowerInvariant(), boostProduct.description);
        var countTextTransform = transform.Find("Image/Buttons/BuyButton/Count");
        var priceTextTransform = transform.Find("Image/Buttons/BuyButton/Price");

        if(countTextTransform != null)
        {
            var countText = countTextTransform.GetComponent<TMP_Text>();

            if(countText != null)
            {
                countText.text = "x" + boostProduct.count[0]; // Access the first element
            }
        }

        if(priceTextTransform != null)
        {
            var priceText = priceTextTransform.GetComponent<TMP_Text>();

            if(priceText != null)
            {
                priceText.text = "" + boostProduct.GemPrices[0]; // Access the first element
            }
        }
    }

    public void BuyBoost(GameObject button)
    {
        // int count = int.Parse(button.transform.Find("Count").GetComponent<Text>().text.Replace("x", ""));
        int price = int.Parse(button.transform.Find("Image/Buttons/BuyButton/Price").GetComponent<TMP_Text>().text);

        GetComponent<AnimationManager>().BuyBoost(boostType, price, 1, boostProducts[(int)boostType].icon);
    }

    public void WatchAd()
    {
        var boostProduct = boostProducts[(int)boostType];
        icon.sprite = boostProduct.icon;

        var adRewardService = LevelManager.Instance.AdRewardService;
        adRewardService.SetInfo(boostProduct.icon, GameLocalization.L10n.Format("you_got_n", GameLocalization.L10n.Get("boost_" + boostType.ToString().ToLowerInvariant(), boostType.ToString())));
        adRewardService.TryShowRewarded(AdRewardType.Booster, boostType);
    }
}

[System.Serializable]
public class BoostProduct
{
    public Sprite icon;
    public string description;
    public int[] count;
    public int[] GemPrices;
}

using System.Collections.Generic;
using Gley.EasyIAP;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Views;

namespace Configs
{
    public enum ProductColumnWidth
    {
        Small,
        Medium,
        Large
    }

    public static class ProductColumnWidthExtensions
    {
        public static int GetUnits(this ProductColumnWidth width)
        {
            switch (width)
            {
                case ProductColumnWidth.Medium: return 2;
                case ProductColumnWidth.Large: return 3;
                case ProductColumnWidth.Small:
                default: return 1;
            }
        }
    }

    /// <summary>Where a product is shown in the shop.</summary>
    public enum ShopSection
    {
        Gems = 0,
        Featured = 1,
        NoAds = 2,
        Bundles = 3,
        Daily = 4,
        Hidden = 5
    }

    public enum ShopBadge
    {
        None = 0,
        Popular = 1,
        BestValue = 2,
        OneTime = 3,
        Sale = 4
    }

    [CreateAssetMenu(fileName = nameof(InAppConfig), menuName = nameof(Configs) + "/" + nameof(InAppConfig))]
    public class InAppConfig : ScriptableObject
    {
        [FormerlySerializedAs("<ProductViewPrefab>k__BackingField")]
        [SerializeField] public InAppProductView ProductViewPrefab;
        [SerializeField] public InAppProductView ProductViewMediumPrefab;
        [SerializeField] public InAppProductView ProductViewLargePrefab;
        [SerializeField] public Transform HLGParentPrefab;
        [SerializeField] public bool SmartFilling;

        [TableList]
        [FormerlySerializedAs("<InAppProducts>k__BackingField")]
        public List<InAppProduct> InAppProducts;

        [Header("Reward icons")]
        public Sprite GemIcon;
        public Sprite BombIcon;
        public Sprite ShovelIcon;
        public Sprite ExtraMovesIcon;
        public Sprite LivesIcon;
        public Sprite UnlimitedLivesIcon;

        [Header("Boosters for gems (Docs/GDD.md 7)")]
        public List<BoosterOffer> BoosterOffers = new();

        public InAppProductView GetProductViewPrefab(ProductColumnWidth columnWidth)
        {
            switch (columnWidth)
            {
                case ProductColumnWidth.Medium:
                    return ProductViewMediumPrefab != null ? ProductViewMediumPrefab : ProductViewPrefab;
                case ProductColumnWidth.Large:
                    return ProductViewLargePrefab != null ? ProductViewLargePrefab : ProductViewPrefab;
                case ProductColumnWidth.Small:
                default:
                    return ProductViewPrefab;
            }
        }
    }

    /// <summary>A booster sold in the menu shop for gems (not a store product).</summary>
    [System.Serializable]
    public struct BoosterOffer
    {
        public BoostType type;
        public Sprite icon;
        public int gemPrice;
        public int count;
    }

    [System.Serializable]
    public struct InAppProduct
    {
        public ShopProductNames product;
        public string productName;
        public ProductColumnWidth columnWidth;
        public Sprite icon;
        public string priceLabel;
        public int gems;
        public int bombs;
        public int butterflies;
        public int extraMoves;
        public int lives;
        public int unlimitedLivesMinutes;
        public bool oneTimePurchase;
        public ShopSection section;
        public ShopBadge badge;
        [Tooltip("Shown as a '+N%' bonus tag on gem packs. 0 = hidden.")]
        public int bonusPercent;
    }
}

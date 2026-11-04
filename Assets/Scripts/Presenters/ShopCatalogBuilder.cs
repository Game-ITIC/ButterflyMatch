using System;
using Configs;
using Data;
using GameLocalization;
using Gley.EasyIAP;
using Presenters.Shop;
using UnityEngine;
using Views;

namespace Presenters
{
    /// <summary>
    /// Builds the in-app shop from InAppConfig: the gem booster cards first, then the real-money products
    /// in the original card layout. Rebuild after a purchase so one-time / daily / no-ads cards disappear.
    /// What is shown lives in ShopProductSelection, the cards in ShopBoosterCards / ShopProductCards and the
    /// container in ShopLayout (all in Presenters.Shop).
    /// </summary>
    public class ShopCatalogBuilder : IDisposable
    {
        private readonly InAppConfig _config;
        private readonly Transform _parent;
        private readonly ShopLayout _layout;
        private readonly ShopProductSelection _selection;
        private readonly ShopProductCards _products;
        private readonly ShopBoosterCards _boosters;
        private bool _subscribedToLocale;

        public ShopCatalogBuilder(
            InAppConfig config,
            Transform parent,
            InternetState internetState,
            Action<ShopProductNames, InAppProductView> onBuy,
            Func<bool> hasNoLives = null,
            Action<BoosterOffer> onBuyBooster = null)
        {
            _config = config;
            _parent = parent;
            _layout = new ShopLayout(config, parent);
            _selection = new ShopProductSelection(config, internetState, hasNoLives);
            _products = new ShopProductCards(config, _layout, onBuy);
            _boosters = new ShopBoosterCards(config, _layout, onBuyBooster);
        }

        public void Build()
        {
            if(_config == null || _parent == null || _config.InAppProducts == null)
            {
                return;
            }

            if(!_subscribedToLocale)
            {
                L10n.Changed += Build;
                _subscribedToLocale = true;
            }

            _layout.Clear();
            var visible = _selection.Collect();

            // Boosters (for gems) first, real-money products below.
            if(_boosters.HasBoosters())
            {
                _layout.AddSectionTitle(L10n.Get("shop_section_boosters", "Boosters"));
                _boosters.FillRows();
            }

            if(visible.Count > 0)
            {
                _layout.AddSectionTitle(L10n.Get("shop_section_store", "Gems & Bundles"));
                _products.FillRows(visible);
            }

            _layout.RebuildLayout();
        }

        /// <summary>Remembers a daily deal purchase so the deal hides until tomorrow (UTC).</summary>
        public void MarkPurchased(ShopProductNames product)
        {
            _selection.MarkPurchased(product);
        }

        public void Dispose()
        {
            if(_subscribedToLocale)
            {
                L10n.Changed -= Build;
                _subscribedToLocale = false;
            }
        }

        /// <summary>Price label of a product for UI outside the shop (e.g. the lose-screen offer).</summary>
        public static string PriceOf(InAppConfig config, ShopProductNames productId)
        {
            if(config != null && config.InAppProducts != null)
            {
                foreach (var product in config.InAppProducts)
                {
                    if(product.product == productId)
                    {
                        return ShopProductCards.ResolvePrice(product);
                    }
                }
            }

            return string.Empty;
        }
    }
}

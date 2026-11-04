using System;
using System.Collections.Generic;
using System.Globalization;
using Configs;
using Data;
using Gley.EasyIAP;
using Services.InApp;
using UnityEngine;

namespace Presenters.Shop
{
    /// <summary>
    /// Which real-money products the shop shows and in what order, plus the daily-deal / context-offer
    /// "bought today" bookkeeping that hides them again.
    /// </summary>
    internal sealed class ShopProductSelection
    {
        private const string DailyBoughtKeyPrefix = "shop-daily-bought-";

        private readonly InAppConfig _config;
        private readonly InternetState _internetState;
        private readonly Func<bool> _hasNoLives;
        private readonly List<InAppProduct> _visible = new();

        public ShopProductSelection(InAppConfig config, InternetState internetState, Func<bool> hasNoLives)
        {
            _config = config;
            _internetState = internetState;
            _hasNoLives = hasNoLives;
        }

        /// <summary>Visible products in display order: featured, no ads, deal of the day, gems, bundles.</summary>
        public IReadOnlyList<InAppProduct> Collect()
        {
            _visible.Clear();

            // Contextual offers (GDD 11.3) go first while their trigger is active.
            AddContextOffer(ShopProductNames.HardLevelOffer, ContextOffers.IsHardLevelAvailable());
            AddContextOffer(ShopProductNames.OutOfLivesOffer,
                _hasNoLives != null && _hasNoLives() && ContextOffers.IsAvailableToday(ShopProductNames.OutOfLivesOffer));

            AddSection(ShopSection.Featured);

            if(_internetState == null || !_internetState.HasRemoveAds)
            {
                AddSection(ShopSection.NoAds);
            }

            var deal = PickDailyDeal();
            if(deal.HasValue)
            {
                _visible.Add(deal.Value);
            }

            var gemsStart = _visible.Count;
            AddSection(ShopSection.Gems);
            _visible.Sort(gemsStart, _visible.Count - gemsStart, Comparer<InAppProduct>.Create((a, b) => a.gems.CompareTo(b.gems)));

            AddSection(ShopSection.Bundles);
            return _visible;
        }

        /// <summary>Remembers a daily deal purchase so the deal hides until tomorrow (UTC).</summary>
        public void MarkPurchased(ShopProductNames product)
        {
            switch (product)
            {
                case ShopProductNames.OutOfLivesOffer:
                case ShopProductNames.NeedMovesOffer:
                    ContextOffers.MarkUsedToday(product);
                    return;
                case ShopProductNames.HardLevelOffer:
                    ContextOffers.MarkHardLevelUsed();
                    return;
            }

            foreach (var config in _config.InAppProducts)
            {
                if(config.product == product && config.section == ShopSection.Daily)
                {
                    PlayerPrefs.SetInt(DailyBoughtKey(), 1);
                    PlayerPrefs.Save();
                    return;
                }
            }
        }

        private void AddSection(ShopSection section)
        {
            foreach (var product in _config.InAppProducts)
            {
                if(product.section == section && !InAppGrantService.IsOneTimeProductBought(product) && IsTimedOfferOpen(product))
                {
                    _visible.Add(product);
                }
            }
        }

        /// <summary>The starter bundle shows only after level 5 and for 48 hours (GDD 11.3).</summary>
        private static bool IsTimedOfferOpen(InAppProduct product)
        {
            return product.product != ShopProductNames.StarterBundle || ContextOffers.IsStarterAvailable();
        }

        private void AddContextOffer(ShopProductNames productId, bool active)
        {
            if(!active)
            {
                return;
            }

            foreach (var product in _config.InAppProducts)
            {
                if(product.product == productId)
                {
                    _visible.Add(product);
                    return;
                }
            }
        }

        private InAppProduct? PickDailyDeal()
        {
            if(PlayerPrefs.GetInt(DailyBoughtKey(), 0) == 1)
            {
                return null;
            }

            var daily = new List<InAppProduct>();
            foreach (var product in _config.InAppProducts)
            {
                if(product.section == ShopSection.Daily)
                {
                    daily.Add(product);
                }
            }

            if(daily.Count == 0)
            {
                return null;
            }

            // One deal per UTC day, rotating through the list.
            var dayIndex = (int)(DateTime.UtcNow.Date - new DateTime(2026, 1, 1)).TotalDays;
            return daily[((dayIndex % daily.Count) + daily.Count) % daily.Count];
        }

        private static string DailyBoughtKey()
        {
            return DailyBoughtKeyPrefix + DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}

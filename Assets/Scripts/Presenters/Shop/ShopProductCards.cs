using System;
using System.Collections.Generic;
using Configs;
using GameLocalization;
using Gley.EasyIAP;
using UnityEngine;
using Views;
using Object = UnityEngine.Object;

namespace Presenters.Shop
{
    /// <summary>
    /// Real-money product cards: rows of 3 units filled with Small (1) / Medium (2) / Large (3) cards,
    /// each with its store price, picture, badge and contents.
    /// </summary>
    internal sealed class ShopProductCards
    {
        private const int RowUnits = 3;

        private readonly InAppConfig _config;
        private readonly ShopLayout _layout;
        private readonly Action<ShopProductNames, InAppProductView> _onBuy;
        private readonly List<(Sprite icon, string text)> _rewards = new();

        public ShopProductCards(InAppConfig config, ShopLayout layout, Action<ShopProductNames, InAppProductView> onBuy)
        {
            _config = config;
            _layout = layout;
            _onBuy = onBuy;
        }

        /// <summary>Row packing: each row takes, in order, the next products that still fit into 3 units.</summary>
        public void FillRows(IReadOnlyList<InAppProduct> visible)
        {
            var placed = new bool[visible.Count];
            var left = visible.Count;

            while (left > 0)
            {
                var row = _layout.CreateRow();
                var used = 0;

                for (var i = 0; i < visible.Count && used < RowUnits; i++)
                {
                    if(placed[i])
                    {
                        continue;
                    }

                    var units = Mathf.Min(RowUnits, visible[i].columnWidth.GetUnits());
                    if(used + units > RowUnits)
                    {
                        continue;
                    }

                    SpawnCard(row, visible[i]);
                    placed[i] = true;
                    used += units;
                    left--;
                }
            }
        }

        public static string ResolvePrice(InAppProduct product)
        {
            // Real store price (local currency) once the store is initialized; config label is only a fallback.
            // The Editor fake store reports $0.01 for everything, so show the config label there.
#if UNITY_EDITOR
            string localized = null;
#else
            var localized = API.IsInitialized() ? API.GetLocalizedPriceString(product.product) : null;
#endif
            if(!string.IsNullOrWhiteSpace(localized) && localized != "-")
            {
                return localized;
            }

            return product.priceLabel ?? string.Empty;
        }

        private void SpawnCard(Transform row, InAppProduct product)
        {
            var prefab = _config.GetProductViewPrefab(product.columnWidth);
            if(prefab == null)
            {
                Debug.LogWarning($"[ShopCatalogBuilder] No card prefab for {product.product} ({product.columnWidth}).");
                return;
            }

            var card = Object.Instantiate(prefab, row, false);
            card.name = $"Card_{product.product}";
            card.SetTitle(GetTitle(product));
            card.SetIcon(product.icon);
            card.SetPrice(ResolvePrice(product));
            card.SetBadge(GetBadgeText(product));
            card.SetBonus(product.bonusPercent > 0
                ? string.Format(L10n.Get("shop_bonus_percent", "+{0}%"), product.bonusPercent)
                : null);

            var rewards = CollectRewards(product);
            card.SetRewards(rewards);
            card.SetAmount(rewards.Count > 0 ? rewards[0].text : string.Empty);
            card.SetDescription(product.section == ShopSection.NoAds
                ? L10n.Get("shop_no_ads_desc", "Remove banners and pop-up ads forever")
                : string.Empty);

            if(card.BuyButton != null)
            {
                card.BuyButton.onClick.RemoveAllListeners();
                var productId = product.product;
                card.BuyButton.onClick.AddListener(() => _onBuy?.Invoke(productId, card));
            }
        }

        private IReadOnlyList<(Sprite icon, string text)> CollectRewards(InAppProduct product)
        {
            _rewards.Clear();

            if(product.gems > 0)
            {
                _rewards.Add((_config.GemIcon, ShopText.FormatNumber(product.gems)));
            }

            if(product.bombs > 0)
            {
                _rewards.Add((_config.BombIcon, "x" + product.bombs));
            }

            if(product.butterflies > 0)
            {
                _rewards.Add((_config.ShovelIcon, "x" + product.butterflies));
            }

            if(product.extraMoves > 0)
            {
                _rewards.Add((_config.ExtraMovesIcon, "x" + product.extraMoves));
            }

            if(product.lives > 0)
            {
                _rewards.Add((_config.LivesIcon, "+" + product.lives));
            }

            if(product.unlimitedLivesMinutes > 0)
            {
                _rewards.Add((_config.UnlimitedLivesIcon, ShopText.FormatDuration(product.unlimitedLivesMinutes)));
            }

            return _rewards;
        }

        private static string GetTitle(InAppProduct product)
        {
            return L10n.Get("shop_product_" + ShopText.ToSnakeCase(product.product.ToString()), product.productName);
        }

        private static string GetBadgeText(InAppProduct product)
        {
            switch (product.badge)
            {
                case ShopBadge.Popular: return L10n.Get("shop_badge_popular", "Popular");
                case ShopBadge.BestValue: return L10n.Get("shop_badge_best_value", "Best Value");
                case ShopBadge.OneTime: return L10n.Get("shop_badge_one_time", "One-time offer");
                case ShopBadge.Sale: return L10n.Get("shop_badge_sale", "Sale");
                default: return null;
            }
        }
    }
}

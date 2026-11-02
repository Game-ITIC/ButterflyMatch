using System;
using System.Collections.Generic;
using System.Globalization;
using Configs;
using Data;
using GameLocalization;
using Gley.EasyIAP;
using Services.InApp;
using UnityEngine;
using UnityEngine.UI;
using Views;
using Object = UnityEngine.Object;

namespace Presenters
{
    /// <summary>
    /// Builds the in-app shop from InAppConfig using the original card layout: rows of 3 units filled with
    /// Small (1) / Medium (2) / Large (3) cards. Each card gets its real price, picture and contents.
    /// Rebuild after a purchase so one-time / daily / no-ads cards disappear.
    /// </summary>
    public class ShopCatalogBuilder : IDisposable
    {
        private const string DailyBoughtKeyPrefix = "shop-daily-bought-";
        private const int RowUnits = 3;

        private readonly InAppConfig _config;
        private readonly Transform _parent;
        private readonly InternetState _internetState;
        private readonly Action<ShopProductNames, InAppProductView> _onBuy;
        private readonly List<(Sprite icon, string text)> _rewards = new();
        private readonly List<InAppProduct> _visible = new();
        private bool _subscribedToLocale;

        public ShopCatalogBuilder(
            InAppConfig config,
            Transform parent,
            InternetState internetState,
            Action<ShopProductNames, InAppProductView> onBuy)
        {
            _config = config;
            _parent = parent;
            _internetState = internetState;
            _onBuy = onBuy;
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

            Clear();
            CollectVisible();
            FillRows();

            LayoutRebuilder.ForceRebuildLayoutImmediate(_parent as RectTransform);
        }

        /// <summary>Remembers a daily deal purchase so the deal hides until tomorrow (UTC).</summary>
        public void MarkPurchased(ShopProductNames product)
        {
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

        public void Dispose()
        {
            if(_subscribedToLocale)
            {
                L10n.Changed -= Build;
                _subscribedToLocale = false;
            }
        }

        private void Clear()
        {
            // Also removes the placeholder rows that are still baked into the scene.
            for (var i = _parent.childCount - 1; i >= 0; i--)
            {
                var child = _parent.GetChild(i).gameObject;
                child.SetActive(false);
                Object.Destroy(child);
            }
        }

        /// <summary>Visible products in display order: featured, no ads, deal of the day, gems, bundles.</summary>
        private void CollectVisible()
        {
            _visible.Clear();
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
        }

        private void AddSection(ShopSection section)
        {
            foreach (var product in _config.InAppProducts)
            {
                if(product.section == section && !InAppGrantService.IsOneTimeProductBought(product))
                {
                    _visible.Add(product);
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

        /// <summary>Row packing: each row takes, in order, the next products that still fit into 3 units.</summary>
        private void FillRows()
        {
            var placed = new bool[_visible.Count];
            var left = _visible.Count;

            while (left > 0)
            {
                var row = CreateRow();
                var used = 0;

                for (var i = 0; i < _visible.Count && used < RowUnits; i++)
                {
                    if(placed[i])
                    {
                        continue;
                    }

                    var units = Mathf.Min(RowUnits, _visible[i].columnWidth.GetUnits());
                    if(used + units > RowUnits)
                    {
                        continue;
                    }

                    SpawnCard(row, _visible[i]);
                    placed[i] = true;
                    used += units;
                    left--;
                }
            }
        }

        private Transform CreateRow()
        {
            if(_config.HLGParentPrefab != null)
            {
                return Object.Instantiate(_config.HLGParentPrefab, _parent, false);
            }

            var go = new GameObject("InAppHLG", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(_parent, false);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            ((RectTransform)go.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 280f);
            return go.transform;
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
                _rewards.Add((_config.GemIcon, FormatNumber(product.gems)));
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
                _rewards.Add((_config.UnlimitedLivesIcon, FormatDuration(product.unlimitedLivesMinutes)));
            }

            return _rewards;
        }

        private static string GetTitle(InAppProduct product)
        {
            return L10n.Get("shop_product_" + ToSnakeCase(product.product.ToString()), product.productName);
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

        private static string ResolvePrice(InAppProduct product)
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

        private static string DailyBoughtKey()
        {
            return DailyBoughtKeyPrefix + DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static string FormatNumber(int value)
        {
            // "1,000" reads wrong outside English; most other locales group with a space ("1 000").
            var format = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            if(!string.Equals(L10n.CurrentCode, "en", StringComparison.OrdinalIgnoreCase))
            {
                format.NumberGroupSeparator = " ";
            }

            return value.ToString("N0", format);
        }

        private static string FormatDuration(int minutes)
        {
            if(minutes >= 60 && minutes % 60 == 0)
            {
                return string.Format(L10n.Get("shop_duration_hours", "{0}h"), minutes / 60);
            }

            return string.Format(L10n.Get("shop_duration_minutes", "{0}m"), minutes);
        }

        private static string ToSnakeCase(string value)
        {
            var sb = new System.Text.StringBuilder(value.Length + 8);
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if(char.IsUpper(c) && i > 0)
                {
                    sb.Append('_');
                }

                sb.Append(char.ToLowerInvariant(c));
            }

            return sb.ToString();
        }
    }
}

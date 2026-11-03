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
        private const int BoostersPerRow = 2;

        private readonly InAppConfig _config;
        private readonly Transform _parent;
        private readonly InternetState _internetState;
        private readonly Action<ShopProductNames, InAppProductView> _onBuy;
        private readonly Func<bool> _hasNoLives;
        private readonly Action<BoosterOffer> _onBuyBooster;
        private readonly List<(Sprite icon, string text)> _rewards = new();
        private readonly List<InAppProduct> _visible = new();
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
            _internetState = internetState;
            _onBuy = onBuy;
            _hasNoLives = hasNoLives;
            _onBuyBooster = onBuyBooster;
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
            FillBoosterRows();

            LayoutRebuilder.ForceRebuildLayoutImmediate(_parent as RectTransform);
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

        /// <summary>Boosters bought for gems: small cards, two per row, after the store products.</summary>
        private void FillBoosterRows()
        {
            if(_onBuyBooster == null || _config.BoosterOffers == null || _config.ProductViewPrefab == null)
            {
                return;
            }

            Transform row = null;
            var inRow = 0;
            foreach (var offer in _config.BoosterOffers)
            {
                if(offer.gemPrice <= 0)
                {
                    continue;
                }

                if(row == null || inRow >= BoostersPerRow)
                {
                    row = CreateRow();
                    inRow = 0;
                }

                SpawnBoosterCard(row, offer);
                inRow++;
            }
        }

        private void SpawnBoosterCard(Transform row, BoosterOffer offer)
        {
            var card = Object.Instantiate(_config.ProductViewPrefab, row, false);
            card.name = $"Card_Booster_{offer.type}";
            card.SetTitle(L10n.Get("boost_" + offer.type.ToString().ToLowerInvariant(), offer.type.ToString()));
            card.SetIcon(offer.icon);
            card.SetBadge(null);
            card.SetBonus(null);
            card.SetDescription(string.Empty);

            _rewards.Clear();
            _rewards.Add((offer.icon, "x" + Mathf.Max(1, offer.count)));
            card.SetRewards(_rewards);
            card.SetAmount(string.Empty);
            card.SetPrice(FormatNumber(offer.gemPrice));

            if(card.BuyButton != null)
            {
                AddGemIcon(card.BuyButton.transform as RectTransform);
                card.BuyButton.onClick.RemoveAllListeners();
                card.BuyButton.onClick.AddListener(() => _onBuyBooster?.Invoke(offer));
            }
        }

        /// <summary>Gem icon left of the price so the button reads "💎 60" instead of a store price.</summary>
        private void AddGemIcon(RectTransform button)
        {
            if(button == null || _config.GemIcon == null)
            {
                return;
            }

            var icon = new GameObject("GemIcon", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)icon.transform;
            rect.SetParent(button, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            const float iconSize = 38f;
            const float gap = 6f;
            rect.sizeDelta = new Vector2(iconSize, iconSize);
            var image = icon.GetComponent<Image>();
            image.sprite = _config.GemIcon;
            image.preserveAspect = true;
            image.raycastTarget = false;

            // Center "icon + price" as one group: the text moves right by half the icon, the icon sits left of it.
            const float shift = (iconSize + gap) * 0.5f;
            var label = button.GetComponentInChildren<TMPro.TMP_Text>();
            var textWidth = 0f;
            if(label != null)
            {
                var labelRect = label.rectTransform;
                labelRect.offsetMin = new Vector2(labelRect.offsetMin.x + shift * 2f, labelRect.offsetMin.y);
                textWidth = label.GetPreferredValues(label.text).x;
            }

            rect.anchoredPosition = new Vector2(shift - textWidth * 0.5f - gap - iconSize * 0.5f, 1f);
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

        /// <summary>Price label of a product for UI outside the shop (e.g. the lose-screen offer).</summary>
        public static string PriceOf(InAppConfig config, ShopProductNames productId)
        {
            if(config != null && config.InAppProducts != null)
            {
                foreach (var product in config.InAppProducts)
                {
                    if(product.product == productId)
                    {
                        return ResolvePrice(product);
                    }
                }
            }

            return string.Empty;
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

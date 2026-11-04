using System;
using System.Collections.Generic;
using Configs;
using GameLocalization;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Presenters.Shop
{
    /// <summary>
    /// Boosters bought for gems: one card per booster type with its x1/x3/x5 packs, or the old small
    /// product cards (three per row) when the config has no booster card prefab.
    /// </summary>
    internal sealed class ShopBoosterCards
    {
        private const int BoostersPerRow = 3;

        private readonly InAppConfig _config;
        private readonly ShopLayout _layout;
        private readonly Action<BoosterOffer> _onBuyBooster;
        private readonly List<(Sprite icon, string text)> _rewards = new();
        private readonly List<BoostType> _boosterTypes = new();

        public ShopBoosterCards(InAppConfig config, ShopLayout layout, Action<BoosterOffer> onBuyBooster)
        {
            _config = config;
            _layout = layout;
            _onBuyBooster = onBuyBooster;
        }

        public bool HasBoosters()
        {
            if(_onBuyBooster == null || _config.BoosterOffers == null
               || (_config.BoosterCardPrefab == null && _config.ProductViewPrefab == null))
            {
                return false;
            }

            foreach (var offer in _config.BoosterOffers)
            {
                if(offer.gemPrice > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public void FillRows()
        {
            if(_config.BoosterCardPrefab == null)
            {
                FillRowsLegacy();
                return;
            }

            _boosterTypes.Clear();
            foreach (var offer in _config.BoosterOffers)
            {
                if(offer.gemPrice > 0 && !_boosterTypes.Contains(offer.type))
                {
                    _boosterTypes.Add(offer.type);
                }
            }

            foreach (var type in _boosterTypes)
            {
                SpawnTypeCard(type);
            }
        }

        private void SpawnTypeCard(BoostType type)
        {
            var card = Object.Instantiate(_config.BoosterCardPrefab, _layout.Parent, false);
            var key = type.ToString().ToLowerInvariant();
            card.name = $"Card_Booster_{type}";

            var pack = 0;
            Sprite icon = null;
            foreach (var offer in _config.BoosterOffers)
            {
                if(offer.type != type || offer.gemPrice <= 0)
                {
                    continue;
                }

                if(icon == null)
                {
                    icon = offer.icon;
                }

                if(pack >= card.PackCount)
                {
                    continue;
                }

                var captured = offer;
                card.SetPack(
                    pack,
                    "x" + Mathf.Max(1, offer.count),
                    ShopText.FormatNumber(offer.gemPrice),
                    offer.discountPercent > 0 ? "-" + offer.discountPercent + "%" : null,
                    () => _onBuyBooster?.Invoke(captured));
                pack++;
            }

            card.HidePacksFrom(pack);
            card.SetHeader(
                L10n.Get("boost_" + key, type.ToString()),
                L10n.Get("boost_desc_" + key, string.Empty),
                icon);
        }

        /// <summary>Old look: small product cards, three per row (one booster, x1/x3/x5 packs).</summary>
        private void FillRowsLegacy()
        {
            if(_config.ProductViewPrefab == null)
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
                    row = _layout.CreateRow();
                    inRow = 0;
                }

                SpawnLegacyCard(row, offer);
                inRow++;
            }
        }

        private void SpawnLegacyCard(Transform row, BoosterOffer offer)
        {
            var card = Object.Instantiate(_config.ProductViewPrefab, row, false);
            card.name = $"Card_Booster_{offer.type}";
            card.SetTitle(L10n.Get("boost_" + offer.type.ToString().ToLowerInvariant(), offer.type.ToString()));
            card.SetIcon(offer.icon);
            card.SetBadge(null);
            card.SetBonus(offer.discountPercent > 0 ? "-" + offer.discountPercent + "%" : null);
            card.SetDescription(string.Empty);

            _rewards.Clear();
            _rewards.Add((offer.icon, "x" + Mathf.Max(1, offer.count)));
            card.SetRewards(_rewards);
            card.SetAmount(string.Empty);
            card.SetPrice(ShopText.FormatNumber(offer.gemPrice));

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
    }
}

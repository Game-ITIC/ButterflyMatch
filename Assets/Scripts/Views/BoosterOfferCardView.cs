using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Views
{
    /// <summary>
    /// Shop card for one booster bought with gems: icon, name, what it does, and up to three packs (x1 / x3 / x5)
    /// side by side, each with its gem price and an optional discount pill. Filled by ShopCatalogBuilder.
    /// </summary>
    public class BoosterOfferCardView : MonoBehaviour
    {
        [Serializable]
        public class PackSlot
        {
            public GameObject root;
            public Button button;
            public TMP_Text count;
            public TMP_Text price;
            public GameObject discountRoot;
            public TMP_Text discount;
        }

        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _description;
        [SerializeField] private PackSlot[] _packs;

        public int PackCount => _packs != null ? _packs.Length : 0;

        public void SetHeader(string title, string description, Sprite icon)
        {
            if (_title != null) _title.text = title;

            if (_description != null)
            {
                _description.text = description;
                _description.gameObject.SetActive(!string.IsNullOrEmpty(description));
            }

            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.enabled = icon != null;
            }
        }

        public void SetPack(int index, string count, string price, string discount, UnityAction onBuy)
        {
            if (_packs == null || index < 0 || index >= _packs.Length || _packs[index] == null)
            {
                return;
            }

            var pack = _packs[index];
            if (pack.root != null) pack.root.SetActive(true);
            if (pack.count != null) pack.count.text = count;
            if (pack.price != null) pack.price.text = price;

            var hasDiscount = !string.IsNullOrEmpty(discount);
            if (pack.discountRoot != null) pack.discountRoot.SetActive(hasDiscount);
            if (hasDiscount && pack.discount != null) pack.discount.text = discount;

            if (pack.button != null)
            {
                pack.button.onClick.RemoveAllListeners();
                if (onBuy != null) pack.button.onClick.AddListener(onBuy);
            }
        }

        /// <summary>Hides the pack slots from <paramref name="firstUnused"/> on (a booster with fewer packs).</summary>
        public void HidePacksFrom(int firstUnused)
        {
            for (var i = Mathf.Max(0, firstUnused); i < PackCount; i++)
            {
                if (_packs[i] != null && _packs[i].root != null) _packs[i].root.SetActive(false);
            }
        }
    }
}

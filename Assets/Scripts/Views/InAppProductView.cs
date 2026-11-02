using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Views
{
    /// <summary>
    /// One shop card (featured / wide / gem). Every reference is optional so the same view
    /// serves all card layouts; ShopCatalogBuilder fills it from InAppConfig.
    /// </summary>
    public class InAppProductView : MonoBehaviour
    {
        [SerializeField] private Button buyButton;
        [SerializeField] private Image image;
        [SerializeField] private IconWtihTextView[] _productGives;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text price;

        [Header("Shop card layout")]
        [SerializeField] private TMP_Text _amount;
        [SerializeField] private TMP_Text _description;
        [SerializeField] private Transform _rewardsParent;
        [SerializeField] private IconWtihTextView _rewardItemPrefab;
        [SerializeField] private GameObject _badgeRoot;
        [SerializeField] private TMP_Text _badgeText;
        [SerializeField] private GameObject _bonusRoot;
        [SerializeField] private TMP_Text _bonusText;

        public Button BuyButton => buyButton;
        public IconWtihTextView[] ProductGives => _productGives;

        public void Init(string productName, Sprite icon, string price, string amount)
        {
            if (_productGives != null && _productGives.Length > 0 && _productGives[0] != null)
            {
                _productGives[0].SetText(amount);
                _productGives[0].SetImage(icon);
            }

            SetIcon(icon);
            SetPrice(price);
            SetTitle(productName);
        }

        public void SetTitle(string title) => SetText(_label, title);

        public void SetPrice(string value) => SetText(price, value);

        public void SetAmount(string value) => SetText(_amount, value);

        public void SetDescription(string value) => SetText(_description, value);

        public void SetIcon(Sprite icon)
        {
            if(image == null || icon == null)
            {
                return;
            }

            image.sprite = icon;
            image.preserveAspect = true;
        }

        public void SetBadge(string text)
        {
            var visible = !string.IsNullOrEmpty(text);
            if(_badgeRoot != null)
            {
                _badgeRoot.SetActive(visible);
            }

            SetText(_badgeText, text);
        }

        public void SetBonus(string text)
        {
            var visible = !string.IsNullOrEmpty(text);
            if(_bonusRoot != null)
            {
                _bonusRoot.SetActive(visible);
            }

            SetText(_bonusText, text);
        }

        public void SetRewards(IReadOnlyList<(Sprite icon, string text)> rewards)
        {
            if(_rewardsParent == null || _rewardItemPrefab == null)
            {
                return;
            }

            for (var i = _rewardsParent.childCount - 1; i >= 0; i--)
            {
                var child = _rewardsParent.GetChild(i);
                if(child != _rewardItemPrefab.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            _rewardItemPrefab.gameObject.SetActive(false);

            for (var i = 0; i < rewards.Count; i++)
            {
                var item = Instantiate(_rewardItemPrefab, _rewardsParent, false);
                item.gameObject.SetActive(true);
                item.SetImage(rewards[i].icon);
                item.SetText(rewards[i].text);
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if(target != null)
            {
                target.text = value ?? string.Empty;
            }
        }
    }
}

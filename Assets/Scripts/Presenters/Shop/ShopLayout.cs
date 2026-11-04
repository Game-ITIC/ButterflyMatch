using Configs;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Presenters.Shop
{
    /// <summary>The shop content container: clearing it, section titles and card rows.</summary>
    internal sealed class ShopLayout
    {
        private readonly InAppConfig _config;
        private readonly Transform _parent;

        public ShopLayout(InAppConfig config, Transform parent)
        {
            _config = config;
            _parent = parent;
        }

        public Transform Parent => _parent;

        public void Clear()
        {
            // Also removes the placeholder rows that are still baked into the scene.
            for (var i = _parent.childCount - 1; i >= 0; i--)
            {
                var child = _parent.GetChild(i).gameObject;
                child.SetActive(false);
                Object.Destroy(child);
            }
        }

        public void AddSectionTitle(string text)
        {
            if(_config.SectionTitlePrefab == null)
            {
                return;
            }

            var title = Object.Instantiate(_config.SectionTitlePrefab, _parent, false);
            title.text = text;
        }

        public Transform CreateRow()
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

        public void RebuildLayout()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_parent as RectTransform);
        }
    }
}

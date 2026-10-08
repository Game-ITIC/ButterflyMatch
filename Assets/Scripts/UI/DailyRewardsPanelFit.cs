using UnityEngine;

namespace UI
{
    /// <summary>
    /// Keeps DailyRewardsPanel at one design size in canvas units.
    /// CanvasScaler already maps that size across resolutions. This only shrinks
    /// the panel, uniformly, when the parent is smaller than the design size.
    /// </summary>
    public sealed class DailyRewardsPanelFit : MonoBehaviour
    {
        [SerializeField] private Vector2 designSize = new Vector2(920f, 984f);
        [SerializeField] private Vector2 designOffset = new Vector2(0f, 115f);

        private RectTransform _rect;
        private bool _applying;

        private void Awake()
        {
            _rect = (RectTransform)transform;
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnRectTransformDimensionsChange()
        {
            Apply();
        }

        private void Apply()
        {
            if(_applying || !isActiveAndEnabled)
            {
                return;
            }

            if(_rect == null)
            {
                _rect = transform as RectTransform;
            }

            var parent = _rect != null ? _rect.parent as RectTransform : null;
            if(parent == null || designSize.x <= 1f || designSize.y <= 1f)
            {
                return;
            }

            var parentSize = parent.rect.size;
            if(parentSize.x <= 1f || parentSize.y <= 1f)
            {
                return;
            }

            var scale = Mathf.Min(parentSize.x / designSize.x, parentSize.y / designSize.y);
            if(scale > 1f)
            {
                scale = 1f;
            }

            var size = designSize * scale;
            var half = size * 0.5f;
            var limitX = Mathf.Max(0f, parentSize.x * 0.5f - half.x);
            var limitY = Mathf.Max(0f, parentSize.y * 0.5f - half.y);
            var position = designOffset * scale;
            position.x = Mathf.Clamp(position.x, -limitX, limitX);
            position.y = Mathf.Clamp(position.y, -limitY, limitY);

            _applying = true;
            _rect.anchorMin = new Vector2(0.5f, 0.5f);
            _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.localScale = Vector3.one;
            _rect.sizeDelta = size;
            _rect.anchoredPosition = position;
            _applying = false;
        }
    }
}

using UnityEngine;

namespace UI
{
    /// <summary>
    /// Keeps a horizontally stretched bar from getting wider than <see cref="maxWidth"/> on tablets and foldables.
    /// The rect stays stretched with <see cref="sidePadding"/> on each side until the parent is wider than
    /// maxWidth + 2 * sidePadding; after that it stays maxWidth wide and centered, so the bar keeps its phone proportions.
    /// Drives only the horizontal anchors, position and width; the vertical layout stays editable.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class MaxWidthFitter : MonoBehaviour
    {
        [Tooltip("Widest the rect may get, in canvas units. Phones stay below it, so they keep the stretched look.")]
        [SerializeField] float maxWidth = 1340f;

        [Tooltip("Gap to the parent's left and right edges while the parent is narrower than the max width.")]
        [SerializeField] float sidePadding = 16f;

        DrivenRectTransformTracker _tracker;
        RectTransform _rect;
        float _lastParentWidth = -1f;

        void OnEnable()
        {
            _rect = (RectTransform)transform;
            _tracker.Clear();
            _tracker.Add(this, _rect, DrivenTransformProperties.SizeDeltaX | DrivenTransformProperties.AnchorMinX |
                                      DrivenTransformProperties.AnchorMaxX | DrivenTransformProperties.AnchoredPositionX);
            _lastParentWidth = -1f;
            Fit();
        }

        void OnDisable() => _tracker.Clear();

#if UNITY_EDITOR
        void OnValidate() => _lastParentWidth = -1f;
#endif

        // The parent (canvas / safe area) resizes with the screen and does not notify its children.
        void Update() => Fit();

        void Fit()
        {
            if (!(_rect.parent is RectTransform parent))
                return;

            float parentWidth = parent.rect.width;
            if (Mathf.Approximately(parentWidth, _lastParentWidth))
                return;

            _lastParentWidth = parentWidth;
            float width = Mathf.Min(parentWidth - 2f * sidePadding, maxWidth);

            _rect.anchorMin = new Vector2(0f, _rect.anchorMin.y);
            _rect.anchorMax = new Vector2(1f, _rect.anchorMax.y);
            _rect.anchoredPosition = new Vector2(0f, _rect.anchoredPosition.y);
            _rect.sizeDelta = new Vector2(width - parentWidth, _rect.sizeDelta.y);
        }
    }
}

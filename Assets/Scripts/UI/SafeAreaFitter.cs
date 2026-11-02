using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Keeps UI inside Screen.safeArea (notch / cutout / home indicator).
    /// Must sit on a direct child of a full-screen (Screen Space) canvas.
    /// Fit: remaps this RectTransform's anchors into the safe area.
    /// FitChildren: leaves this object full-screen (dim/background) and remaps its direct children instead.
    /// Anchors are only changed at runtime; serialized values stay untouched.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        public enum FitMode
        {
            Fit,
            FitChildren
        }

        [SerializeField] FitMode mode = FitMode.Fit;
        [SerializeField] bool applyTop = true;
        [SerializeField] bool applyBottom = true;
        [SerializeField] bool applyLeft = true;
        [SerializeField] bool applyRight = true;

        [Tooltip("Edges that originally touch the screen edge stay there, so the background bleeds under the notch/home bar " +
                 "while the opposite edge (and content aligned to it) moves into the safe area. Use for edge bars.")]
        [SerializeField] bool extendToScreenEdge;

        struct Entry
        {
            public RectTransform Rect;
            public Vector2 AnchorMin;
            public Vector2 AnchorMax;
        }

        readonly List<Entry> _entries = new List<Entry>();
        Rect _lastSafeArea;
        Vector2Int _lastScreenSize;
        bool _captured;

        void OnEnable()
        {
            Capture();
            Apply(force: true);
        }

        void Update()
        {
            Apply(force: false);
        }

        void Capture()
        {
            if (_captured)
                return;

            _captured = true;
            _entries.Clear();

            if (mode == FitMode.Fit)
            {
                AddEntry((RectTransform)transform);
                return;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i) is RectTransform child)
                    AddEntry(child);
            }
        }

        void AddEntry(RectTransform rect)
        {
            _entries.Add(new Entry { Rect = rect, AnchorMin = rect.anchorMin, AnchorMax = rect.anchorMax });
        }

#if UNITY_EDITOR
        /// <summary>Editor-only: normalized (0..1) safe rect used instead of Screen.safeArea, for testing notches in the plain Game View.</summary>
        public static Rect? EditorSafeAreaOverride;
#endif

        static Rect GetSafeArea()
        {
#if UNITY_EDITOR
            if (EditorSafeAreaOverride.HasValue)
            {
                Rect n = EditorSafeAreaOverride.Value;
                return new Rect(n.x * Screen.width, n.y * Screen.height, n.width * Screen.width, n.height * Screen.height);
            }
#endif
            return Screen.safeArea;
        }

        void Apply(bool force)
        {
            Rect safe = GetSafeArea();
            var screenSize = new Vector2Int(Screen.width, Screen.height);

            if (!force && safe == _lastSafeArea && screenSize == _lastScreenSize)
                return;

            _lastSafeArea = safe;
            _lastScreenSize = screenSize;

            if (screenSize.x <= 0 || screenSize.y <= 0)
                return;

            Vector2 safeMin = new Vector2(
                applyLeft ? safe.xMin / screenSize.x : 0f,
                applyBottom ? safe.yMin / screenSize.y : 0f);
            Vector2 safeMax = new Vector2(
                applyRight ? safe.xMax / screenSize.x : 1f,
                applyTop ? safe.yMax / screenSize.y : 1f);
            Vector2 safeSize = safeMax - safeMin;

            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                if (entry.Rect == null)
                    continue;

                Vector2 min = safeMin + Vector2.Scale(entry.AnchorMin, safeSize);
                Vector2 max = safeMin + Vector2.Scale(entry.AnchorMax, safeSize);

                if (extendToScreenEdge)
                {
                    if (entry.AnchorMin.x <= 0f) min.x = 0f;
                    if (entry.AnchorMin.y <= 0f) min.y = 0f;
                    if (entry.AnchorMax.x >= 1f) max.x = 1f;
                    if (entry.AnchorMax.y >= 1f) max.y = 1f;
                }

                entry.Rect.anchorMin = min;
                entry.Rect.anchorMax = max;
            }
        }
    }
}

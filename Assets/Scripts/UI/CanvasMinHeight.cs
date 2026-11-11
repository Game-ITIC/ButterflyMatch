using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Keeps a "Scale With Screen Size / Match Width Or Height" canvas at least <see cref="minHeight"/> units tall.
    /// Phones (16:9 and taller) already get a taller canvas and keep the authored match. On tablets and foldables the
    /// authored match would make the canvas too short for the windows, so the match moves toward width just enough
    /// to keep minHeight. Runtime only: the serialized match value is never changed.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasScaler))]
    public class CanvasMinHeight : MonoBehaviour
    {
        [Tooltip("Shortest canvas height in units. Menu windows are 1280 tall below a 320 top bar, so 2000 leaves a gap " +
                 "above the bottom nav. A 1080x1920 phone with a 1920x1080 / 0.5 scaler gets 1920, 20:9 phones ~2100.")]
        [SerializeField] float minHeight = 2000f;

        CanvasScaler _scaler;
        float _authoredMatch;
        Vector2Int _lastScreen;

        void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
            _authoredMatch = _scaler.matchWidthOrHeight;
        }

        void OnEnable()
        {
            _lastScreen = default;
            Apply();
        }

        void OnDisable()
        {
            if (_scaler != null)
                _scaler.matchWidthOrHeight = _authoredMatch;
        }

        // Screen size changes (rotation, split screen, Device Simulator) don't raise an event; comparing two ints is cheap.
        void Update() => Apply();

        void Apply()
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (screen == _lastScreen || screen.x <= 0 || screen.y <= 0)
                return;

            _lastScreen = screen;
            _scaler.matchWidthOrHeight = MatchFor(screen);
        }

        float MatchFor(Vector2Int screen)
        {
            if (_scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                _scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
                return _authoredMatch;

            // Same log-space blend CanvasScaler uses.
            Vector2 reference = _scaler.referenceResolution;
            float logWidth = Mathf.Log(screen.x / reference.x);
            float logHeight = Mathf.Log(screen.y / reference.y);
            float scale = Mathf.Exp(Mathf.Lerp(logWidth, logHeight, _authoredMatch));
            float maxScale = screen.y / minHeight;

            if (scale <= maxScale || Mathf.Approximately(logWidth, logHeight))
                return _authoredMatch;

            return Mathf.Clamp01((Mathf.Log(maxScale) - logWidth) / (logHeight - logWidth));
        }
    }
}

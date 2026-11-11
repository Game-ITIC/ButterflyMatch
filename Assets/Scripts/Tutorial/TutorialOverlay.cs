using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameTutorials
{
    /// <summary>
    /// The tutorial's screen layer (Game/Tutorial/Tutorial Overlay): the dim, the message card and the hand.
    /// It only draws; <see cref="LevelTutorialDirector"/> decides what to show.
    /// </summary>
    public sealed class TutorialOverlay : MonoBehaviour
    {
        public enum Place
        {
            Top,
            Bottom,
        }

        private enum HandMode
        {
            Hidden,
            TapUi,
            TapWorld,
            Swipe,
        }

        // Card positions on the 1536x2048 canvas: under the top bar, right above the booster bar
        // (tall boards like level 6 reach down to ~650 units, the booster icons top out at ~370).
        private const float TopY = -300f;
        private const float BottomY = 400f;

        // A tap that arrives together with the step's appearance must not skip it.
        private const float TapDelay = 0.6f;

        private const float TapPeriod = 0.9f;
        private const float TapTravel = 22f;
        private const float PressTime = 0.35f;
        private const float StepTime = 0.32f;
        private const float HoldTime = 0.45f;
        private const float GapTime = 0.35f;

        [SerializeField] private Canvas _canvas;
        [SerializeField] private Button _dim;
        [SerializeField] private RectTransform _card;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _continueLabel;
        [SerializeField] private RectTransform _hand;
        [SerializeField] private CanvasGroup _handGroup;

        private readonly List<Vector3> _path = new List<Vector3>();
        private HandMode _handMode;
        private RectTransform _uiTarget;
        private Vector3 _worldTarget;
        private float _handStart;
        private float _tapEnabledAt;
        private bool _tapToContinue;

        /// <summary>The player tapped the dim during a "tap to continue" step.</summary>
        public event Action Tapped;

        public int SortingLayerId => _canvas.sortingLayerID;

        /// <summary>The order a raised element needs to sit above the dim and under the hand.</summary>
        public int RaisedOrder => _canvas.sortingOrder + 1;

        private void Awake()
        {
            // The scene's other canvases hold Main Camera; the prefab cannot, so it is picked up here.
            if (_canvas.worldCamera == null)
                _canvas.worldCamera = Camera.main;

            _dim.onClick.AddListener(OnDimClicked);
            Hide();
        }

        private void OnDestroy()
        {
            _dim.onClick.RemoveListener(OnDimClicked);
        }

        /// <summary>Hides the layer for a moment (pause, popups) without forgetting the step.</summary>
        public void SetVisible(bool visible) => _canvas.enabled = visible;

        public void Hide()
        {
            _dim.gameObject.SetActive(false);
            _card.gameObject.SetActive(false);
            HideHand();
            _tapToContinue = false;
        }

        public void ShowDim(bool on) => _dim.gameObject.SetActive(on);

        public void ShowMessage(string text, Place place, bool tapToContinue)
        {
            _label.text = text;
            _continueLabel.text = GameLocalization.L10n.Get("tap_to_continue", "Tap to continue");
            _continueLabel.gameObject.SetActive(tapToContinue);
            _tapToContinue = tapToContinue;
            _tapEnabledAt = Time.unscaledTime + TapDelay;

            bool top = place == Place.Top;
            _card.anchorMin = _card.anchorMax = new Vector2(0.5f, top ? 1f : 0f);
            _card.pivot = new Vector2(0.5f, top ? 1f : 0f);
            _card.anchoredPosition = new Vector2(0f, top ? TopY : BottomY);
            _card.gameObject.SetActive(true);
        }

        public void TapAt(RectTransform uiTarget)
        {
            _uiTarget = uiTarget;
            StartHand(HandMode.TapUi);
        }

        public void TapAt(Vector3 worldPosition)
        {
            _worldTarget = worldPosition;
            StartHand(HandMode.TapWorld);
        }

        /// <summary>Drags the finger through the points again and again.</summary>
        public void Swipe(IEnumerable<Vector3> worldPath)
        {
            _path.Clear();
            _path.AddRange(worldPath);
            StartHand(_path.Count > 1 ? HandMode.Swipe : HandMode.Hidden);
        }

        public void HideHand()
        {
            _handMode = HandMode.Hidden;
            _uiTarget = null;
            _hand.gameObject.SetActive(false);
        }

        private void StartHand(HandMode mode)
        {
            _handMode = mode;
            _handStart = Time.unscaledTime;
            _hand.gameObject.SetActive(mode != HandMode.Hidden);
            LateUpdate();
        }

        private void OnDimClicked()
        {
            if (_tapToContinue && Time.unscaledTime >= _tapEnabledAt)
                Tapped?.Invoke();
        }

        private void LateUpdate()
        {
            if (_handMode == HandMode.Hidden)
                return;

            float t = Time.unscaledTime - _handStart;
            switch (_handMode)
            {
                case HandMode.TapUi:
                    if (_uiTarget == null)
                    {
                        HideHand();
                        return;
                    }

                    PlaceTap(UiToLocal(_uiTarget), t);
                    break;
                case HandMode.TapWorld:
                    PlaceTap(WorldToLocal(_worldTarget), t);
                    break;
                case HandMode.Swipe:
                    PlaceSwipe(t);
                    break;
            }
        }

        // The fingertip bobs onto the target.
        private void PlaceTap(Vector2 target, float t)
        {
            float press = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f / TapPeriod);
            _hand.anchoredPosition = target + new Vector2(1f, -1f) * (TapTravel * press);
            _hand.localScale = Vector3.one * (1f - 0.08f * (1f - press));
            _handGroup.alpha = 1f;
        }

        // Press on the first piece, drag through the rest, hold, vanish, repeat.
        private void PlaceSwipe(float t)
        {
            float moveTime = StepTime * (_path.Count - 1);
            float cycle = PressTime + moveTime + HoldTime + GapTime;
            float c = t % cycle;

            Vector3 world;
            float alpha = 1f;
            float scale = 0.92f;

            if (c < PressTime)
            {
                float k = c / PressTime;
                world = _path[0];
                alpha = k;
                scale = Mathf.Lerp(1.1f, 0.92f, k);
            }
            else if (c < PressTime + moveTime)
            {
                float s = (c - PressTime) / StepTime;
                int i = Mathf.Min((int)s, _path.Count - 2);
                world = Vector3.Lerp(_path[i], _path[i + 1], Mathf.SmoothStep(0f, 1f, s - i));
            }
            else if (c < PressTime + moveTime + HoldTime)
            {
                world = _path[_path.Count - 1];
            }
            else
            {
                world = _path[_path.Count - 1];
                alpha = 0f;
            }

            _hand.anchoredPosition = WorldToLocal(world);
            _hand.localScale = Vector3.one * scale;
            _handGroup.alpha = alpha;
        }

        private Vector2 WorldToLocal(Vector3 world)
        {
            Camera boardCamera = Camera.main;
            Vector2 screen = boardCamera != null ? (Vector2)boardCamera.WorldToScreenPoint(world) : Vector2.zero;
            return ScreenToLocal(screen);
        }

        private Vector2 UiToLocal(RectTransform target)
        {
            Canvas targetCanvas = target.GetComponentInParent<Canvas>();
            Camera uiCamera = targetCanvas != null && targetCanvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.rootCanvas.worldCamera
                : null;
            Vector3 center = target.TransformPoint(target.rect.center);
            return ScreenToLocal(RectTransformUtility.WorldToScreenPoint(uiCamera, center));
        }

        private Vector2 ScreenToLocal(Vector2 screen)
        {
            var parent = (RectTransform)_hand.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, _canvas.worldCamera, out Vector2 local);
            return local;
        }
    }
}

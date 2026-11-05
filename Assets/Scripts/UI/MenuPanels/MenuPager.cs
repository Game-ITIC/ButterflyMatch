using System;
using System.Collections.Generic;
using DG.Tweening;
using UI;
using UnityEngine;
using UnityEngine.UI;

// Main menu screens laid side by side (Island | Main | Shop) on one strip.
// The strip follows the finger on a horizontal swipe and snaps to the nearest page;
// the nav bar buttons slide to their page. Pages are screens, not popups: no scale/fade, no close button.
public class MenuPager : MonoBehaviour
{
    public enum Page
    {
        Island = 0,
        Main = 1,
        Shop = 2
    }

    [SerializeField] private RectTransform _strip;
    [Tooltip("Page slots in order: Island, Main, Shop. Each is a full-screen child of the strip.")]
    [SerializeField] private RectTransform[] _pages = new RectTransform[3];
    [SerializeField] private MenuNavPanel _navPanel;
    [Tooltip("Popups live here; swiping is off while any of its children is active.")]
    [SerializeField] private Transform _popupRoot;
    [SerializeField] private Page _startPage = Page.Main;
    [Tooltip("HUD bits that belong to the Main screen only (Tasks/Daily side buttons); they fade out as Main slides away.")]
    [SerializeField] private RectTransform[] _mainPageOnly;

    [Header("Swipe")]
    [Tooltip("Share of the screen width the finger has to travel to switch page.")]
    [SerializeField, Range(0.05f, 0.5f)] private float _switchDistance = 0.2f;
    [Tooltip("Flick speed (screen widths per second) that switches page regardless of distance.")]
    [SerializeField, Min(0f)] private float _flickSpeed = 1f;
    [Tooltip("Share of the screen width the finger must move before the swipe starts.")]
    [SerializeField, Range(0.005f, 0.1f)] private float _startDistance = 0.025f;
    [Tooltip("How much more horizontal than vertical the move must be to count as a swipe.")]
    [SerializeField, Min(1f)] private float _horizontalBias = 1.2f;
    [Tooltip("How far the first/last page follows the finger past the edge.")]
    [SerializeField, Range(0f, 1f)] private float _edgeResistance = 0.25f;
    [SerializeField, Min(0.05f)] private float _snapDuration = 0.3f;

    public event Action<Page> PageChanged;

    public Page Current { get; private set; }

    private readonly List<ScrollRect> _pausedScrolls = new();
    private Canvas[] _pageCanvases;
    private CanvasGroup[] _mainOnlyGroups;
    private CanvasGroup _group;
    private Tween _tween;
    private float _position;
    private bool _wasPressed;
    private bool _tracking;
    private bool _dragging;
    private bool _restoreInput;
    private Vector2 _pressPosition;
    private float _dragStartPosition;
    private float _lastPointerX;
    private float _velocity;

    private float Width => ((RectTransform)transform).rect.width;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();

        _pageCanvases = new Canvas[_pages.Length];
        for (var i = 0; i < _pages.Length; i++)
        {
            _pageCanvases[i] = _pages[i] != null ? _pages[i].GetComponent<Canvas>() : null;
        }

        var mainOnlyCount = _mainPageOnly != null ? _mainPageOnly.Length : 0;
        _mainOnlyGroups = new CanvasGroup[mainOnlyCount];
        for (var i = 0; i < mainOnlyCount; i++)
        {
            var target = _mainPageOnly[i];
            if (target == null) continue;
            _mainOnlyGroups[i] = target.GetComponent<CanvasGroup>();
            if (_mainOnlyGroups[i] == null) _mainOnlyGroups[i] = target.gameObject.AddComponent<CanvasGroup>();
        }

        Current = _startPage;
        SetPosition((int)_startPage);
    }

    private void OnEnable()
    {
        Bind(_navPanel != null ? _navPanel.IslandButton : null, ShowIsland);
        Bind(_navPanel != null ? _navPanel.MenuButton : null, ShowMain);
        Bind(_navPanel != null ? _navPanel.ShopButton : null, ShowShop);
    }

    private void Start()
    {
        Highlight();
    }

    private void OnDisable()
    {
        Unbind(_navPanel != null ? _navPanel.IslandButton : null, ShowIsland);
        Unbind(_navPanel != null ? _navPanel.MenuButton : null, ShowMain);
        Unbind(_navPanel != null ? _navPanel.ShopButton : null, ShowShop);

        _tween?.Kill();
        if (_dragging)
        {
            ResumeInput();
        }

        _tracking = false;
        _dragging = false;
    }

    public void ShowIsland() => GoTo(Page.Island);

    public void ShowMain() => GoTo(Page.Main);

    public void ShowShop() => GoTo(Page.Shop);

    public void GoTo(Page page, bool animate = true)
    {
        if (page != Current)
        {
            Current = page;
            OnPageChanged();
        }

        AnimateTo((int)page, animate);
    }

    // Lets page views (IslandPanel, InAppView) open/close themselves without knowing their slot.
    public bool Contains(Component view) => FindPage(view) >= 0;

    public bool IsCurrent(Component view) => FindPage(view) == (int)Current;

    public void Open(Component view)
    {
        var index = FindPage(view);
        if (index >= 0)
        {
            GoTo((Page)index);
        }
    }

    public void CloseIfCurrent(Component view)
    {
        if (IsCurrent(view))
        {
            GoTo(Page.Main);
        }
    }

    private int FindPage(Component view)
    {
        if (view == null)
        {
            return -1;
        }

        for (var i = 0; i < _pages.Length; i++)
        {
            if (_pages[i] != null && view.transform.IsChildOf(_pages[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private void Update()
    {
        var pressed = ReadPointer(out var pointer);

        if (!_tracking)
        {
            if (pressed && !_wasPressed && CanStartSwipe())
            {
                _tracking = true;
                _pressPosition = pointer;
            }
        }
        else if (pressed)
        {
            if (!_dragging)
            {
                TryBeginDrag(pointer);
            }

            if (_dragging)
            {
                Drag(pointer.x);
            }
        }
        else
        {
            if (_dragging)
            {
                EndDrag();
            }

            _tracking = false;
        }

        _wasPressed = pressed;
    }

    // Screen size / safe area changes move the page slots; keep the strip on the current page.
    private void OnRectTransformDimensionsChange()
    {
        if (_pageCanvases != null && !_dragging && (_tween == null || !_tween.IsActive()))
        {
            SetPosition(_position);
        }
    }

    private void LateUpdate()
    {
        // Runs after the EventSystem has handled the release, so the swipe never ends in a button click.
        if (_restoreInput && !_dragging)
        {
            _restoreInput = false;
            ResumeInput();
        }
    }

    private static bool ReadPointer(out Vector2 position)
    {
        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            position = touch.position;
            return touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
        }

        position = Input.mousePosition;
        return Input.GetMouseButton(0);
    }

    private bool CanStartSwipe()
    {
        if (_strip == null || Screen.width <= 0 || Input.touchCount > 1)
        {
            return false;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Utils.DebugTools.CheatPanel.BlocksGameInput)
        {
            return false;
        }
#endif

        if (_popupRoot != null)
        {
            for (var i = 0; i < _popupRoot.childCount; i++)
            {
                if (_popupRoot.GetChild(i).gameObject.activeSelf)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void TryBeginDrag(Vector2 pointer)
    {
        var delta = pointer - _pressPosition;
        var threshold = Screen.width * _startDistance;
        var dx = Mathf.Abs(delta.x);
        var dy = Mathf.Abs(delta.y);

        if (dx > threshold && dx > dy * _horizontalBias)
        {
            _dragging = true;
            _tween?.Kill();
            _dragStartPosition = _position;
            // Start from the current finger point so the page does not jump by the threshold.
            _pressPosition = pointer;
            _lastPointerX = pointer.x;
            _velocity = 0f;
            PauseInput();
        }
        else if (dy > threshold)
        {
            // Vertical move: leave it to the page scroll views.
            _tracking = false;
        }
    }

    private void Drag(float pointerX)
    {
        var travelled = (pointerX - _pressPosition.x) / Screen.width;
        var position = _dragStartPosition - travelled;

        var last = _pages.Length - 1;
        if (position < 0f) position *= _edgeResistance;
        else if (position > last) position = last + (position - last) * _edgeResistance;

        var dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        var frameVelocity = -(pointerX - _lastPointerX) / Screen.width / dt;
        _velocity = Mathf.Lerp(_velocity, frameVelocity, 0.5f);
        _lastPointerX = pointerX;

        SetPosition(position);
    }

    private void EndDrag()
    {
        _dragging = false;
        _restoreInput = true;

        var target = (int)Current;
        var moved = _position - target;
        if (moved > _switchDistance || _velocity > _flickSpeed) target++;
        else if (moved < -_switchDistance || _velocity < -_flickSpeed) target--;

        target = Mathf.Clamp(target, 0, _pages.Length - 1);
        GoTo((Page)target);
    }

    // While the strip follows the finger, page buttons must not click and scroll views must not scroll.
    private void PauseInput()
    {
        if (_restoreInput)
        {
            // The previous swipe has not handed input back yet; do it first so no scroll view stays disabled.
            _restoreInput = false;
            ResumeInput();
        }

        _group.blocksRaycasts = false;
        _pausedScrolls.Clear();
        for (var i = 0; i < _pages.Length; i++)
        {
            if (_pages[i] == null) continue;

            var scrolls = _pages[i].GetComponentsInChildren<ScrollRect>();
            for (var s = 0; s < scrolls.Length; s++)
            {
                if (scrolls[s].enabled)
                {
                    scrolls[s].StopMovement();
                    scrolls[s].enabled = false;
                    _pausedScrolls.Add(scrolls[s]);
                }
            }
        }
    }

    private void ResumeInput()
    {
        _group.blocksRaycasts = true;
        for (var i = 0; i < _pausedScrolls.Count; i++)
        {
            if (_pausedScrolls[i] != null) _pausedScrolls[i].enabled = true;
        }

        _pausedScrolls.Clear();
    }

    private void AnimateTo(int index, bool animate)
    {
        _tween?.Kill();
        if (!animate || !isActiveAndEnabled)
        {
            SetPosition(index);
            return;
        }

        var distance = Mathf.Abs(index - _position);
        if (distance < 0.001f)
        {
            SetPosition(index);
            return;
        }

        var duration = _snapDuration * Mathf.Clamp(distance, 0.5f, 1.4f);
        _tween = DOTween.To(() => _position, SetPosition, index, duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void SetPosition(float position)
    {
        _position = position;
        if (_strip != null)
        {
            // Page slots are anchored one screen apart, so the Main slot sits at strip x = 0.
            _strip.anchoredPosition = new Vector2(-(position - (int)Page.Main) * Width, _strip.anchoredPosition.y);
        }

        if (_navPanel != null)
        {
            _navPanel.SetSelectionPosition(position);
        }

        // Pages fully off screen stop rendering.
        for (var i = 0; i < _pageCanvases.Length; i++)
        {
            var canvas = _pageCanvases[i];
            if (canvas == null) continue;

            var visible = Mathf.Abs(position - i) < 0.999f;
            if (canvas.enabled != visible) canvas.enabled = visible;
        }

        // Main-only HUD is gone by the time Main is half off screen.
        var mainAlpha = 1f - Mathf.Clamp01(Mathf.Abs(position - (int)Page.Main) * 2f);
        for (var i = 0; _mainOnlyGroups != null && i < _mainOnlyGroups.Length; i++)
        {
            var group = _mainOnlyGroups[i];
            if (group == null) continue;

            group.alpha = mainAlpha;
            group.blocksRaycasts = mainAlpha > 0.5f;
        }
    }

    private void OnPageChanged()
    {
        var page = _pages[(int)Current];
        if (page != null)
        {
            // Shop cards are spawned at runtime; give them the usual press feedback.
            UIButtonFeel.InstallInChildren(page);
        }

        Highlight();
        PageChanged?.Invoke(Current);
    }

    private void Highlight()
    {
        if (_navPanel == null) return;

        var button = Current switch
        {
            Page.Island => _navPanel.IslandButton,
            Page.Shop => _navPanel.ShopButton,
            _ => _navPanel.MenuButton
        };
        _navPanel.SetSelected(button);
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void Unbind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null) button.onClick.RemoveListener(action);
    }
}

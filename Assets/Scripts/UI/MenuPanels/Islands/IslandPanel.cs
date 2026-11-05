using System.Collections.Generic;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

// Regions panel: one titled section per RegionSO with a row of island cards (filled by IslandSelectPresenter).
public class IslandPanel : MonoBehaviour
{
    [SerializeField] private Transform _container;
    [SerializeField] private RegionCardView _regionCardPrefab;
    [SerializeField] private Button[] _closeButtons;

    // Region section template (title + _container card row). BeginRegion clones it for every region after the first.
    [SerializeField] private RectTransform _regionSection;
    [SerializeField] private TMP_Text _regionTitle;

    [Header("Animation Settings")]
    [SerializeField] private bool animatePanelTransitions = true;
    [SerializeField, Min(0f)] private float panelOpenDuration = 0.28f;
    [SerializeField, Min(0f)] private float panelCloseDuration = 0.18f;
    [SerializeField] private AnimationCurve panelOpenCurve = CurvedUIPanelAnimator.CreateDefaultOpenCurve();
    [SerializeField] private AnimationCurve panelCloseCurve = CurvedUIPanelAnimator.CreateDefaultCloseCurve();
    [SerializeField] private AnimationCurve panelFadeCurve = CurvedUIPanelAnimator.CreateDefaultFadeCurve();

    private readonly List<RegionCardView> _cards = new();
    private readonly List<GameObject> _sections = new();
    private readonly List<TMP_Text> _cardNames = new();
    private Transform _activeContainer;
    private int _regionCount;

    private MenuPager _pager;
    private bool _pagerResolved;

    // Inside the main menu pager the panel is a swipeable screen instead of a popup.
    private MenuPager Pager
    {
        get
        {
            if (!_pagerResolved)
            {
                _pagerResolved = true;
                _pager = GetComponentInParent<MenuPager>(true);
                if (_pager != null && !_pager.Contains(this)) _pager = null;
            }

            return _pager;
        }
    }

    public bool IsPaged => Pager != null;

    // A panel that is fading out no longer counts as open.
    public bool IsOpen => IsPaged
        ? Pager.IsCurrent(this)
        : gameObject.activeSelf && (!TryGetComponent(out CanvasGroup group) || group.interactable);

    private void Awake()
    {
        if (_closeButtons == null)
        {
            return;
        }

        for (var i = 0; i < _closeButtons.Length; i++)
        {
            var button = _closeButtons[i];
            if (button != null)
            {
                button.onClick.AddListener(Hide);
                // A screen is left by swiping or the nav bar, not by a close button.
                if (IsPaged) button.gameObject.SetActive(false);
            }
        }
    }

    public void Show()
    {
        if (IsPaged)
        {
            Pager.Open(this);
            return;
        }

        CurvedUIPanelAnimator.Show(
            gameObject,
            animatePanelTransitions ? panelOpenDuration : 0f,
            panelOpenCurve,
            panelFadeCurve);

        if (animatePanelTransitions)
        {
            UIFeel.Stagger(_cards);
        }
    }

    public void Hide()
    {
        if (IsPaged)
        {
            Pager.CloseIfCurrent(this);
            return;
        }

        CurvedUIPanelAnimator.Hide(
            gameObject,
            animatePanelTransitions ? panelCloseDuration : 0f,
            panelCloseCurve,
            panelFadeCurve);
    }

    public void HideImmediate()
    {
        if (IsPaged)
        {
            return;
        }

        CurvedUIPanelAnimator.HideImmediate(gameObject);
    }

    // Starts a titled region section; following CreateCard calls go into its card row.
    // The first region reuses the template section, later regions get a clone of it.
    public void BeginRegion(string title)
    {
        if (_regionSection == null || _container == null)
        {
            return;
        }

        var section = _regionSection;
        var container = _container;
        var titleText = _regionTitle;
        if (_regionCount > 0)
        {
            section = Instantiate(_regionSection, _regionSection.parent);
            section.SetSiblingIndex(_regionSection.GetSiblingIndex() + _regionCount);
            container = Counterpart(_container, _regionSection, section);
            titleText = _regionTitle != null ? Counterpart(_regionTitle.transform, _regionSection, section).GetComponent<TMP_Text>() : null;
            _sections.Add(section.gameObject);

            // The template row already holds the previous region's cards; drop the copies.
            for (var i = container.childCount - 1; i >= 0; i--)
            {
                var child = container.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        if (titleText != null)
        {
            titleText.text = title;
        }

        _activeContainer = container;
        _regionCount++;
    }

    public RegionCardView CreateCard()
    {
        var container = _activeContainer != null ? _activeContainer : _container;
        if (_regionCardPrefab == null || container == null)
        {
            Debug.LogWarning("[IslandPanel] Card prefab or container is not assigned.", this);
            return null;
        }

        var card = Instantiate(_regionCardPrefab, container);
        card.gameObject.SetActive(true);
        _cards.Add(card);
        return card;
    }

    // All island names on the screen share one font size, whatever the longest name in this locale is.
    public void MatchCardNameSizes()
    {
        _cardNames.Clear();
        for (var i = 0; i < _cards.Count; i++)
        {
            if (_cards[i] != null)
            {
                _cardNames.Add(_cards[i].NameLabel);
            }
        }

        UITextFit.MatchSmallest(_cardNames);
    }

    public void ClearCards()
    {
        for (var i = _cards.Count - 1; i >= 0; i--)
        {
            if (_cards[i] != null)
            {
                Destroy(_cards[i].gameObject);
            }
        }

        _cards.Clear();

        for (var i = _sections.Count - 1; i >= 0; i--)
        {
            if (_sections[i] != null)
            {
                _sections[i].SetActive(false);
                Destroy(_sections[i]);
            }
        }

        _sections.Clear();
        _activeContainer = null;
        _regionCount = 0;
    }

    // Finds the clone's transform that matches templateChild (same sibling-index path under the section root).
    private static Transform Counterpart(Transform templateChild, Transform templateRoot, Transform cloneRoot)
    {
        var path = new List<int>();
        for (var t = templateChild; t != null && t != templateRoot; t = t.parent)
        {
            path.Add(t.GetSiblingIndex());
        }

        var result = cloneRoot;
        for (var i = path.Count - 1; i >= 0; i--)
        {
            result = result.GetChild(path[i]);
        }

        return result;
    }
}

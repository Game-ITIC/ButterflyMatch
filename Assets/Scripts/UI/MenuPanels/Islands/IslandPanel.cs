using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class IslandPanel : MonoBehaviour
{
    [SerializeField] private Transform _container;
    [SerializeField] private RegionCardView _regionCardPrefab;
    [SerializeField] private Button[] _closeButtons;

    [Header("Animation Settings")]
    [SerializeField] private bool animatePanelTransitions = true;
    [SerializeField, Min(0f)] private float panelOpenDuration = 0.28f;
    [SerializeField, Min(0f)] private float panelCloseDuration = 0.18f;
    [SerializeField] private AnimationCurve panelOpenCurve = CurvedUIPanelAnimator.CreateDefaultOpenCurve();
    [SerializeField] private AnimationCurve panelCloseCurve = CurvedUIPanelAnimator.CreateDefaultCloseCurve();
    [SerializeField] private AnimationCurve panelFadeCurve = CurvedUIPanelAnimator.CreateDefaultFadeCurve();

    private readonly List<RegionCardView> _cards = new();

    // A panel that is fading out no longer counts as open.
    public bool IsOpen => gameObject.activeSelf && (!TryGetComponent(out CanvasGroup group) || group.interactable);

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
            }
        }
    }

    public void Show()
    {
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
        CurvedUIPanelAnimator.Hide(
            gameObject,
            animatePanelTransitions ? panelCloseDuration : 0f,
            panelCloseCurve,
            panelFadeCurve);
    }

    public void HideImmediate()
    {
        CurvedUIPanelAnimator.HideImmediate(gameObject);
    }

    public RegionCardView CreateCard()
    {
        if (_regionCardPrefab == null || _container == null)
        {
            Debug.LogWarning("[IslandPanel] Card prefab or container is not assigned.", this);
            return null;
        }

        var card = Instantiate(_regionCardPrefab, _container);
        card.gameObject.SetActive(true);
        _cards.Add(card);
        return card;
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
    }
}

using System;
using System.Collections.Generic;
using Configs;
using Services;
using Services.Islands;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class BuildingCatalogPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _title;
    [SerializeField] private Transform _content;
    [SerializeField] private BuildingCardView _cardPrefab;
    [SerializeField] private Button _closeButton;
    [SerializeField] private ScrollRect _scroll;

    [Header("Animation Settings")]
    [SerializeField] private bool animatePanelTransitions = true;
    [SerializeField, Min(0f)] private float panelOpenDuration = 0.28f;
    [SerializeField, Min(0f)] private float panelCloseDuration = 0.18f;
    [SerializeField] private AnimationCurve panelOpenCurve = CurvedUIPanelAnimator.CreateDefaultOpenCurve();
    [SerializeField] private AnimationCurve panelCloseCurve = CurvedUIPanelAnimator.CreateDefaultCloseCurve();
    [SerializeField] private AnimationCurve panelFadeCurve = CurvedUIPanelAnimator.CreateDefaultFadeCurve();

    private readonly List<BuildingCardView> _cards = new();

    // A panel that is fading out no longer counts as open.
    public bool IsOpen => gameObject.activeSelf && (!TryGetComponent(out CanvasGroup group) || group.interactable);

    private void Awake()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Hide);
        }
    }

    public void Show(IslandSO island, IslandBuildService buildService, Action<BuildingSlot> onBuild, bool resetScroll)
    {
        var opening = !IsOpen;
        if (opening)
        {
            CurvedUIPanelAnimator.Show(
                gameObject,
                animatePanelTransitions ? panelOpenDuration : 0f,
                panelOpenCurve,
                panelFadeCurve);
        }

        if (_title != null)
        {
            _title.text = island != null ? GameLocalization.L10n.Translate(island.DisplayName) : GameLocalization.L10n.Get("build", "Build");
        }

        ClearCards();
        var slots = island != null ? island.Slots : null;
        if (slots == null || _cardPrefab == null || _content == null || buildService == null)
        {
            return;
        }

        // Order and card state come from BuildingCatalogEntries (buildable cheapest first, waiting, built).
        var entries = BuildingCatalogEntries.Build(island, buildService);
        for (var i = 0; i < entries.Count; i++)
        {
            AddCard(entries[i], onBuild);
        }

        if (_content is RectTransform contentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        if (resetScroll && _scroll != null)
        {
            _scroll.verticalNormalizedPosition = 1f;
        }

        if (opening && animatePanelTransitions)
        {
            UIFeel.Stagger(_cards);
        }
    }

    private void AddCard(BuildingCatalogEntry entry, Action<BuildingSlot> onBuild)
    {
        var slot = entry.Slot;
        var subtitle = entry.Built
            ? GameLocalization.L10n.Get("built", "Built")
            : !entry.RequirementsMet
                ? slot.RequirementLabel()
                : entry.CanAfford ? GameLocalization.L10n.Format("stars_n", slot.StarCost) : GameLocalization.L10n.Format("need_stars_n", slot.StarCost);
        var card = Instantiate(_cardPrefab, _content);
        card.gameObject.SetActive(true);
        card.Bind(
            GameLocalization.L10n.Translate(slot.Building.DisplayName),
            subtitle,
            slot.Building.Icon,
            entry.Built ? GameLocalization.L10n.Get("built", "Built") : GameLocalization.L10n.Get("build", "Build"),
            entry.CanBuild,
            () => onBuild?.Invoke(slot));
        _cards.Add(card);
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

    private void ClearCards()
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

using System;
using System.Collections.Generic;
using Configs;
using Services;
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
            _title.text = island != null ? island.DisplayName : "Build";
        }

        ClearCards();
        var slots = island != null ? island.Slots : null;
        if (slots == null || _cardPrefab == null || _content == null || buildService == null)
        {
            return;
        }

        for (var pass = 0; pass < 2; pass++)
        {
            var builtPass = pass == 1;
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.Building == null || buildService.IsBuilt(slot) != builtPass)
                {
                    continue;
                }

                AddCard(island, buildService, slot, onBuild);
            }
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

    private void AddCard(IslandSO island, IslandBuildService buildService, BuildingSlot slot, Action<BuildingSlot> onBuild)
    {
        var built = buildService.IsBuilt(slot);
        var requirementsMet = buildService.CanSelect(island, slot);
        var canAfford = buildService.CanAfford(slot);
        var subtitle = built
            ? "Built"
            : !requirementsMet
                ? slot.RequirementLabel()
                : canAfford ? $"{slot.StarCost} stars" : $"Need {slot.StarCost} stars";
        var card = Instantiate(_cardPrefab, _content);
        card.gameObject.SetActive(true);
        var captured = slot;
        card.Bind(
            slot.Building.DisplayName,
            subtitle,
            slot.Building.Icon,
            built ? "Built" : "Build",
            !built && requirementsMet && canAfford,
            () => onBuild?.Invoke(captured));
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

using System;
using System.Collections.Generic;
using Configs;
using Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingCatalogPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _title;
    [SerializeField] private Transform _content;
    [SerializeField] private BuildingCardView _cardPrefab;
    [SerializeField] private Button _closeButton;
    [SerializeField] private ScrollRect _scroll;

    private readonly List<BuildingCardView> _cards = new();

    private void Awake()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Hide);
        }
    }

    public void Show(IslandSO island, IslandBuildService buildService, Action<BuildingSlot> onBuild)
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (_title != null)
        {
            _title.text = island != null ? island.DisplayName : "Build";
        }

        ClearCards();
        var slots = island != null ? island.Slots : null;
        if (slots == null || _cardPrefab == null || _content == null)
        {
            return;
        }

        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null || slot.Building == null || buildService.IsBuilt(slot))
            {
                continue;
            }

            var requirementsMet = buildService.CanSelect(island, slot);
            var canAfford = buildService.CanAfford(slot);
            var subtitle = !requirementsMet
                ? slot.RequirementLabel()
                : canAfford ? $"{slot.StarCost} stars" : $"Need {slot.StarCost} stars";
            var card = Instantiate(_cardPrefab, _content);
            card.gameObject.SetActive(true);
            var captured = slot;
            card.Bind(
                slot.Building.DisplayName,
                subtitle,
                slot.Building.Icon,
                "Build",
                requirementsMet && canAfford,
                () => onBuild?.Invoke(captured));
            _cards.Add(card);
        }

        if (_content is RectTransform contentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        if (_scroll != null)
        {
            _scroll.verticalNormalizedPosition = 1f;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
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

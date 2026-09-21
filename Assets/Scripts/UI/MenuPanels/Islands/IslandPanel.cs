using System;
using System.Collections.Generic;
using Configs;
using Models;
using UnityEngine;
using UnityEngine.UI;

public class IslandPanel : MonoBehaviour
{
    public event Action<int> OnRegionSelected;

    [SerializeField] private Transform _container;
    [SerializeField] private RegionCardView _regionCardPrefab;
    [SerializeField] private Button _closeButton;

    private readonly List<RegionCardView> _spawnedCards = new();
    private RegionConfig _regionConfig;
    private RegionModel _regionModel;

    private void Awake()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(Hide);
        }
    }

    public void Initialize(RegionConfig regionConfig, RegionModel regionModel)
    {
        _regionConfig = regionConfig;
        _regionModel = regionModel;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        if (_regionConfig == null || _regionModel == null)
        {
            return;
        }

        var targetContainer = _container != null ? _container : transform;

        // Collect any pre-existing cards in container if spawned list is empty
        if (_spawnedCards.Count == 0)
        {
            var existingCards = targetContainer.GetComponentsInChildren<RegionCardView>(true);
            foreach (var existing in existingCards)
            {
                if (existing != null && !_spawnedCards.Contains(existing))
                {
                    _spawnedCards.Add(existing);
                }
            }
        }

        int totalRegions = _regionConfig.Count;

        for (int i = 0; i < totalRegions; i++)
        {
            var regionData = _regionConfig.GetRegionByIndex(i);
            if (regionData == null) continue;

            RegionCardView cardView;
            if (i < _spawnedCards.Count)
            {
                cardView = _spawnedCards[i];
            }
            else
            {
                if (_regionCardPrefab != null)
                {
                    cardView = Instantiate(_regionCardPrefab, targetContainer);
                }
                else
                {
                    Debug.LogWarning("[IslandPanel] _regionCardPrefab is not assigned!");
                    break;
                }
                _spawnedCards.Add(cardView);
            }

            if (cardView != null)
            {
                cardView.gameObject.SetActive(true);
                float progress = _regionModel.GetRegionProgressNormalized(i);
                bool isUnlocked = _regionModel.IsRegionUnlocked(i);
                bool isCurrent = (_regionModel.RegionService != null && _regionModel.RegionService.CurrentRegionIndex == i);

                cardView.Setup(regionData, i, progress, isUnlocked, isCurrent, OnCardClicked);
            }
        }

        // Hide extra cards if config has fewer items than spawned cards
        for (int i = totalRegions; i < _spawnedCards.Count; i++)
        {
            if (_spawnedCards[i] != null)
            {
                _spawnedCards[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnCardClicked(int index)
    {
        OnRegionSelected?.Invoke(index);
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IslandPanel : MonoBehaviour
{
    [SerializeField] private Transform _container;
    [SerializeField] private RegionCardView _regionCardPrefab;
    [SerializeField] private Button[] _closeButtons;

    private readonly List<RegionCardView> _cards = new();

    public bool IsOpen => gameObject.activeSelf;

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
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
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

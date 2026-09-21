using System;
using Configs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RegionCardView : MonoBehaviour
{
    [SerializeField] private TMP_Text _regionNameText;
    [SerializeField] private TMP_Text _progresPercentageText;
    [SerializeField] private Image _progressImage;
    [SerializeField] private Image _regionImage;
    [SerializeField] private Button _cardButton;

    private int _regionIndex;
    private Action<int> _onClickCallback;

    public int RegionIndex => _regionIndex;
    public bool IsUnlocked { get; private set; }
    public bool IsCurrent { get; private set; }

    public void Setup(
        RegionData data,
        int index,
        float progressNormalized,
        bool isUnlocked,
        bool isCurrent,
        Action<int> onClick
    )
    {
        _regionIndex = index;
        _onClickCallback = onClick;
        IsUnlocked = isUnlocked;
        IsCurrent = isCurrent;

        if (data == null) return;

        if (_regionNameText != null)
        {
            _regionNameText.text = !string.IsNullOrEmpty(data.regionName) ? data.regionName : $"Region {index + 1}";
        }

        if (_regionImage != null && data.regionIcon != null)
        {
            _regionImage.sprite = data.regionIcon;
        }

        if (_progressImage != null)
        {
            _progressImage.fillAmount = progressNormalized;
        }

        if (_progresPercentageText != null)
        {
            if (data.isComingSoon)
            {
                _progresPercentageText.text = "Soon";
            }
            else if (!isUnlocked)
            {
                _progresPercentageText.text = "Locked";
            }
            else
            {
                _progresPercentageText.text = $"{Mathf.RoundToInt(progressNormalized * 100f)}%";
            }
        }

        if (_regionImage != null)
        {
            _regionImage.color = (!isUnlocked || data.isComingSoon)
                ? new Color(0.65f, 0.65f, 0.65f, 0.75f)
                : Color.white;
        }

        if (_cardButton == null)
        {
            _cardButton = GetComponent<Button>() ?? GetComponentInChildren<Button>();
            if (_cardButton == null)
            {
                _cardButton = gameObject.AddComponent<Button>();
            }
        }

        if (_cardButton != null)
        {
            _cardButton.interactable = isUnlocked && !data.isComingSoon;
            _cardButton.onClick.RemoveAllListeners();
            _cardButton.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnButtonClicked()
    {
        if (IsUnlocked)
        {
            _onClickCallback?.Invoke(_regionIndex);
        }
    }
}

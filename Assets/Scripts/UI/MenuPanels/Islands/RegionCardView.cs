using System;
using Configs;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class RegionCardView : MonoBehaviour
{
    [SerializeField] private TMP_Text _regionNameText;
    [SerializeField] private TMP_Text _progresPercentageText;
    [SerializeField] private Image _progressImage;
    [SerializeField] private Image _regionImage;
    [SerializeField] private Button _button;

    public int RegionIndex { get; private set; }
    public bool IsUnlocked { get; private set; }
    public bool IsCurrent { get; private set; }

    public void Bind(string islandName, Sprite icon, int builtCount, int totalCount, bool interactable, string status, Action onClick)
    {
        if (_regionNameText != null)
        {
            _regionNameText.text = islandName;
        }

        if (_regionImage != null)
        {
            _regionImage.sprite = icon;
            _regionImage.enabled = icon != null;
        }

        var progress = totalCount > 0 ? (float)builtCount / totalCount : 0f;
        if (_progressImage != null)
        {
            _progressImage.type = Image.Type.Filled;
            _progressImage.fillMethod = Image.FillMethod.Horizontal;
            _progressImage.fillAmount = progress;
        }

        if (_progresPercentageText != null)
        {
            _progresPercentageText.text = string.IsNullOrEmpty(status)
                ? $"{Mathf.RoundToInt(progress * 100f)}%"
                : status;
        }

        if (_button == null)
        {
            return;
        }

        UIButtonFeel.Ensure(_button);
        _button.interactable = interactable;
        _button.onClick.RemoveAllListeners();
        if (interactable && onClick != null)
        {
            _button.onClick.AddListener(() => onClick());
        }
    }

    // Legacy RegionConfig flow (IslandPresenter).
    public void Setup(RegionData data, int index, float progressNormalized, bool isUnlocked, bool isCurrent, Action<int> onClick)
    {
        RegionIndex = index;
        IsUnlocked = isUnlocked;
        IsCurrent = isCurrent;

        if (data == null) return;

        var name = !string.IsNullOrEmpty(data.regionName) ? data.regionName : $"Region {index + 1}";
        var status = data.isComingSoon ? "Soon" : !isUnlocked ? "Locked" : null;
        var interactable = isUnlocked && !data.isComingSoon;

        Bind(name, data.regionIcon, Mathf.RoundToInt(progressNormalized * 100f), 100, interactable, status,
            onClick != null ? () => onClick(index) : null);

        if (_regionImage != null)
        {
            _regionImage.color = interactable ? Color.white : new Color(0.65f, 0.65f, 0.65f, 0.75f);
        }
    }
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RegionCardView : MonoBehaviour
{
    [SerializeField] private TMP_Text _regionNameText;
    [SerializeField] private TMP_Text _progresPercentageText;
    [SerializeField] private Image _progressImage;
    [SerializeField] private Image _regionImage;
    [SerializeField] private Button _button;

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

        _button.interactable = interactable;
        _button.onClick.RemoveAllListeners();
        if (interactable && onClick != null)
        {
            _button.onClick.AddListener(() => onClick());
        }
    }
}

using System;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class RegionCardView : MonoBehaviour
{
    [SerializeField] private TMP_Text _regionNameText;
    [SerializeField] private TMP_Text _progresPercentageText;
    [SerializeField] private Image _progressImage;
    [Tooltip("Bar and percentage together; hidden while the island is locked.")]
    [SerializeField] private GameObject _progressBar;
    [SerializeField] private Image _regionImage;
    [SerializeField] private Button _button;
    [SerializeField] private GameObject _lockIcon;

    public TMP_Text NameLabel => _regionNameText;

    public void Bind(string islandName, Sprite icon, int builtCount, int totalCount, bool interactable, string status, Action onClick)
    {
        Bind(islandName, icon, builtCount, totalCount, interactable, false, status, onClick);
    }

    /// <summary>A locked island shows a lock icon instead of the progress percentage.</summary>
    public void Bind(string islandName, Sprite icon, int builtCount, int totalCount, bool interactable, bool locked, string status, Action onClick)
    {
        if (_lockIcon != null)
        {
            _lockIcon.SetActive(locked);
        }

        if (_regionNameText != null)
        {
            _regionNameText.text = islandName;
        }

        if (_regionImage != null)
        {
            _regionImage.sprite = icon;
            _regionImage.enabled = icon != null;
            // Locked / coming-soon islands look faded.
            _regionImage.color = interactable ? Color.white : new Color(0.6f, 0.6f, 0.68f, 0.75f);
        }

        if (_progressBar != null)
        {
            _progressBar.SetActive(!locked);
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
            _progresPercentageText.gameObject.SetActive(!locked || _lockIcon == null);
            _progresPercentageText.text = string.IsNullOrEmpty(status)
                ? $"{Mathf.RoundToInt(progress * 100f)}%"
                : status;
            // The label is sized for "100%"; a word status must stay on one line.
            _progresPercentageText.enableWordWrapping = false;
            if (!string.IsNullOrEmpty(status))
            {
                var size = _progresPercentageText.fontSize;
                _progresPercentageText.enableAutoSizing = true;
                _progresPercentageText.fontSizeMax = size;
                _progresPercentageText.fontSizeMin = Mathf.Max(8f, size * 0.45f);
            }
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
}

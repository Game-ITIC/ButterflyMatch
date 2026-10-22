using System;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class BuildingCardView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _subtitle;
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _buttonLabel;

    public void Bind(string title, string subtitle, Sprite icon, string buttonText, bool interactable, Action onClick)
    {
        if (_title != null)
        {
            _title.text = title;
        }

        if (_subtitle != null)
        {
            _subtitle.text = subtitle;
        }

        if (_icon != null)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }

        if (_buttonLabel != null)
        {
            _buttonLabel.text = buttonText;
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

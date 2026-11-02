using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TaskView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Image _claimButtonIcon;
    [SerializeField] private Button _claimButton;
    [SerializeField] private TMP_Text _claimButtonText;
    [SerializeField] private TMP_Text _subtitleText;
    [SerializeField] private TMP_Text _titleText;

    public Image Icon => _icon;
    public Image ClaimButtonIcon => _claimButtonIcon;
    public Button ClaimButton => _claimButton;
    public TMP_Text ClaimButtonText => _claimButtonText;
    public TMP_Text SubtitleText => _subtitleText;
    public TMP_Text TitleText => _titleText;

    public void SetTitle(string title)
    {
        if (_titleText) _titleText.text = title;
    }

    public void SetSubtitle(string subtitle)
    {
        if (_subtitleText) _subtitleText.text = subtitle;
    }

    public void SetIcon(Sprite sprite)
    {
        if (!_icon) return;
        _icon.sprite = sprite;
        _icon.gameObject.SetActive(sprite != null);
    }

    public void SetClaimButtonText(string text)
    {
        if (_claimButtonText) _claimButtonText.text = text;
    }

    public void SetClaimButtonIcon(Sprite sprite)
    {
        if (!_claimButtonIcon) return;
        _claimButtonIcon.sprite = sprite;
        _claimButtonIcon.gameObject.SetActive(sprite != null);
    }

    public void SetClaimInteractable(bool isInteractable)
    {
        if (!_claimButton) return;
        UIButtonFeel.Ensure(_claimButton);
        _claimButton.interactable = isInteractable;
        UIFeel.SetBreathing(_claimButton, isInteractable, 0.05f, 1.1f);
    }

    public void SetClaimAction(UnityAction action)
    {
        if (!_claimButton) return;
        _claimButton.onClick.RemoveAllListeners();
        if (action != null) _claimButton.onClick.AddListener(action);
    }

    public void SetVisible(bool isVisible) => gameObject.SetActive(isVisible);
}

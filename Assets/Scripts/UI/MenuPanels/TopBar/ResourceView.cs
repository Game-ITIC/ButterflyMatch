using DG.Tweening;
using Systems.CurrencySystem.Interfaces;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class ResourceView : MonoBehaviour, ICurrencyView
{
    [SerializeField] private TMP_Text _healthText;
    [SerializeField] private Image _image;
    [SerializeField] private Button _addMoreButton;

    [Header("Feel")]
    [SerializeField, Min(0f)] private float _countDuration = 0.45f;
    [SerializeField, Min(0f)] private float _iconPunch = 0.18f;

    private int _shownAmount;
    private bool _hasAmount;
    private Tween _countTween;

    public Button AddMoreButton => _addMoreButton;
    public Image Icon => _image;
    public TMP_Text TextComponent => _healthText;

    public void SetAmount(int amount)
    {
        if (_healthText == null)
        {
            return;
        }

        var animate = _hasAmount && amount != _shownAmount && _countDuration > 0f && isActiveAndEnabled;
        KillCount();
        _hasAmount = true;

        if (!animate)
        {
            SetShownAmount(amount);
            return;
        }

        _countTween = DOTween.To(GetShownAmount, SetShownAmount, amount, _countDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .SetTarget(this)
            .SetLink(gameObject, LinkBehaviour.CompleteAndKillOnDisable);

        UIFeel.Punch(_image != null ? _image : _healthText, _iconPunch);
    }

    public void SetAmount(string text)
    {
        SetText(text);
    }

    public void SetText(string text)
    {
        if (_healthText != null)
        {
            KillCount();
            _hasAmount = false;
            _healthText.SetText(text);
        }
    }

    private int GetShownAmount() => _shownAmount;

    private void SetShownAmount(int amount)
    {
        _shownAmount = amount;
        _healthText.SetText(amount.ToString());
    }

    private void KillCount()
    {
        if (_countTween != null && _countTween.IsActive())
        {
            _countTween.Kill();
        }

        _countTween = null;
    }
}

using System;
using System.Collections;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class CustomToggle : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _toggleHandle;
    [SerializeField] private Image _background;
    [SerializeField] private RectTransform _pointA;
    [SerializeField] private RectTransform _pointB;
    [SerializeField] private bool _isOn;
    [SerializeField] private Color _offColor = Color.white;
    [SerializeField] private Color _onColor = Color.white;
    [SerializeField, Min(0f)] private float _animationDuration = 0.2f;
    [SerializeField] private AnimationCurve _easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private UnityEvent<bool> _onValueChanged;

    private Coroutine _animation;

    public bool IsOn
    {
        get => _isOn;
        set => SetIsOn(value);
    }

    public event Action<bool> ValueChanged;

    private void OnEnable()
    {
        SetVisualState(_isOn ? 1f : 0f);
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) SetVisualState(_isOn ? 1f : 0f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Toggle();
    }

    public void Toggle()
    {
        IsOn = !_isOn;
    }

    public void SetIsOn(bool isOn)
    {
        if (_isOn == isOn) return;

        _isOn = isOn;
        PlayAnimation();
        _onValueChanged?.Invoke(_isOn);
        ValueChanged?.Invoke(_isOn);
    }

    public void SetIsOnWithoutNotify(bool isOn)
    {
        if (_isOn == isOn) return;

        _isOn = isOn;
        PlayAnimation();
    }

    private void PlayAnimation()
    {
        StopAnimation();

        if (!isActiveAndEnabled || _toggleHandle == null || _pointA == null || _pointB == null || _animationDuration <= 0f)
        {
            SetVisualState(_isOn ? 1f : 0f);
            return;
        }

        _animation = StartCoroutine(AnimateRoutine());
        UIFeel.Punch(_toggleHandle, 0.15f, 0.25f);
    }

    private IEnumerator AnimateRoutine()
    {
        var handleRectTransform = _toggleHandle.rectTransform;
        var startPosition = handleRectTransform.position;
        var targetProgress = _isOn ? 1f : 0f;
        var targetPosition = GetHandlePosition(targetProgress);
        var startColor = _background != null ? _background.color : Color.clear;
        var targetColor = GetBackgroundColor(targetProgress);
        var elapsed = 0f;

        while (elapsed < _animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            var progress = Mathf.Clamp01(elapsed / _animationDuration);
            var easedProgress = _easing == null ? progress : _easing.Evaluate(progress);
            handleRectTransform.position = Vector3.LerpUnclamped(startPosition, targetPosition, easedProgress);

            if (_background != null) _background.color = Color.LerpUnclamped(startColor, targetColor, easedProgress);

            yield return null;
        }

        SetVisualState(targetProgress);
        _animation = null;
    }

    private void StopAnimation()
    {
        if (_animation == null) return;

        StopCoroutine(_animation);
        _animation = null;
    }

    private void SetVisualState(float progress)
    {
        if (_toggleHandle != null && _pointA != null && _pointB != null)
        {
            _toggleHandle.rectTransform.position = GetHandlePosition(progress);
        }

        if (_background != null) _background.color = GetBackgroundColor(progress);
    }

    private Vector3 GetHandlePosition(float progress)
    {
        return Vector3.LerpUnclamped(_pointA.position, _pointB.position, progress);
    }

    private Color GetBackgroundColor(float progress)
    {
        return Color.LerpUnclamped(_offColor, _onColor, progress);
    }
}

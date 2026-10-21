using System;
using System.Collections.Generic;
using Services.Settings;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text tMP_Text;
    [SerializeField] private Button[] _closeButtons;
    [SerializeField] private SettingsToggleView _soundToggleView;
    [SerializeField] private SettingsToggleView _musicToggleView;
    [SerializeField] private SettingsToggleView _vibrationToggleView;
    [SerializeField] private SettingsToggleView _notificationToggleView;

    [Header("Animation Settings")]
    [SerializeField] private bool animatePanelTransitions = true;
    [SerializeField, Min(0f)] private float panelOpenDuration = 0.28f;
    [SerializeField, Min(0f)] private float panelCloseDuration = 0.18f;
    [SerializeField] private AnimationCurve panelOpenCurve = CurvedUIPanelAnimator.CreateDefaultOpenCurve();
    [SerializeField] private AnimationCurve panelCloseCurve = CurvedUIPanelAnimator.CreateDefaultCloseCurve();
    [SerializeField] private AnimationCurve panelFadeCurve = CurvedUIPanelAnimator.CreateDefaultFadeCurve();

    private readonly List<(CustomToggle Toggle, Action<bool> Handler)> _boundToggles = new();
    private bool _togglesBound;

    public IReadOnlyList<Button> CloseButtons => _closeButtons ?? Array.Empty<Button>();
    public SettingsToggleView SoundToggleView => _soundToggleView;
    public SettingsToggleView MusicToggleView => _musicToggleView;
    public SettingsToggleView VibrationToggleView => _vibrationToggleView;
    public SettingsToggleView NotificationToggleView => _notificationToggleView;

    private void OnEnable()
    {
        BindPreferenceToggles();
        RefreshToggleVisuals();
    }

    private void OnDisable()
    {
        UnbindPreferenceToggles();
    }

    public void Show()
    {
        CurvedUIPanelAnimator.Show(
            gameObject,
            animatePanelTransitions ? panelOpenDuration : 0f,
            panelOpenCurve,
            panelFadeCurve);
        RefreshToggleVisuals();
    }

    public void Hide()
    {
        CurvedUIPanelAnimator.Hide(
            gameObject,
            animatePanelTransitions ? panelCloseDuration : 0f,
            panelCloseCurve,
            panelFadeCurve);
    }

    public void HideImmediate()
    {
        CurvedUIPanelAnimator.HideImmediate(gameObject);
    }

    public void RefreshToggleVisuals()
    {
        SetToggleWithoutNotify(_soundToggleView, SettingsPreferences.SoundEnabled);
        SetToggleWithoutNotify(_musicToggleView, SettingsPreferences.MusicEnabled);
        SetToggleWithoutNotify(_vibrationToggleView, SettingsPreferences.VibrationEnabled);
        SetToggleWithoutNotify(_notificationToggleView, SettingsPreferences.NotificationsEnabled);
    }

    private void BindPreferenceToggles()
    {
        if (_togglesBound)
        {
            return;
        }

        // Drop a previous partial bind so we never double-subscribe on retry.
        if (_boundToggles.Count > 0)
        {
            UnbindPreferenceToggles();
        }

        var boundCount = 0;
        boundCount += BindToggle(_soundToggleView, SettingsPreferences.SoundEnabled, isOn =>
        {
            SettingsPreferences.SoundEnabled = isOn;
            SettingsPreferences.ApplyAudio();
        }) ? 1 : 0;

        boundCount += BindToggle(_musicToggleView, SettingsPreferences.MusicEnabled, isOn =>
        {
            SettingsPreferences.MusicEnabled = isOn;
            SettingsPreferences.ApplyAudio();
        }) ? 1 : 0;

        boundCount += BindToggle(_vibrationToggleView, SettingsPreferences.VibrationEnabled, isOn =>
        {
            SettingsPreferences.VibrationEnabled = isOn;
            if (isOn)
            {
                SettingsPreferences.TryVibrate();
            }
        }) ? 1 : 0;

        boundCount += BindToggle(_notificationToggleView, SettingsPreferences.NotificationsEnabled, isOn =>
        {
            SettingsPreferences.NotificationsEnabled = isOn;
        }) ? 1 : 0;

        // Only lock binding when every expected row wired; otherwise retry next OnEnable.
        var expected = CountExpectedToggles();
        _togglesBound = expected > 0 && boundCount >= expected;
    }

    private int CountExpectedToggles()
    {
        var count = 0;
        if (_soundToggleView != null) count++;
        if (_musicToggleView != null) count++;
        if (_vibrationToggleView != null) count++;
        if (_notificationToggleView != null) count++;
        return count;
    }

    private void UnbindPreferenceToggles()
    {
        foreach (var (toggle, handler) in _boundToggles)
        {
            if (toggle != null && handler != null)
            {
                toggle.ValueChanged -= handler;
            }
        }

        _boundToggles.Clear();
        _togglesBound = false;
    }

    private bool BindToggle(SettingsToggleView view, bool currentValue, Action<bool> onChanged)
    {
        if (view == null || onChanged == null)
        {
            return false;
        }

        var toggle = view.Toggle;
        if (toggle == null)
        {
            return false;
        }

        toggle.SetIsOnWithoutNotify(currentValue);
        toggle.ValueChanged += onChanged;
        _boundToggles.Add((toggle, onChanged));
        return true;
    }

    private static void SetToggleWithoutNotify(SettingsToggleView view, bool value)
    {
        if (view != null && view.Toggle != null)
        {
            view.Toggle.SetIsOnWithoutNotify(value);
        }
    }
}

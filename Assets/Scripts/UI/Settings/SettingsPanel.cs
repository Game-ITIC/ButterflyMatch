using System;
using System.Collections.Generic;
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
    [SerializeField] private Transform _rowsParent;

    [Header("Animation Settings")]
    [SerializeField] private bool animatePanelTransitions = true;
    [SerializeField, Min(0f)] private float panelOpenDuration = 0.28f;
    [SerializeField, Min(0f)] private float panelCloseDuration = 0.18f;
    [SerializeField] private AnimationCurve panelOpenCurve = CurvedUIPanelAnimator.CreateDefaultOpenCurve();
    [SerializeField] private AnimationCurve panelCloseCurve = CurvedUIPanelAnimator.CreateDefaultCloseCurve();
    [SerializeField] private AnimationCurve panelFadeCurve = CurvedUIPanelAnimator.CreateDefaultFadeCurve();

    public IReadOnlyList<Button> CloseButtons => _closeButtons ?? Array.Empty<Button>();
    public SettingsToggleView SoundToggleView => _soundToggleView;
    public SettingsToggleView MusicToggleView => _musicToggleView;
    public SettingsToggleView VibrationToggleView => _vibrationToggleView;
    public SettingsToggleView NotificationToggleView => _notificationToggleView;

    private Transform RowsParent => _rowsParent != null
        ? _rowsParent
        : _soundToggleView != null ? _soundToggleView.transform.parent : null;

    public void Show()
    {
        var opening = !gameObject.activeSelf;
        CurvedUIPanelAnimator.Show(
            gameObject,
            animatePanelTransitions ? panelOpenDuration : 0f,
            panelOpenCurve,
            panelFadeCurve);

        if (opening && animatePanelTransitions)
        {
            UIFeel.StaggerChildren(RowsParent);
        }
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
}

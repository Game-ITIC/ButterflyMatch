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
    [SerializeField] private Button _restorePurchasesButton;
    [SerializeField] private GameObject _animatedContent;

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
    public Button RestorePurchasesButton => _restorePurchasesButton;

    private GameObject AnimationTarget => _animatedContent != null ? _animatedContent : gameObject;

    private Transform RowsParent => _rowsParent != null
        ? _rowsParent
        : _soundToggleView != null ? _soundToggleView.transform.parent : null;

    public void Show()
    {
        var opening = !gameObject.activeSelf;
        gameObject.SetActive(true);
        CurvedUIPanelAnimator.Show(
            AnimationTarget,
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
            AnimationTarget,
            animatePanelTransitions ? panelCloseDuration : 0f,
            panelCloseCurve,
            panelFadeCurve,
            DeactivateRoot);
    }

    public void HideImmediate()
    {
        CurvedUIPanelAnimator.HideImmediate(AnimationTarget);
        DeactivateRoot();
    }

    private void DeactivateRoot()
    {
        if (this != null)
            gameObject.SetActive(false);
    }
}

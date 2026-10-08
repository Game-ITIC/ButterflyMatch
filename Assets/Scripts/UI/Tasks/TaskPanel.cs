using System;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class TaskPanel : MonoBehaviour
{
    [SerializeField] private Button[] _closeButtons;
    [SerializeField] private TaskView _taskViewPrefab;
    [SerializeField] private Transform _taskViewParent;
    [SerializeField] private GameObject _animatedContent;

    [Header("Animation Settings")]
    [SerializeField] private bool animatePanelTransitions = true;
    [SerializeField, Min(0f)] private float panelOpenDuration = 0.28f;
    [SerializeField, Min(0f)] private float panelCloseDuration = 0.18f;
    [SerializeField] private AnimationCurve panelOpenCurve = CurvedUIPanelAnimator.CreateDefaultOpenCurve();
    [SerializeField] private AnimationCurve panelCloseCurve = CurvedUIPanelAnimator.CreateDefaultCloseCurve();
    [SerializeField] private AnimationCurve panelFadeCurve = CurvedUIPanelAnimator.CreateDefaultFadeCurve();

    public IReadOnlyList<Button> CloseButtons => _closeButtons ?? Array.Empty<Button>();
    public TaskView TaskViewPrefab => _taskViewPrefab;
    public Transform TaskViewParent => _taskViewParent;

    private GameObject AnimationTarget => _animatedContent != null ? _animatedContent : gameObject;

    public void Show()
    {
        gameObject.SetActive(true);
        CurvedUIPanelAnimator.Show(
            AnimationTarget,
            animatePanelTransitions ? panelOpenDuration : 0f,
            panelOpenCurve,
            panelFadeCurve);
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

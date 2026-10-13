using System;
using UnityEngine;
using UnityEngine.UI;

public class TopBarPanel : MonoBehaviour
{
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _taskButton;
    [SerializeField] private HealthBarView _healthBarView;
    [SerializeField] private ResourceView _starView;
    [SerializeField] private ResourceView _gemView;

    public Button SettingsButton
    {
        get
        {
            if (_settingsButton != null) return _settingsButton;
            _settingsButton = FindChildButton("SettingsButton", "Settings Button", "Pause", "Settings", "BtnSettings");
            return _settingsButton;
        }
    }

    public Button TaskButton
    {
        get
        {
            if (_taskButton != null) return _taskButton;
            _taskButton = FindChildButton("TaskButton", "Task Button", "Tasks Button", "Task Button 01");
            return _taskButton;
        }
    }

    public HealthBarView HealthBarView
    {
        get
        {
            if (_healthBarView != null) return _healthBarView;
            _healthBarView = GetComponentInChildren<HealthBarView>(true);
            return _healthBarView;
        }
    }

    public ResourceView StarView => _starView;

    public ResourceView GemView => _gemView;

    private Button FindChildButton(params string[] names)
    {
        var buttons = GetComponentsInChildren<Button>(true);
        foreach (var button in buttons)
        {
            foreach (var name in names)
            {
                if (string.Equals(button.gameObject.name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return button;
                }
            }
        }

        return null;
    }
}

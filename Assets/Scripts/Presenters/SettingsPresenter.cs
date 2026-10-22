using System;
using System.Collections.Generic;
using Services.Settings;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer.Unity;

namespace Presenters
{
    public class SettingsPresenter : IInitializable, IDisposable
    {
        private readonly SettingsPanel _settingsPanel;
        private Button _settingsButton;
        private readonly List<(Button Button, UnityAction Action)> _closeActions = new();

        public SettingsPresenter(SettingsPanel settingsPanel, Button settingsButton)
        {
            _settingsPanel = settingsPanel;
            _settingsButton = settingsButton;
        }

        public void Initialize()
        {
            if (_settingsPanel == null)
            {
                return;
            }

            _settingsPanel.HideImmediate();
            SettingsPreferences.ApplyAudio();

            if (_settingsButton == null)
            {
                _settingsButton = FindButtonInScene("SettingsButton", "Settings Button", "SettingButton", "Settings", "BtnSettings", "Pause");
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
                _settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            }
            else
            {
                Debug.LogWarning("[SettingsPresenter] Settings button could not be found in TopBarPanel or Scene.");
            }

            if (_settingsPanel.CloseButtons != null)
            {
                foreach (var closeButton in _settingsPanel.CloseButtons)
                {
                    if (closeButton == null)
                    {
                        continue;
                    }

                    UnityAction closeAction = OnCloseButtonClicked;
                    closeButton.onClick.RemoveListener(closeAction);
                    closeButton.onClick.AddListener(closeAction);
                    _closeActions.Add((closeButton, closeAction));
                }
            }
        }

        private void OnSettingsButtonClicked()
        {
            _settingsPanel.Show();
        }

        private void OnCloseButtonClicked()
        {
            _settingsPanel.Hide();
        }

        private static Button FindButtonInScene(params string[] names)
        {
            for (var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                var scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded)
                {
                    continue;
                }

                foreach (var root in scene.GetRootGameObjects())
                {
                    var buttons = root.GetComponentsInChildren<Button>(true);
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
                }
            }

            return null;
        }

        public void Dispose()
        {
            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
            }

            foreach (var (button, action) in _closeActions)
            {
                if (button != null && action != null)
                {
                    button.onClick.RemoveListener(action);
                }
            }

            _closeActions.Clear();
        }
    }
}

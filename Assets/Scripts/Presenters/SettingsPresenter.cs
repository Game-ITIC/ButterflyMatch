using System;
using System.Collections.Generic;
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

        public SettingsPresenter(SettingsPanel settingsPanel, Button settingsButton, Services.InApp.InAppGrantService grantService)
        {
            _settingsPanel = settingsPanel;
            _settingsButton = settingsButton;
            _grantService = grantService;
        }

        private readonly Services.InApp.InAppGrantService _grantService;
        private bool _restoring;

        /// <summary>"Restore purchases" (GDD 11.1): brings back No Ads after reinstall / on a new device.</summary>
        private void OnRestoreClicked()
        {
            if (_restoring) return;
            RestoreAsync().Forget();
        }

        private async Cysharp.Threading.Tasks.UniTaskVoid RestoreAsync()
        {
            _restoring = true;
            var button = _settingsPanel.RestorePurchasesButton;
            if (button != null) button.interactable = false;

            try
            {
                var ok = await Services.InApp.InAppPurchasingService.RestoreAsync();

                if (ok && IsRemoveAdsActive())
                {
                    _grantService?.SetRemoveAds();
                }

                UI.Toast.Show(ok
                    ? GameLocalization.L10n.Get("restore_done", "Purchases restored")
                    : GameLocalization.L10n.Get("purchase_store_unavailable", "Store is not available."));
            }
            finally
            {
                _restoring = false;
                if (button != null) button.interactable = true;
            }
        }

        // Gley throws when its product list is not loaded yet (e.g. store initialised without settings), so treat that as "not owned".
        private static bool IsRemoveAdsActive()
        {
            try
            {
                return Gley.EasyIAP.API.IsInitialized() && Gley.EasyIAP.API.IsActive(Gley.EasyIAP.ShopProductNames.RemoveAds);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Settings] Restore: product state unavailable: " + e.Message);
                return false;
            }
        }

        public void Initialize()
        {
            if (_settingsPanel == null) return;

            _settingsPanel.HideImmediate();

            var restore = _settingsPanel.RestorePurchasesButton;
            if (restore != null)
            {
                restore.onClick.RemoveListener(OnRestoreClicked);
                restore.onClick.AddListener(OnRestoreClicked);
            }

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

            BindToggle(_settingsPanel.SoundToggleView, SoundKey, ApplySound);
            BindToggle(_settingsPanel.MusicToggleView, MusicKey, ApplyMusic);
            BindToggle(_settingsPanel.VibrationToggleView, VibrationKey, ApplyVibration);
            BindToggle(_settingsPanel.NotificationToggleView, NotificationsKey, null);

            if (_settingsPanel.CloseButtons != null)
            {
                foreach (var closeButton in _settingsPanel.CloseButtons)
                {
                    if (closeButton == null) continue;

                    UnityAction closeAction = OnCloseButtonClicked;
                    closeButton.onClick.RemoveListener(closeAction);
                    closeButton.onClick.AddListener(closeAction);
                    _closeActions.Add((closeButton, closeAction));
                }
            }
        }

        // Same PlayerPrefs keys JuiceFresh (InitScript / AnimationManager) already uses: 1 = on, 0 = off.
        private const string SoundKey = "Sound";
        private const string MusicKey = "Music";
        private const string VibrationKey = "Vibration";
        private const string NotificationsKey = "Notifications";

        private readonly List<(CustomToggle Toggle, Action<bool> Handler)> _toggleHandlers = new();

        private void BindToggle(SettingsToggleView view, string key, Action<bool> apply)
        {
            var isOn = PlayerPrefs.GetInt(key, 1) != 0;
            apply?.Invoke(isOn);

            var toggle = view != null ? view.Toggle : null;
            if (toggle == null) return;

            toggle.SetIsOnWithoutNotify(isOn);
            Action<bool> handler = value =>
            {
                PlayerPrefs.SetInt(key, value ? 1 : 0);
                PlayerPrefs.Save();
                apply?.Invoke(value);
            };
            toggle.ValueChanged += handler;
            _toggleHandlers.Add((toggle, handler));
        }

        private static void ApplySound(bool isOn)
        {
            InitScript.sound = isOn;
            var source = SoundBase.Instance != null ? SoundBase.Instance.GetComponent<AudioSource>() : null;
            if (source != null) source.volume = isOn ? 1f : 0f;
        }

        private static void ApplyMusic(bool isOn)
        {
            InitScript.music = isOn;
            var source = MusicBase.Instance != null ? MusicBase.Instance.GetComponent<AudioSource>() : null;
            if (source != null) source.volume = isOn ? 1f : 0f;
        }

        private static void ApplyVibration(bool isOn)
        {
            Lofelt.NiceVibrations.HapticController.hapticsEnabled = isOn;
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
                if (!scene.isLoaded) continue;

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

            foreach (var (toggle, handler) in _toggleHandlers)
            {
                if (toggle != null) toggle.ValueChanged -= handler;
            }

            _toggleHandlers.Clear();
        }
    }
}

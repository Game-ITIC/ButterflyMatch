using System;
using Configs;
using Cysharp.Threading.Tasks;
using Itic.Scopes;
using Systems;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Views;

namespace Initializers
{
    /// <summary>
    /// The menu Play button: starts the next level when the player has lives (else opens the life popup)
    /// and shows "Play / Level N", re-translated when the language changes.
    /// </summary>
    internal sealed class CountryPlayButtonBinder : IDisposable
    {
        private readonly SceneLoader _sceneLoader;
        private readonly LevelConfig _levelConfig;
        private readonly HealthSystem _healthSystem;
        private readonly LifePopup _lifePopup;
        private readonly MainMenuPanel _mainMenuPanel;

        private Button _playButton;
        private Action _refreshPlayLabel;

        public CountryPlayButtonBinder(
            SceneLoader sceneLoader,
            LevelConfig levelConfig,
            HealthSystem healthSystem,
            LifePopup lifePopup,
            MainMenuPanel mainMenuPanel)
        {
            _sceneLoader = sceneLoader;
            _levelConfig = levelConfig;
            _healthSystem = healthSystem;
            _lifePopup = lifePopup;
            _mainMenuPanel = mainMenuPanel;
        }

        public void BindButton()
        {
            _playButton = _mainMenuPanel?.MenuActionPanel?.PlayButton?.Button
                          ?? FindButtonInScene("PlayButton", "Play Button", "Play", "StartGame", "Start Game");
            if(_playButton != null)
            {
                Debug.Log($"[CountryInitializer] Bound Play button: {_playButton.gameObject.name}");
                _playButton.onClick.RemoveAllListeners();
                _playButton.onClick.AddListener(StartGame);
            }
            else
            {
                Debug.LogWarning("[CountryInitializer] Play button could not be found!");
            }
        }

        public void ShowLabels()
        {
            var nextLevel = PlayerPrefs.GetInt("OpenLevel", 1);
            var playActionButton = _mainMenuPanel?.MenuActionPanel?.PlayButton;
            if(playActionButton != null)
            {
                playActionButton.SetLabel(GameLocalization.L10n.Get("play", "Play"));
                playActionButton.SetSubLabel(GameLocalization.L10n.Format("level_n", nextLevel));
            }
            else if(_playButton != null)
            {
                var tmp = _playButton.GetComponentInChildren<TMPro.TMP_Text>(true);
                if(tmp != null) tmp.text = GameLocalization.L10n.Format("level_n", nextLevel);
            }

            // Language can be switched in Settings while the menu is open.
            _refreshPlayLabel = () =>
            {
                if(playActionButton != null) { playActionButton.SetLabel(GameLocalization.L10n.Get("play", "Play")); playActionButton.SetSubLabel(GameLocalization.L10n.Format("level_n", nextLevel)); }
            };
            GameLocalization.L10n.Changed += _refreshPlayLabel;
        }

        public void Dispose()
        {
            if(_refreshPlayLabel != null) GameLocalization.L10n.Changed -= _refreshPlayLabel;
        }

        private void StartGame()
        {
            var nextLevel = PlayerPrefs.GetInt("OpenLevel", 1);

            if(_levelConfig.Testing)
            {
                nextLevel = _levelConfig.LevelToPlay;
            }

            if(_healthSystem.CanPlay)
            {
                PlayerPrefs.SetInt("OpenLevel", nextLevel);
                _sceneLoader.LoadGameAsync().Forget();
            }
            else if(_lifePopup != null)
            {
                _lifePopup.Show();
            }
            else
            {
                Debug.LogWarning("Cannot start game: no lives available and life popup is not assigned.");
            }
        }

        private static Button FindButtonInScene(params string[] names)
        {
            for (var sceneIndex = 0; sceneIndex < UnityEngine.SceneManagement.SceneManager.sceneCount; sceneIndex++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    var buttons = root.GetComponentsInChildren<Button>(true);
                    foreach (var button in buttons)
                    {
                        foreach (var name in names)
                        {
                            if (string.Equals(button.gameObject.name.Trim(), name.Trim(), System.StringComparison.OrdinalIgnoreCase))
                            {
                                return button;
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}

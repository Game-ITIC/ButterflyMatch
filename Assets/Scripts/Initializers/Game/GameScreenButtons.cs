using System;
using Configs;
using Cysharp.Threading.Tasks;
using Itic.Scopes;
using Services;
using Services.Ads;
using Systems;
using UnityEngine;
using UnityEngine.UI;
using Views;

namespace Initializers
{
    /// <summary>
    /// The buttons of the win, lose and pause screens: leave to the menu, next level, restart, resume, rewarded ads.
    /// Leaving the win / lose screen is the only place for an interstitial (GDD 8.2); the ad is awaited first
    /// so the next scene does not load under it.
    /// </summary>
    internal sealed class GameScreenButtons
    {
        private readonly GameCompleteView _completeView;
        private readonly GamePauseView _pauseView;
        private readonly GameOverView _gameOverView;
        private readonly SceneLoader _sceneLoader;
        private readonly LevelManager _levelManager;
        private readonly AdRewardService _adRewardService;
        private readonly GameConfig _gameConfig;
        private readonly HealthSystem _healthSystem;
        private readonly InterstitialService _interstitialService;

        private bool _leavingResultScreen;

        public GameScreenButtons(
            GameCompleteView completeView,
            GamePauseView pauseView,
            GameOverView gameOverView,
            SceneLoader sceneLoader,
            LevelManager levelManager,
            AdRewardService adRewardService,
            GameConfig gameConfig,
            HealthSystem healthSystem,
            InterstitialService interstitialService)
        {
            _completeView = completeView;
            _pauseView = pauseView;
            _gameOverView = gameOverView;
            _sceneLoader = sceneLoader;
            _levelManager = levelManager;
            _adRewardService = adRewardService;
            _gameConfig = gameConfig;
            _healthSystem = healthSystem;
            _interstitialService = interstitialService;
        }

        public void Bind()
        {
            // Level progress (OpenLevel) and stars are saved in WinState at the moment of the win.
            Rebind(_completeView.Home, () => LeaveResultScreen(ReturnToMainMenu));
            Rebind(_completeView.Next, () => LeaveResultScreen(LoadGameOrMenuIfNoLives));

            var adsPrice = _completeView.AdsButton.transform.Find("Price");
            if (adsPrice != null && adsPrice.TryGetComponent(out TMPro.TMP_Text adsPriceText))
                adsPriceText.text = "+" + _gameConfig.WinAdDiamondReward;

            Rebind(_completeView.AdsButton, () => _adRewardService.TryShowRewarded(AdRewardType.X2));
            Rebind(_gameOverView.AdsButton, () => _adRewardService.TryShowRewarded(AdRewardType.Continue));

            Rebind(_pauseView.Home, ReturnToMainMenu);
            Rebind(_pauseView.ContinueButton, () =>
            {
                _pauseView.Hide();
                _levelManager.ResumeGame();
            });
            Rebind(_pauseView.RestartButton, RestartGame);

            Rebind(_gameOverView.RestartButton, () => LeaveResultScreen(RestartGame));
            Rebind(_gameOverView.Home, () => LeaveResultScreen(ReturnToMainMenu));
        }

        private static void Rebind(Button button, UnityEngine.Events.UnityAction onClick)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
        }

        private void LeaveResultScreen(Action leave)
        {
            if (_leavingResultScreen)
                return;

            _leavingResultScreen = true;
            LeaveResultScreenAsync(leave).Forget();
        }

        private async UniTaskVoid LeaveResultScreenAsync(Action leave)
        {
            try
            {
                await _interstitialService.TryShowAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            Time.timeScale = 1f;
            leave?.Invoke();
        }

        private void RestartGame()
        {
            Time.timeScale = 1f;
            LoadGameOrMenuIfNoLives();
        }

        /// <summary>Starting a new attempt needs a life; without one go to the menu (it offers the life popup).</summary>
        private void LoadGameOrMenuIfNoLives()
        {
            if (_healthSystem.CanPlay)
                _sceneLoader.LoadGameAsync().Forget();
            else
                _sceneLoader.LoadMenuAsync().Forget();
        }

        private void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            _sceneLoader.LoadMenuAsync().Forget();
        }
    }
}

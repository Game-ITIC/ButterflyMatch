using System;
using System.Threading;
using Configs;
using Cysharp.Threading.Tasks;
using Data;
using Gley.EasyIAP;
using Itic.Scopes;
using JetBrains.Annotations;
using Models;
using Monobehaviours;
using Providers;
using R3;
using Services;
using Services.InApp;
using UI;
using UnityEngine;
using UnityEngine.UI;
using VContainer.Unity;
using Views;

namespace Initializers
{
    [UsedImplicitly]
    public class GameInitializer : IInitializable, IAsyncStartable, ITickable, IDisposable
    // , IDisposable
    {
        // private readonly IronSourceManager _ironSourceManager;
        private readonly IGameEvents _gameEvents;
        private readonly InternetState _internetState;
        private readonly GameCompleteView _gameCompleteView;
        private readonly GamePauseView _gamePauseView;
        private readonly GameOverView _gameOverView;
        private readonly SceneLoader _sceneLoader;
        private readonly LevelManager _levelManager;
        private readonly AdRewardService _adRewardService;
        private readonly IronSourceManager _ironSourceManager;
        private readonly AdEventModel _adEventModel;
        private readonly GameProvider _gameProvider;
        private readonly BoostShopView _boostShopView;
        private readonly GameConfig _gameConfig;
        private readonly Systems.CurrencySystem.Interfaces.ICurrencyService _currencyService;
        private readonly Systems.HealthSystem _healthSystem;
        private readonly Services.Ads.InterstitialService _interstitialService;
        private readonly Services.Tasks.ITaskService _taskService;
        private Button _pauseButton;
        private bool _leavingResultScreen;

        private CompositeDisposable _disposable = new();

        public GameInitializer(
            // IronSourceManager ironSourceManager,
            IGameEvents gameEvents,
            InternetState internetState,
            GameCompleteView gameCompleteView,
            GamePauseView gamePauseView,
            GameOverView gameOverView,
            SceneLoader sceneLoader,
            LevelManager levelManager,
            AdRewardService adRewardService,
            IronSourceManager ironSourceManager,
            AdEventModel adEventModel,
            GameProvider gameProvider,
            BoostShopView boostShopView,
            GameConfig gameConfig,
            Systems.CurrencySystem.Interfaces.ICurrencyService currencyService,
            Systems.HealthSystem healthSystem,
            Services.Ads.InterstitialService interstitialService,
            Services.Tasks.ITaskService taskService
        )
        {
            // _ironSourceManager = ironSourceManager;
            _gameEvents = gameEvents;
            _internetState = internetState;
            _gameCompleteView = gameCompleteView;
            _gamePauseView = gamePauseView;
            _gameOverView = gameOverView;
            _sceneLoader = sceneLoader;
            _levelManager = levelManager;
            _adRewardService = adRewardService;
            _ironSourceManager = ironSourceManager;
            _adEventModel = adEventModel;
            _gameProvider = gameProvider;
            _boostShopView = boostShopView;
            _gameConfig = gameConfig;
            _currencyService = currencyService;
            _healthSystem = healthSystem;
            _interstitialService = interstitialService;
            _taskService = taskService;
        }

        public void Initialize()
        {
            EnsurePauseButtonBound();

            UIButtonFeel.InstallInChildren(_gameCompleteView.transform.root);
            if(_levelManager.Level != null)
            {
                UIButtonFeel.InstallInChildren(_levelManager.Level.transform);
            }

            // Level progress (OpenLevel) and stars are saved in WinState at the moment of the win.
            _gameCompleteView.Home.onClick.RemoveAllListeners();
            _gameCompleteView.Home.onClick.AddListener(() => LeaveResultScreen(() => _sceneLoader.LoadMenuAsync().Forget()));

            _gameCompleteView.Next.onClick.RemoveAllListeners();
            _gameCompleteView.Next.onClick.AddListener(() => LeaveResultScreen(LoadGameOrMenuIfNoLives));

            var adsPrice = _gameCompleteView.AdsButton.transform.Find("Price");
            if(adsPrice != null && adsPrice.TryGetComponent(out TMPro.TMP_Text adsPriceText))
            {
                adsPriceText.text = "+" + _gameConfig.WinAdDiamondReward;
            }

            _gameCompleteView.AdsButton.onClick.RemoveAllListeners();
            _gameCompleteView.AdsButton.onClick.AddListener(() => _adRewardService.TryShowRewarded(AdRewardType.X2));

            _gameOverView.AdsButton.onClick.RemoveAllListeners();
            _gameOverView.AdsButton.onClick.AddListener(() => _adRewardService.TryShowRewarded(AdRewardType.Continue));

            _gamePauseView.Home.onClick.RemoveAllListeners();
            _gamePauseView.Home.onClick.AddListener(ReturnToMainMenu);

            _gamePauseView.ContinueButton.onClick.RemoveAllListeners();
            _gamePauseView.ContinueButton.onClick.AddListener(() =>
                                                              {
                                                                  _gamePauseView.Hide();
                                                                  _levelManager.ResumeGame();
                                                              });

            _gamePauseView.RestartButton.onClick.RemoveAllListeners();
            _gamePauseView.RestartButton.onClick.AddListener(RestartGame);

            _gameOverView.RestartButton.onClick.RemoveAllListeners();
            _gameOverView.RestartButton.onClick.AddListener(() => LeaveResultScreen(RestartGame));

            _gameOverView.Home.onClick.RemoveAllListeners();
            _gameOverView.Home.onClick.AddListener(() => LeaveResultScreen(ReturnToMainMenuAfterLose));


            // One subscription for the whole game scene (disposed with it), keyed by the granted reward type.
            _adRewardService.OnRewardGrantedTyped.Subscribe(OnAdRewardGranted).AddTo(_disposable);

            if(!_healthSystem.ConsumeLifeForLevel())
            {
                Debug.LogWarning("[GameInitializer] Level started without lives.");
            }

            Models.LevelSessionStats.Reset();

            _levelManager.InvokeStart();

            // The level is loaded later (PrepareGameState), so take its number from the save.
            Services.GameAnalyticsEvents.LevelStart(JuiceFresh.Scripts.LevelProgressionHelper.CalculateLevel(Mathf.Max(1, PlayerPrefs.GetInt("OpenLevel", 1))));
        }

        /// <summary>
        /// Leaving the win / lose screen (Home, Next, Restart) is the only place for an interstitial (GDD 8.2).
        /// The ad is awaited first so the next scene does not load under it.
        /// </summary>
        private void LeaveResultScreen(Action leave)
        {
            if(_leavingResultScreen)
            {
                return;
            }

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

        public void RestartGame()
        {
            Time.timeScale = 1f;
            LoadGameOrMenuIfNoLives();
        }

        /// <summary>Starting a new attempt needs a life; without one go to the menu (it offers the life popup).</summary>
        private void LoadGameOrMenuIfNoLives()
        {
            if(_healthSystem.CanPlay)
            {
                _sceneLoader.LoadGameAsync().Forget();
            }
            else
            {
                _sceneLoader.LoadMenuAsync().Forget();
            }
        }

        private void OnAdRewardGranted(AdRewardType rewardType)
        {
            _boostShopView.Hide();
            var popup = _gameProvider.RewardPopup;

            switch (rewardType)
            {
                case AdRewardType.X2:
                    // Win-screen bonus: gems (stars stay as earned, GDD 4.2).
                    popup.Init(new RewardInfo
                    {
                        Icon = _gameProvider.DiamondSprite,
                        Description = GameLocalization.L10n.Format("reward_gems_n", _gameConfig.WinAdDiamondReward)
                    });
                    _gameCompleteView.AdsButton.gameObject.SetActive(false);
                    break;
                case AdRewardType.Continue:
                    popup.Init(new RewardInfo
                    {
                        Icon = _gameProvider.ExtraStepSprite,
                        Description = GameLocalization.L10n.Format("reward_extra_moves", _levelManager.ExtraFailedMoves)
                    });
                    break;
                default:
                    popup.Init(_adRewardService.GetInfo());
                    break;
            }

            popup.Show();
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            _sceneLoader.LoadMenuAsync().Forget();
        }

        public void ReturnToMainMenuAfterLose()
        {
            Time.timeScale = 1f;
            _sceneLoader.LoadMenuAsync().Forget();
        }

        private void EnsurePauseButtonBound()
        {
            if(_pauseButton != null)
            {
                return;
            }

            Transform pauseTransform = null;
            if(_levelManager.Level != null)
            {
                pauseTransform = _levelManager.Level.transform.Find("Canvas/Panel/TopBarPanel/Pause");
            }

            GameObject pauseObject = pauseTransform != null ? pauseTransform.gameObject : GameObject.Find("Pause");
            if(pauseObject == null || !pauseObject.TryGetComponent(out Button pauseButton))
            {
                return;
            }

            _pauseButton = pauseButton;
            _pauseButton.onClick.RemoveAllListeners();
            _pauseButton.onClick.AddListener(PauseGame);
        }

        private void PauseGame()
        {
            if(_levelManager.gameStatus != GameState.Playing)
            {
                return;
            }

            if(SoundBase.Instance != null && SoundBase.Instance.click != null)
            {
                SoundBase.Instance.PlaySound(SoundBase.Instance.click);
            }

            _gamePauseView.Show();
            _levelManager.gameStatus = GameState.Pause;
        }

        public async UniTask StartAsync(CancellationToken cancellation = new CancellationToken())
        {
            _gameEvents.OnGameLost += OnLevelLost;
            _gameEvents.OnGameWon += OnLevelWon;
            _gameEvents.OnEnterGame += GameStart;

            await UniTask.Yield();
        }

        private void GameStart()
        {
            //     if (_internetState.HasInternet && !_internetState.HasRemoveAds)
            //         _ironSourceManager.LoadInterstitial();
            //     // _ironSourceManager.LoadBannerAd();
        }

        private bool _winCounted;
        private bool _loseCounted;

        private void OnLevelWon()
        {
            if(_winCounted)
            {
                return;
            }

            _winCounted = true;
            ContextOffers.RegisterWin();
            if(!_loseCounted)
            {
                _interstitialService.OnLevelFinished();
            }

            var stars = _levelManager.stars;
            Services.GameAnalyticsEvents.LevelWin(_levelManager.currentLevel, _levelManager.Limit, stars, Models.LevelSessionStats.BoostersUsed);

            // Daily tasks (Docs/TaskSystem_TODO.md): level, 3-star level, collected pieces, used boosters.
            _taskService.TrackProgress(Meta.Quests.Models.QuestType.CompleteLevels);
            if(stars >= 3)
            {
                _taskService.TrackProgress(Meta.Quests.Models.QuestType.PerfectLevels);
            }

            TrackCollectedAndBoosters();
        }

        private void OnLevelLost()
        {
            // A lose screen can open again after a continue; count the attempt once.
            if(!_loseCounted)
            {
                _loseCounted = true;
                _interstitialService.OnLevelFinished();
                ContextOffers.RegisterLoss(_levelManager.currentLevel);
                Services.GameAnalyticsEvents.LevelFail(_levelManager.currentLevel, Models.LevelSessionStats.BoostersUsed);
            }

            TrackCollectedAndBoosters();
        }

        private int _trackedItems;
        private int _trackedBoosters;

        private void TrackCollectedAndBoosters()
        {
            var items = Models.LevelSessionStats.ItemsCollected - _trackedItems;
            var boosters = Models.LevelSessionStats.BoostersUsed - _trackedBoosters;
            _trackedItems = Models.LevelSessionStats.ItemsCollected;
            _trackedBoosters = Models.LevelSessionStats.BoostersUsed;

            if(items > 0)
            {
                _taskService.TrackProgress(Meta.Quests.Models.QuestType.CollectItems, "butterfly", items);
            }

            if(boosters > 0)
            {
                _taskService.TrackProgress(Meta.Quests.Models.QuestType.UseItems, null, boosters);
            }
        }

        //
        public void Dispose()
        {
            _gameEvents.OnGameLost -= OnLevelLost;
            _gameEvents.OnGameWon -= OnLevelWon;
            _gameEvents.OnEnterGame -= GameStart;

            if(_pauseButton != null)
            {
                _pauseButton.onClick.RemoveListener(PauseGame);
            }

            _disposable.Dispose();
        }

        public void Tick()
        {
            EnsurePauseButtonBound();
            _levelManager.InvokeUpdate();
        }
    }
}

using System;
using System.Threading;
using Configs;
using Cysharp.Threading.Tasks;
using Data;
using Itic.Scopes;
using JetBrains.Annotations;
using Models;
using Monobehaviours;
using Providers;
using R3;
using Services;
using UI;
using UnityEngine;
using VContainer.Unity;
using Views;

namespace Initializers
{
    /// <summary>
    /// Entry point of the game scene: wires the screens, takes the life for this attempt, starts the level
    /// and drives LevelManager every frame. The work itself is done by the collaborators below.
    /// </summary>
    [UsedImplicitly]
    public class GameInitializer : IInitializable, IAsyncStartable, ITickable, IDisposable
    {
        private readonly IGameEvents _gameEvents;
        private readonly GameCompleteView _gameCompleteView;
        private readonly LevelManager _levelManager;
        private readonly AdRewardService _adRewardService;
        private readonly Systems.HealthSystem _healthSystem;

        private readonly GameScreenButtons _screenButtons;
        private readonly PauseButtonBinder _pauseButton;
        private readonly AdRewardPopupPresenter _adRewardPopup;
        private readonly LevelOutcomeTracker _outcomeTracker;

        private readonly CompositeDisposable _disposable = new();

        // InternetState, IronSourceManager and AdEventModel are no longer used here; they stay in the constructor
        // so the scene keeps resolving them at the same moment as before.
        public GameInitializer(
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
            Services.Tasks.ITaskService taskService)
        {
            _gameEvents = gameEvents;
            _gameCompleteView = gameCompleteView;
            _levelManager = levelManager;
            _adRewardService = adRewardService;
            _healthSystem = healthSystem;

            _screenButtons = new GameScreenButtons(gameCompleteView, gamePauseView, gameOverView, sceneLoader, levelManager,
                adRewardService, gameConfig, healthSystem, interstitialService);
            _pauseButton = new PauseButtonBinder(levelManager, gamePauseView);
            _adRewardPopup = new AdRewardPopupPresenter(gameProvider, gameCompleteView, boostShopView, adRewardService, gameConfig, levelManager);
            _outcomeTracker = new LevelOutcomeTracker(levelManager, interstitialService, taskService);
        }

        public void Initialize()
        {
            _pauseButton.EnsureBound();

            UIButtonFeel.InstallInChildren(_gameCompleteView.transform.root);
            if (_levelManager.Level != null)
                UIButtonFeel.InstallInChildren(_levelManager.Level.transform);

            _screenButtons.Bind();

            // One subscription for the whole game scene (disposed with it), keyed by the granted reward type.
            _adRewardService.OnRewardGrantedTyped.Subscribe(_adRewardPopup.Show).AddTo(_disposable);

            if (!_healthSystem.ConsumeLifeForLevel())
                Debug.LogWarning("[GameInitializer] Level started without lives.");

            LevelSessionStats.Reset();

            _levelManager.InvokeStart();

            // The level is loaded later (PrepareGameState), so take its number from the save.
            GameAnalyticsEvents.LevelStart(JuiceFresh.Scripts.LevelProgressionHelper.CalculateLevel(Mathf.Max(1, PlayerPrefs.GetInt("OpenLevel", 1))));
        }

        public async UniTask StartAsync(CancellationToken cancellation = new CancellationToken())
        {
            _gameEvents.OnGameLost += _outcomeTracker.OnLevelLost;
            _gameEvents.OnGameWon += _outcomeTracker.OnLevelWon;

            await UniTask.Yield();
        }

        public void Tick()
        {
            _pauseButton.EnsureBound();
            _levelManager.InvokeUpdate();
        }

        public void Dispose()
        {
            _gameEvents.OnGameLost -= _outcomeTracker.OnLevelLost;
            _gameEvents.OnGameWon -= _outcomeTracker.OnLevelWon;

            _pauseButton.Dispose();
            _disposable.Dispose();
        }
    }
}

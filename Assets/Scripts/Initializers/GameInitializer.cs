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
        private Button _pauseButton;

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
            Systems.CurrencySystem.Interfaces.ICurrencyService currencyService
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
        }

        public void Initialize()
        {
            EnsurePauseButtonBound();

            UIButtonFeel.InstallInChildren(_gameCompleteView.transform.root);
            if(_levelManager.Level != null)
            {
                UIButtonFeel.InstallInChildren(_levelManager.Level.transform);
            }

            _gameCompleteView.Home.onClick.RemoveAllListeners();
            _gameCompleteView.Home.onClick.AddListener(() =>
                                                       {
                                                           var currentLevel = PlayerPrefs.GetInt("OpenLevel", 1);
                                                           PlayerPrefs.SetInt("OpenLevel", currentLevel + 1);

                                                           // TODO Gomnokod change

                                                           if(LevelManager.THIS.stars >= 3)
                                                           {
                                                               _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Cash, _gameConfig.CoinRewardForLevelPass + _gameConfig.CoinRewardFor3StarPass);
                                                           }
                                                           else
                                                           {
                                                               _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Cash, _gameConfig.CoinRewardForLevelPass);
                                                           }

                                                           _sceneLoader.LoadMenuAsync().Forget();
                                                       });

            _gameCompleteView.Next.onClick.RemoveAllListeners();
            _gameCompleteView.Next.onClick.AddListener(() =>
                                                       {
                                                           var currentLevel = PlayerPrefs.GetInt("OpenLevel", 1);

                                                           PlayerPrefs.SetInt("OpenLevel", currentLevel + 1);

                                                           if(LevelManager.THIS.stars >= 3)
                                                           {
                                                               _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Cash, _gameConfig.CoinRewardForLevelPass + _gameConfig.CoinRewardFor3StarPass);
                                                           }
                                                           else
                                                           {
                                                               _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Cash, _gameConfig.CoinRewardForLevelPass);
                                                           }

                                                           _sceneLoader.LoadGameAsync().Forget();
                                                       });

            _gameCompleteView.AdsButton.onClick.RemoveAllListeners();
            _gameCompleteView.AdsButton.onClick.AddListener(() =>
                                                            {
                                                                CompositeDisposable localDisposable = new();
                                                                _adRewardService.SetAdRewardType(AdRewardType.X2);
                                                                _adEventModel.OnRewardGranted.Subscribe(_ =>
                                                                                                        {
                                                                                                            _gameProvider.RewardPopup.Init(new RewardInfo
                                                                                                            {
                                                                                                                Icon = _gameProvider.CoinSprite, Description = "You 2x your reward"
                                                                                                            });

                                                                                                            if(LevelManager.THIS.stars >= 3)
                                                                                                            {
                                                                                                                _gameCompleteView.CoinsCollectedText.text =
                                                                                                                    ((_gameConfig.CoinRewardForLevelPass +
                                                                                                                      _gameConfig.CoinRewardFor3StarPass) *
                                                                                                                     2).ToString();
                                                                                                            }
                                                                                                            else
                                                                                                            {
                                                                                                                _gameCompleteView.CoinsCollectedText.text =
                                                                                                                    (_gameConfig.CoinRewardForLevelPass * 2).ToString();
                                                                                                            }


                                                                                                            _gameCompleteView.AdsButton.gameObject.SetActive(false);
                                                                                                        })
                                                                    .AddTo(localDisposable);

                                                                _adEventModel.OnRewardedReward.Subscribe(_ => { localDisposable.Dispose(); }).AddTo(localDisposable);
                                                                IronSourceManager.Instance.ShowRewardedAd();
                                                            });
            _gameOverView.AdsButton.onClick.AddListener(() =>
                                                        {
                                                            CompositeDisposable localDisposable = new();
                                                            _adRewardService.SetAdRewardType(AdRewardType.Continue);
                                                            _adEventModel.OnRewardGranted.Subscribe(_ =>
                                                                                                    {
                                                                                                        _gameProvider.RewardPopup.Init(new RewardInfo
                                                                                                        {
                                                                                                            Icon = _gameProvider.ExtraStepSprite,
                                                                                                            Description = "You got yourself extra 5 steps!"
                                                                                                        });
                                                                                                    })
                                                                .AddTo(localDisposable);

                                                            _adEventModel.OnRewardedReward.Subscribe(_ => { localDisposable.Dispose(); }).AddTo(localDisposable);
                                                            IronSourceManager.Instance.ShowRewardedAd();
                                                        });

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
            _gameOverView.RestartButton.onClick.AddListener(RestartGame);

            _gameOverView.Home.onClick.RemoveAllListeners();
            _gameOverView.Home.onClick.AddListener(ReturnToMainMenuAfterLose);


            _adEventModel.OnRewardGranted.Subscribe(_ =>
                                                    {
                                                        _boostShopView.Hide();
                                                        var popup = _gameProvider.RewardPopup;
                                                        popup.Init(_adRewardService.GetInfo());
                                                        popup.Show();
                                                    })
                .AddTo(_disposable);

            _levelManager.InvokeStart();
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            _sceneLoader.LoadGameAsync().Forget();
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            _sceneLoader.LoadMenuAsync().Forget();
        }

        public void ReturnToMainMenuAfterLose()
        {
            Time.timeScale = 1f;
            _currencyService?.AddCurrency(Systems.CurrencySystem.CurrencyType.Cash, _gameConfig.CoinRewardForLevelLose);
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
            _gameEvents.OnGameLost += ShowInterstitial;
            _gameEvents.OnGameWon += ShowInterstitial;
            _gameEvents.OnEnterGame += GameStart;

            await UniTask.Yield();
        }

        private void GameStart()
        {
            //     if (_internetState.HasInternet && !_internetState.HasRemoveAds)
            //         _ironSourceManager.LoadInterstitial();
            //     // _ironSourceManager.LoadBannerAd();
        }

        //
        private void ShowInterstitial()
        {
            //     if (_internetState.HasInternet && !_internetState.HasRemoveAds)
            //         _ironSourceManager.ShowInterstitial();
        }

        //
        public void Dispose()
        {
            _gameEvents.OnGameLost -= ShowInterstitial;
            _gameEvents.OnGameWon -= ShowInterstitial;
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

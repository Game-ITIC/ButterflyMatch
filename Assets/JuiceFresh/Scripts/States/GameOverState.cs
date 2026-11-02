using Cysharp.Threading.Tasks;
using Itic.Scopes;
using R3;
using Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceFresh.States
{
    public class GameOverState : GameStateBase, System.IDisposable
    {
        private readonly AdRewardService _adRewardService;
        private readonly SceneLoader _sceneLoader;
        private GameObject _menuFailedUI;
        private CompositeDisposable _disposables = new CompositeDisposable();

        public GameOverState(LevelManager levelManager, AdRewardService adRewardService, SceneLoader sceneLoader) : base(levelManager)
        {
            _adRewardService = adRewardService;
            _sceneLoader = sceneLoader;
        }

        private void OnRewardGranted(Unit unit)
        {
            // Rewarded "continue" ad: free extra moves.
            ApplyContinue();
        }

        public override void EnterState()
        {
            StopGameMusic();

            PlayGameOverSound();
            GameFeelManager.Instance?.OnLose();

            // Listen for the continue reward only while the game over screen is open.
            _disposables.Clear();
            _adRewardService.OnContinueRewardGranted
                .Subscribe(OnRewardGranted)
                .AddTo(_disposables);

            ShowFailedUI();

            levelManager.GameEventDispatcher.DispatchLose();
            LevelManager.TriggerOnLose();

            // The life was already taken at level start (HealthSystem.ConsumeLifeForLevel),
            // so continuing does not cost an extra life.
            // NOTE: the old "rewarded ad every 3rd level" was removed: rewarded ads must be opt-in.
        }

        public override void UpdateState()
        {
            // No specific update logic for game over state
        }

        public override void ExitState()
        {
            _disposables.Clear();

            // Hide the failed UI if it's still visible
            if(_menuFailedUI != null && _menuFailedUI.activeSelf)
            {
                _menuFailedUI.SetActive(false);
            }
        }

        private void StopGameMusic()
        {
            MusicBase.Instance.GetComponent<AudioSource>().Stop();
        }

        private void PlayGameOverSound()
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.gameOver[0]);
        }

        private void ShowFailedUI()
        {
            _menuFailedUI = GameObject.Find("CanvasGlobal").transform.Find("MenuFailed").gameObject;
            _menuFailedUI.SetActive(true);

            SetupFailedMenu();
        }

        private void SetupFailedMenu()
        {
            // Get the score display and set it
            TMP_Text scoreText = _menuFailedUI.transform.Find("Score")?.GetComponent<TMP_Text>();

            if(scoreText != null)
            {
                scoreText.text = LevelManager.Score.ToString();
            }

            // Set up retry button
            Button retryButton = _menuFailedUI.transform.Find("ButtonReplay")?.GetComponent<Button>();

            if(retryButton != null)
            {
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(() => OnRetryClicked());
            }

            // Set up quit button
            Button quitButton = _menuFailedUI.transform.Find("ButtonQuit")?.GetComponent<Button>();

            if(quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(() => OnQuitClicked());
            }

            // Set up any continue mechanics if applicable
            SetupContinueOption();
        }

        private void SetupContinueOption()
        {
            // If the level allows continuing after failing
            if(levelManager.FailedCost > 0)
            {
                // Set up continue button
                Button continueButton = _menuFailedUI.transform.Find("ButtonContinue")?.GetComponent<Button>();

                if(continueButton != null)
                {
                    continueButton.onClick.RemoveAllListeners();
                    continueButton.onClick.AddListener(() => OnPaidContinueClicked());

                    // Update the cost text
                    TMP_Text costText = continueButton.transform.Find("Cost")?.GetComponent<TMP_Text>();

                    if(costText != null)
                    {
                        costText.text = levelManager.FailedCost.ToString();
                    }

                    // Enable/disable based on whether player has enough gems/coins
                    bool canAfford = levelManager.CurrencyService != null
                                     && levelManager.CurrencyService.GetCurrency(Systems.CurrencySystem.CurrencyType.Cash) != null
                                     && levelManager.CurrencyService.GetCurrency(Systems.CurrencySystem.CurrencyType.Cash).Value >= levelManager.FailedCost;
                    continueButton.interactable = canAfford;
                }
            }
        }

        private void OnRetryClicked()
        {
            // Retry the current level
            levelManager.gameStatus = GameState.PrepareGame;
        }

        private void OnQuitClicked()
        {
            Time.timeScale = 1f;

            if(_sceneLoader != null)
            {
                _sceneLoader.LoadMenuAsync().Forget();
                return;
            }

            levelManager.gameStatus = GameState.Map;
        }

        private void OnPaidContinueClicked()
        {
            var currencyService = levelManager.CurrencyService;
            if(currencyService == null
               || !currencyService.SpendCurrency(Systems.CurrencySystem.CurrencyType.Cash, levelManager.FailedCost))
            {
                return;
            }

            ApplyContinue();
        }

        private void ApplyContinue()
        {
            if(_menuFailedUI == null || !_menuFailedUI.activeInHierarchy)
            {
                return;
            }

            // Add extra moves or time
            if(levelManager.limitType == LIMIT.MOVES)
            {
                levelManager.Limit += levelManager.ExtraFailedMoves;
            }
            else if(levelManager.limitType == LIMIT.TIME)
            {
                levelManager.Limit += levelManager.ExtraFailedSecs;
            }

            // Hide the failed UI
            _menuFailedUI.SetActive(false);

            // Return to playing state
            levelManager.gameStatus = GameState.Playing;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}

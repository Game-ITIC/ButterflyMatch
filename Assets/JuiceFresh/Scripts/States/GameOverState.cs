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
            _adContinuesUsed++;
            Models.LevelSessionStats.ContinuesUsed++;
            Services.GameAnalyticsEvents.ContinueUsed(levelManager.currentLevel, "ad");
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

        // Per level attempt (the scene reloads on restart): GDD 4.3 / 8.1.
        private int _adContinuesUsed;
        private int _paidContinuesUsed;

        private void SetupContinueOption()
        {
            var config = levelManager.GameConfig;
            var adLimit = config != null ? config.ContinueAdsPerAttempt : 1;

            // Rewarded continue: once per attempt, afterwards only gems.
            var adsTransform = _menuFailedUI.transform.Find("AdsButton") as RectTransform;
            var hasAdContinue = _adContinuesUsed < adLimit;
            if(adsTransform != null)
            {
                adsTransform.gameObject.SetActive(hasAdContinue);
            }

            var continueTransform = _menuFailedUI.transform.Find("ButtonContinue") as RectTransform;
            Button continueButton = continueTransform != null ? continueTransform.GetComponent<Button>() : null;

            var costs = config != null ? config.ContinueGemCosts : null;
            var hasPaidContinue = continueButton != null && costs != null && _paidContinuesUsed < costs.Length;
            if(continueButton != null)
            {
                continueButton.gameObject.SetActive(hasPaidContinue);
            }

            // Two buttons side by side; a single one is centered.
            const float sideOffset = 205f;
            if(adsTransform != null)
            {
                adsTransform.anchoredPosition = new Vector2(hasPaidContinue ? -sideOffset : 0f, adsTransform.anchoredPosition.y);
            }

            if(continueTransform != null)
            {
                continueTransform.anchoredPosition = new Vector2(hasAdContinue && adsTransform != null ? sideOffset : 0f, continueTransform.anchoredPosition.y);
            }

            if(!hasPaidContinue)
            {
                return;
            }

            var cost = costs[_paidContinuesUsed];
            continueButton.onClick.RemoveAllListeners();
            continueButton.interactable = true;

            TMP_Text costText = continueButton.transform.Find("Cost")?.GetComponent<TMP_Text>();
            var icon = continueButton.transform.Find("Icon")?.gameObject;

            // Not enough gems: the "need moves" offer (GDD 11.3, once a day) takes the button with its real price.
            if(!HasGems(cost) && CanOfferNeedMoves())
            {
                continueButton.onClick.AddListener(() => OnNeedMovesOfferClicked(continueButton).Forget());
                if(costText != null)
                {
                    costText.text = Presenters.ShopCatalogBuilder.PriceOf(levelManager.InAppConfig, Gley.EasyIAP.ShopProductNames.NeedMovesOffer);
                }

                if(icon != null) icon.SetActive(false);
                return;
            }

            if(icon != null) icon.SetActive(true);
            continueButton.onClick.AddListener(() => OnPaidContinueClicked(cost));

            if(costText != null)
            {
                costText.text = cost.ToString();
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

        private bool HasGems(int cost)
        {
            var gems = levelManager.CurrencyService?.GetCurrency(Systems.CurrencySystem.CurrencyType.Diamond);
            return gems != null && gems.Value >= cost;
        }

        private bool CanOfferNeedMoves()
        {
            return Gley.EasyIAP.API.IsInitialized()
                   && Services.InApp.ContextOffers.IsAvailableToday(Gley.EasyIAP.ShopProductNames.NeedMovesOffer)
                   && !string.IsNullOrEmpty(Presenters.ShopCatalogBuilder.PriceOf(levelManager.InAppConfig, Gley.EasyIAP.ShopProductNames.NeedMovesOffer));
        }

        private async UniTaskVoid OnNeedMovesOfferClicked(Button button)
        {
            if(Services.InApp.InAppPurchasingService.IsPurchaseInProgress)
            {
                return;
            }

            button.interactable = false;
            var bought = await Services.InApp.InAppPurchasingService.TryBuyConsumableAsync(Gley.EasyIAP.ShopProductNames.NeedMovesOffer);
            if(button != null) button.interactable = true;

            if(!bought)
            {
                if(Services.InApp.InAppPurchasingService.LastFailure != Services.InApp.PurchaseFailure.Cancelled)
                {
                    UI.Toast.Show(GameLocalization.L10n.Get("purchase_failed", "Purchase failed. Please try again."));
                }

                return;
            }

            Services.InApp.ContextOffers.MarkUsedToday(Gley.EasyIAP.ShopProductNames.NeedMovesOffer);

            // The offer granted extra-moves boosters and gems (InAppGrantService); one booster is used right now.
            var moves = levelManager.BoostersProvider?.GetBoosterModel(BoostType.ExtraMoves);
            if(moves != null && moves.HasBooster())
            {
                moves.Use();
                levelManager.BoostersProvider.Save();
            }

            Models.LevelSessionStats.ContinuesUsed++;
            Services.GameAnalyticsEvents.ContinueUsed(levelManager.currentLevel, "offer");
            ApplyContinue();
        }

        private void OnPaidContinueClicked(int cost)
        {
            var currencyService = levelManager.CurrencyService;
            if(currencyService == null)
            {
                return;
            }

            var gems = currencyService.GetCurrency(Systems.CurrencySystem.CurrencyType.Diamond);
            if(gems == null || gems.Value < cost
               || !currencyService.SpendCurrency(Systems.CurrencySystem.CurrencyType.Diamond, cost))
            {
                UI.Toast.Show(GameLocalization.L10n.Get("not_enough_gems", "Not enough gems"));
                return;
            }

            _paidContinuesUsed++;
            Models.LevelSessionStats.ContinuesUsed++;
            Services.GameAnalyticsEvents.GemsSpent("continue", cost);
            Services.GameAnalyticsEvents.ContinueUsed(levelManager.currentLevel, "gems", cost);
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

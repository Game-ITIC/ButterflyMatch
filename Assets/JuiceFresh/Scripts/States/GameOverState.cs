using Cysharp.Threading.Tasks;
using Itic.Scopes;
using R3;
using Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JuiceFresh.States
{
    /// <summary>
    /// The level was lost: shows the failed menu (score, retry, quit) and resumes the level when a continue
    /// is bought. The continue offers themselves are <see cref="ContinueOffers"/>.
    /// The life was already taken at level start (HealthSystem.ConsumeLifeForLevel), so continuing costs no extra life.
    /// </summary>
    public class GameOverState : GameStateBase, System.IDisposable
    {
        private readonly AdRewardService _adRewardService;
        private readonly SceneLoader _sceneLoader;
        private readonly ContinueOffers _continueOffers;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private GameObject _menuFailedUI;

        public GameOverState(LevelManager levelManager, AdRewardService adRewardService, SceneLoader sceneLoader) : base(levelManager)
        {
            _adRewardService = adRewardService;
            _sceneLoader = sceneLoader;
            _continueOffers = new ContinueOffers(levelManager, ApplyContinue);
        }

        public override void EnterState()
        {
            MusicBase.Instance.GetComponent<AudioSource>().Stop();
            SoundBase.Instance.PlaySound(SoundBase.Instance.gameOver[0]);
            GameFeelManager.Instance?.OnLose();

            // Listen for the continue reward only while the game over screen is open.
            _disposables.Clear();
            _adRewardService.OnContinueRewardGranted
                .Subscribe(_ => _continueOffers.OnAdContinueGranted())
                .AddTo(_disposables);

            ShowFailedUI();

            levelManager.GameEventDispatcher.DispatchLose();
            LevelManager.TriggerOnLose();
        }

        public override void UpdateState()
        {
        }

        public override void ExitState()
        {
            _disposables.Clear();

            if (_menuFailedUI != null && _menuFailedUI.activeSelf)
                _menuFailedUI.SetActive(false);
        }

        private void ShowFailedUI()
        {
            _menuFailedUI = GameObject.Find("CanvasGlobal").transform.Find("MenuFailed").gameObject;
            _menuFailedUI.SetActive(true);

            Transform menu = _menuFailedUI.transform;
            TMP_Text scoreText = menu.Find("Score")?.GetComponent<TMP_Text>();
            if (scoreText != null)
                scoreText.text = LevelManager.Score.ToString();

            Rebind(menu.Find("ButtonReplay"), OnRetryClicked);
            Rebind(menu.Find("ButtonQuit"), OnQuitClicked);

            _continueOffers.Setup(menu);
        }

        private static void Rebind(Transform buttonTransform, UnityEngine.Events.UnityAction onClick)
        {
            Button button = buttonTransform?.GetComponent<Button>();
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
        }

        private void OnRetryClicked()
        {
            levelManager.gameStatus = GameState.PrepareGame;
        }

        private void OnQuitClicked()
        {
            Time.timeScale = 1f;

            if (_sceneLoader != null)
            {
                _sceneLoader.LoadMenuAsync().Forget();
                return;
            }

            levelManager.gameStatus = GameState.Map;
        }

        /// <summary>Gives the extra moves or time of a continue and resumes the level.</summary>
        private void ApplyContinue()
        {
            if (_menuFailedUI == null || !_menuFailedUI.activeInHierarchy)
                return;

            if (levelManager.limitType == LIMIT.MOVES)
                levelManager.Limit += levelManager.ExtraFailedMoves;
            else if (levelManager.limitType == LIMIT.TIME)
                levelManager.Limit += levelManager.ExtraFailedSecs;

            _menuFailedUI.SetActive(false);
            levelManager.gameStatus = GameState.Playing;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}

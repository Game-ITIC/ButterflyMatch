#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using JuiceFresh.Scripts;
using Systems.CurrencySystem;
using UnityEngine;
using UnityEngine.UI;

namespace Utils.DebugTools
{
    /// <summary>
    /// Floating cheat button + panel for testers. Exists only in the Editor and Development Builds: the whole file
    /// is compiled out of release builds. Built from code on its own overlay canvas, so no scene or prefab holds it.
    /// The cheats themselves live in Utils/Debug/Cheats (CheatEconomy, CheatLevels, CheatIslands, CheatAds,
    /// CheatResets, CheatTools); this class owns the canvas, open/close, input blocking and the cheat list.
    /// </summary>
    [DefaultExecutionOrder(-2000)] // Press tracking must run before gameplay input reads BlocksGameInput.
    public class CheatPanel : MonoBehaviour
    {
        private const float RefreshSeconds = 0.5f;

        private static CheatPanel _instance;

        private CheatUi _ui;
        private CheatPanelView _view;
        private CheatFloatingButton _floating;
        private CheatFpsCounter _fps;
        private RectTransform _safeRoot;
        private Rect _appliedSafeArea;
        private bool _open;
        private bool _pressOnCheats;
        private float _nextRefresh;

        private CheatEconomy _economy;
        private CheatLevels _levels;
        private CheatIslands _islands;
        private CheatAds _ads;
        private CheatResets _resets;
        private CheatTools _tools;

        /// <summary>True while a press belongs to the cheat UI, so raw-Input gameplay must not react to it.</summary>
        public static bool BlocksGameInput => _instance != null && (_instance._open || _instance._pressOnCheats);

        /// <summary>The chosen game speed; it survives the game resetting timeScale to 1.</summary>
        internal float Speed { get; set; } = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ApplyPendingReset()
        {
            _instance = null;
            CheatResets.ApplyPending();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if(_instance != null)
            {
                return;
            }

            var root = new GameObject("CheatPanel", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(root);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 31000; // Under Toast (32000), so cheat results stay readable.

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            _instance = root.AddComponent<CheatPanel>();
            _instance.Build();
        }

        private void OnDestroy()
        {
            if(_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            if(Input.GetMouseButtonDown(0))
            {
                _pressOnCheats = _open
                    || RectTransformUtility.RectangleContainsScreenPoint(_floating.Rect, Input.mousePosition, null);
            }
            else if(!Input.GetMouseButton(0) && !Input.GetMouseButtonUp(0))
            {
                _pressOnCheats = false;
            }

            if(Screen.safeArea != _appliedSafeArea)
            {
                ApplySafeArea();
            }

            // The game resets timeScale to 1 on state changes; a chosen speed survives that, a pause (0) is respected.
            if(!Mathf.Approximately(Speed, 1f) && Time.timeScale > 0f && !Mathf.Approximately(Time.timeScale, Speed))
            {
                Time.timeScale = Speed;
            }

            _fps.Tick();
            _ui.Tick();

            if(_open && Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + RefreshSeconds;
                _view.RefreshStatus(Speed);
            }
        }

        private void ApplySafeArea()
        {
            _appliedSafeArea = Screen.safeArea;
            if(Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            var min = new Vector2(_appliedSafeArea.xMin / Screen.width, _appliedSafeArea.yMin / Screen.height);
            var max = new Vector2(_appliedSafeArea.xMax / Screen.width, _appliedSafeArea.yMax / Screen.height);
            _safeRoot.anchorMin = _view.SafeRoot.anchorMin = min;
            _safeRoot.anchorMax = _view.SafeRoot.anchorMax = max;
            _floating.RestorePosition();
        }

        // The canvas gets its final size only after CanvasScaler runs, and again on rotation or window resize.
        private void OnRectTransformDimensionsChange()
        {
            if(_floating != null)
            {
                _floating.RestorePosition();
            }
        }

        private void Open()
        {
            _open = true;
            _view.Dim.SetActive(true);
            _floating.Rect.gameObject.SetActive(false);
            _levels.ResetInput();
            _nextRefresh = 0f;
        }

        internal void Close()
        {
            _open = false;
            _view.Dim.SetActive(false);
            _floating.Rect.gameObject.SetActive(true);
            _ui.Disarm();
        }

        /// <summary>Shows the cheat result and refreshes the status line right away.</summary>
        internal void Done(string message)
        {
            UI.Toast.Show(message);
            _nextRefresh = 0f;
        }

        // ---------- UI ----------

        private void Build()
        {
            _ui = new CheatUi();

            _safeRoot = CheatUi.NewRect("SafeArea", transform);
            CheatUi.Stretch(_safeRoot);

            _view = new CheatPanelView(_ui, transform, Close);
            _floating = new CheatFloatingButton(_ui, _safeRoot, Open);
            _fps = new CheatFpsCounter(_safeRoot);

            _economy = new CheatEconomy(this);
            _levels = new CheatLevels(this);
            _islands = new CheatIslands(this, _levels);
            _ads = new CheatAds(this);
            _resets = new CheatResets(_levels);
            _tools = new CheatTools(this, _fps);
            BuildCheats(_view.Content);

            ApplySafeArea();
            _view.Dim.SetActive(false);
        }

        private void BuildCheats(RectTransform content)
        {
            _ui.Section(content, "Валюта");
            _ui.Row(content,
                ("+1000 кристаллов", () => _economy.AddCurrency(CurrencyType.Diamond, 1000, "+1000 кристаллов")),
                ("+100 звёзд", () => _economy.AddCurrency(CurrencyType.Star, 100, "+100 звёзд")));
            _ui.Row(content,
                ("Обнулить кристаллы", () => _economy.ZeroCurrency(CurrencyType.Diamond, "Кристаллы: 0")),
                ("Обнулить звёзды", () => _economy.ZeroCurrency(CurrencyType.Star, "Звёзды: 0")));

            _ui.Section(content, "Жизни");
            _ui.Row(content,
                ("Полные жизни", () => _economy.ChangeLives(h => h.RestoreAllLives(), "Жизни восстановлены")),
                ("-1 жизнь", () => _economy.ChangeLives(h => h.AddLives(-1), "-1 жизнь")),
                ("0 жизней", () => _economy.ChangeLives(h => h.AddLives(-h.MaxLives), "Жизни: 0")));
            _ui.Row(content, ("Безлимит на 1 час", () => _economy.ChangeLives(h => h.AddUnlimitedLives(TimeSpan.FromHours(1)), "Безлимит на 1 час")));

            _ui.Section(content, "Бустеры");
            _ui.Row(content,
                ("+10 всех", () => _economy.ChangeBoosters(b => b.Add(10), "+10 всех бустеров")),
                ("Обнулить", () => _economy.ChangeBoosters(b => b.Count.Value = 0, "Бустеры: 0")));

            _ui.Section(content, "Переход на уровень (1-" + LevelProgressionHelper.LastLevel + ", дальше цикл)");
            _levels.BuildLevelRow(_ui, content);
            _ui.Row(content,
                ("Играть этот уровень", () => _levels.GoToLevel(_levels.InputLevel)),
                ("Сделать текущим и в меню", _levels.MakeCurrentAndGoToMenu));
            _ui.Row(content, ("Открыть все уровни", _levels.OpenAllLevels));

            _ui.Section(content, "Текущий уровень");
            _ui.Row(content,
                ("Победа: 3 звезды", () => _levels.WinLevel(3)),
                ("Победа: 1 звезда", () => _levels.WinLevel(1)),
                ("Поражение", _levels.LoseLevel));
            _ui.Row(content,
                ("Лимит +5", () => _levels.AddLimit(5)),
                ("Лимит +20", () => _levels.AddLimit(20)),
                ("Заново", () => _levels.GoToLevel(CheatServices.OpenLevel)));

            _ui.Section(content, "Острова (из главного меню)");
            _ui.Row(content,
                ("Построить следующее", () => _islands.BuildIsland(false)),
                ("Достроить остров", () => _islands.BuildIsland(true)));
            _ui.ConfirmRow(content, "Сбросить острова", _islands.ResetIslands);

            _ui.Section(content, "Награды и задания");
            _ui.Row(content,
                ("Сбросить награду дня", _resets.ResetDaily),
                ("Обновить задания", _resets.ResetTasks));

            _ui.Section(content, "Реклама");
            _ui.Row(content,
                ("Показать interstitial", _ads.ShowInterstitial),
                ("Выдать Remove Ads", _ads.GrantRemoveAds));

            _ui.Section(content, "Прочее");
            _ui.Row(content,
                ("x1", () => _tools.SetSpeed(1f)),
                ("x2", () => _tools.SetSpeed(2f)),
                ("x5", () => _tools.SetSpeed(5f)),
                ("FPS", _tools.ToggleFps));
            _ui.Row(content,
                ("Следующий язык", _tools.NextLanguage),
                ("В меню", () => _levels.GoToMenu()));

            _ui.Section(content, "Сброс");
            _ui.ConfirmRow(content, "Сбросить весь прогресс и закрыть игру", CheatResets.ResetEverything);
        }
    }
}
#endif

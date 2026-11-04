#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Itic.Scopes;
using JuiceFresh.Scripts;
using Providers;
using Services;
using Services.DailyRewards;
using Services.InApp;
using Services.Tasks;
using Systems;
using Systems.CurrencySystem;
using Systems.CurrencySystem.Interfaces;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utils.Save;
using VContainer;
using VContainer.Unity;

namespace Utils.DebugTools
{
    /// <summary>
    /// Floating cheat button + panel for testers. Exists only in the Editor and Development Builds: the whole file
    /// is compiled out of release builds. Built from code on its own overlay canvas, so no scene or prefab holds it.
    /// </summary>
    [DefaultExecutionOrder(-2000)] // Press tracking must run before gameplay input reads BlocksGameInput.
    public class CheatPanel : MonoBehaviour
    {
        private const string ResetPendingKey = "cheat-reset-pending";
        private const string PosXKey = "cheat-panel-pos-x";
        private const string PosYKey = "cheat-panel-pos-y";
        private const string MenuOnly = "Открой главное меню";
        private const string LevelOnly = "Только во время уровня";
        private const float ConfirmSeconds = 3f;
        private const float RefreshSeconds = 0.5f;
        private const float RowHeight = 104f;
        private const float CardMargin = 20f;

        private static readonly Color CardColor = new(0.1f, 0.09f, 0.16f, 0.98f);
        private static readonly Color ButtonColor = new(0.27f, 0.25f, 0.42f, 1f);
        private static readonly Color FieldColor = new(0.05f, 0.05f, 0.09f, 1f);
        private static readonly Color DangerColor = new(0.62f, 0.2f, 0.22f, 1f);
        private static readonly Color FloatingColor = new(0.78f, 0.2f, 0.45f, 0.8f);
        private static readonly Color TitleColor = new(1f, 0.83f, 0.35f, 1f);

        private static CheatPanel _instance;

        private Sprite _rounded;
        private RectTransform _safeRoot;
        private RectTransform _panelSafeRoot;
        private RectTransform _floating;
        private GameObject _dim;
        private TextMeshProUGUI _status;
        private TextMeshProUGUI _fpsLabel;
        private TMP_InputField _levelInput;
        private Rect _appliedSafeArea;
        private bool _open;
        private bool _pressOnCheats;
        private float _speed = 1f;
        private float _nextRefresh;
        private float _fpsTime;
        private int _fpsFrames;
        private TextMeshProUGUI _armedLabel;
        private string _armedText;
        private float _armedUntil;

        /// <summary>True while a press belongs to the cheat UI, so raw-Input gameplay must not react to it.</summary>
        public static bool BlocksGameInput => _instance != null && (_instance._open || _instance._pressOnCheats);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ApplyPendingReset()
        {
            _instance = null;

            // Done on the next launch: running services would write their in-memory values back on quit.
            if(PlayerPrefs.GetInt(ResetPendingKey, 0) == 1)
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
            }
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
                    || RectTransformUtility.RectangleContainsScreenPoint(_floating, Input.mousePosition, null);
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
            if(!Mathf.Approximately(_speed, 1f) && Time.timeScale > 0f && !Mathf.Approximately(Time.timeScale, _speed))
            {
                Time.timeScale = _speed;
            }

            UpdateFps();

            if(_armedLabel != null && Time.unscaledTime > _armedUntil)
            {
                Disarm();
            }

            if(_open && Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + RefreshSeconds;
                RefreshStatus();
            }
        }

        private void UpdateFps()
        {
            if(!_fpsLabel.gameObject.activeSelf)
            {
                return;
            }

            _fpsFrames++;
            _fpsTime += Time.unscaledDeltaTime;
            if(_fpsTime >= RefreshSeconds)
            {
                _fpsLabel.text = "FPS " + Mathf.RoundToInt(_fpsFrames / _fpsTime);
                _fpsFrames = 0;
                _fpsTime = 0f;
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
            _safeRoot.anchorMin = _panelSafeRoot.anchorMin = min;
            _safeRoot.anchorMax = _panelSafeRoot.anchorMax = max;
            RestoreFloatingPosition();
        }

        // The canvas gets its final size only after CanvasScaler runs, and again on rotation or window resize.
        private void OnRectTransformDimensionsChange()
        {
            if(_floating != null)
            {
                RestoreFloatingPosition();
            }
        }

        private void Open()
        {
            _open = true;
            _dim.SetActive(true);
            _floating.gameObject.SetActive(false);
            _levelInput.SetTextWithoutNotify(OpenLevel.ToString());
            _nextRefresh = 0f;
        }

        private void Close()
        {
            _open = false;
            _dim.SetActive(false);
            _floating.gameObject.SetActive(true);
            Disarm();
        }

        // ---------- Cheats ----------

        private static int OpenLevel => Mathf.Max(1, PlayerPrefs.GetInt("OpenLevel", 1));

        private int InputLevel
        {
            get => int.TryParse(_levelInput.text, out var value) ? Mathf.Max(1, value) : OpenLevel;
            set => _levelInput.SetTextWithoutNotify(Mathf.Max(1, value).ToString());
        }

        private void RefreshStatus()
        {
            var currency = Find<ICurrencyService>();
            var health = Find<HealthSystem>();
            var gems = currency?.GetCurrency(CurrencyType.Diamond)?.Value ?? 0f;
            var stars = currency?.GetCurrency(CurrencyType.Star)?.Value ?? 0f;
            var lives = health == null
                ? "-"
                : health.CurrentLives.Value + "/" + health.MaxLives + (health.HasUnlimitedLives ? " +безлимит" : string.Empty);
            var level = LevelManager.THIS;
            var state = level != null ? "  |  " + level.gameStatus + ", лимит " + level.Limit : string.Empty;

            _status.text = "Открыт уровень " + OpenLevel + "  |  Жизни " + lives
                           + "\nКристаллы " + Mathf.FloorToInt(gems) + "  |  Звёзды " + Mathf.FloorToInt(stars)
                           + "  |  x" + _speed
                           + "\nСцена " + SceneManager.GetActiveScene().name + state
                           + "  |  v" + Application.version;
        }

        private void AddCurrency(CurrencyType type, int amount, string message)
        {
            var currency = Need<ICurrencyService>();
            if(currency != null)
            {
                currency.AddCurrency(type, amount);
                Done(message);
            }
        }

        private void ZeroCurrency(CurrencyType type, string message)
        {
            var currency = Need<ICurrencyService>();
            var value = currency?.GetCurrency(type)?.Value ?? 0f;
            if(currency != null)
            {
                currency.SpendCurrency(type, value);
                Done(message);
            }
        }

        private void ChangeLives(Action<HealthSystem> change, string message)
        {
            var health = Need<HealthSystem>();
            if(health != null)
            {
                change(health);
                Done(message);
            }
        }

        private void ChangeBoosters(Action<Models.BoosterModel> change, string message)
        {
            var boosters = Need<BoostersProvider>();
            if(boosters == null)
            {
                return;
            }

            foreach(var model in boosters.BoostersModels)
            {
                change(model);
            }

            boosters.Save();
            Done(message);
        }

        private void GoToLevel(int level)
        {
            var loader = NeedIdleLoader();
            if(loader == null)
            {
                return;
            }

            PlayerPrefs.SetInt("OpenLevel", Mathf.Max(1, level));
            PlayerPrefs.Save();
            var health = Find<HealthSystem>();
            if(health != null && !health.CanPlay)
            {
                health.AddLives(1);
            }

            Time.timeScale = _speed;
            Close();
            loader.LoadGameAsync().Forget();
        }

        private void GoToMenu(string message = null)
        {
            var loader = NeedIdleLoader();
            if(loader == null)
            {
                return;
            }

            Time.timeScale = _speed;
            Close();
            loader.LoadMenuAsync().Forget();
            if(message != null)
            {
                Toast.Show(message);
            }
        }

        private SceneLoader NeedIdleLoader()
        {
            var loader = Need<SceneLoader>();
            if(loader != null && loader.IsLoading)
            {
                Toast.Show("Сцена ещё загружается");
                return null;
            }

            return loader;
        }

        private LevelManager NeedPlayingLevel()
        {
            var level = LevelManager.THIS;
            if(level == null || level.gameStatus != GameState.Playing)
            {
                Toast.Show(LevelOnly);
                return null;
            }

            return level;
        }

        private void WinLevel(int stars)
        {
            var level = NeedPlayingLevel();
            if(level == null)
            {
                return;
            }

            if(stars >= 3)
            {
                LevelManager.Score = Mathf.Max(LevelManager.Score, level.star3);
            }
            else
            {
                // Stars only ever go up with the score, and leftover moves turn into score on the win screen.
                LevelManager.Score = level.star1;
                level.Limit = 0;
            }

            level.stars = stars;
            Close();
            level.gameStatus = GameState.PreWinAnimations;
        }

        private void LoseLevel()
        {
            var level = NeedPlayingLevel();
            if(level != null)
            {
                level.Limit = 0;
                Close();
                level.gameStatus = GameState.GameOver;
            }
        }

        private void AddLimit(int amount)
        {
            var level = NeedPlayingLevel();
            if(level != null)
            {
                level.Limit += amount;
                Done("Лимит уровня +" + amount);
            }
        }

        private void BuildIsland(bool wholeIsland)
        {
            var service = Find<IslandBuildService>();
            var currency = Need<ICurrencyService>();
            if(service == null)
            {
                Toast.Show(MenuOnly);
                return;
            }

            if(currency == null)
            {
                return;
            }

            var island = service.CurrentProgressIsland();
            if(island == null || service.IsCompleted(island))
            {
                Toast.Show("Строить больше нечего");
                return;
            }

            if(service.ActiveIsland != island)
            {
                service.SelectIsland(island);
            }

            var built = 0;
            while(TryBuildNextSlot(service, currency, island))
            {
                built++;
                if(!wholeIsland)
                {
                    break;
                }
            }

            Close();
            Toast.Show(built > 0 ? island.DisplayName + ": построено " + built : "Не удалось построить");
        }

        private static bool TryBuildNextSlot(IslandBuildService service, ICurrencyService currency, Configs.IslandSO island)
        {
            var slots = island.Slots;
            for(var i = 0; i < slots.Count; i++)
            {
                if(!service.CanSelect(island, slots[i]))
                {
                    continue;
                }

                // The slot is paid by the cheat, so the player's stars stay as they were.
                currency.AddCurrency(CurrencyType.Star, slots[i].StarCost);
                if(service.TryBuild(slots[i]))
                {
                    return true;
                }

                currency.SpendCurrency(CurrencyType.Star, slots[i].StarCost);
            }

            return false;
        }

        private void ResetIslands()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsKeys.IslandBuildProgress);
            PlayerPrefs.Save();
            GoToMenu("Острова сброшены");
        }

        private void ResetDaily()
        {
            PlayerPrefs.DeleteKey(DailyRewardService.LastClaimDateKey);
            PlayerPrefs.Save();
            GoToMenu("Ежедневную награду можно забрать снова");
        }

        private void ResetTasks()
        {
            new TaskStorage().Clear();
            GoToMenu("Задания обновлены");
        }

        private void ShowInterstitial()
        {
            if(IronSourceManager.Instance == null)
            {
                Toast.Show("Реклама ещё не готова");
                return;
            }

            Close();
            Toast.Show(IronSourceManager.Instance.ShowInterstitial() ? "Показ рекламы" : "Реклама не готова, запрошена загрузка");
        }

        private void GrantRemoveAds()
        {
            var grants = Need<InAppGrantService>();
            if(grants != null)
            {
                grants.SetRemoveAds();
                Done("Реклама отключена, вернуть можно полным сбросом");
            }
        }

        private void SetSpeed(float speed)
        {
            _speed = speed;
            if(Time.timeScale > 0f)
            {
                Time.timeScale = speed;
            }

            Done("Скорость x" + speed);
        }

        private void ToggleFps()
        {
            _fpsLabel.gameObject.SetActive(!_fpsLabel.gameObject.activeSelf);
            _fpsLabel.text = "FPS";
            _fpsFrames = 0;
            _fpsTime = 0f;
        }

        private void NextLanguage()
        {
            var locales = LocalizationSettings.AvailableLocales?.Locales;
            if(locales == null || locales.Count == 0)
            {
                Toast.Show("Локали ещё не загружены");
                return;
            }

            var next = locales[(locales.IndexOf(LocalizationSettings.SelectedLocale) + 1) % locales.Count];
            LocalizationSettings.SelectedLocale = next;
            Toast.Show(next.Identifier.Code);
        }

        private static void ResetEverything()
        {
            PlayerPrefs.SetInt(ResetPendingKey, 1);
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Done(string message)
        {
            Toast.Show(message);
            _nextRefresh = 0f;
        }

        private static T Need<T>() where T : class
        {
            var service = Find<T>();
            if(service == null)
            {
                Toast.Show(typeof(T).Name + " пока недоступен");
            }

            return service;
        }

        // Services live in per-scene VContainer scopes; only called from clicks and the 2 Hz status line.
        private static T Find<T>() where T : class
        {
            foreach(var scope in FindObjectsByType<LifetimeScope>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if(scope == null || scope.Container == null)
                {
                    continue;
                }

                try
                {
                    if(scope.Container.Resolve(typeof(T)) is T service)
                    {
                        return service;
                    }
                }
                catch(VContainerException)
                {
                    // Not registered in this scope, try the next one.
                }
            }

            return null;
        }

        // ---------- UI ----------

        private void Build()
        {
            _rounded = MakeRoundedSprite(32);

            _safeRoot = NewRect("SafeArea", transform);
            Stretch(_safeRoot);

            BuildPanel();
            BuildFloatingButton();
            BuildFpsLabel();

            ApplySafeArea();
            _dim.SetActive(false);
        }

        private void BuildFloatingButton()
        {
            _floating = NewRect("Floating Button", _safeRoot);
            _floating.anchorMin = _floating.anchorMax = new Vector2(0.5f, 0.5f);
            _floating.sizeDelta = new Vector2(132f, 132f);

            var image = AddImage(_floating, FloatingColor);
            image.pixelsPerUnitMultiplier = 0.5f; // 32 px corners at 2x: a full circle at this size.

            var label = AddText(_floating, "CHEAT", 30f, Color.white);
            label.fontStyle = FontStyles.Bold;

            var floating = _floating.gameObject.AddComponent<FloatingButton>();
            floating.Clicked = Open;
            floating.Dropped = SaveFloatingPosition;
        }

        private void RestoreFloatingPosition()
        {
            var size = _safeRoot.rect.size;
            var position = new Vector2(
                (PlayerPrefs.GetFloat(PosXKey, 1f) - 0.5f) * size.x,
                (PlayerPrefs.GetFloat(PosYKey, 0.68f) - 0.5f) * size.y);
            _floating.DOKill();
            _floating.anchoredPosition = ClampToSafeArea(position, _floating, _safeRoot);
        }

        private void SaveFloatingPosition(Vector2 position)
        {
            var size = _safeRoot.rect.size;
            if(size.x > 0f && size.y > 0f)
            {
                PlayerPrefs.SetFloat(PosXKey, position.x / size.x + 0.5f);
                PlayerPrefs.SetFloat(PosYKey, position.y / size.y + 0.5f);
                PlayerPrefs.Save();
            }
        }

        private void BuildFpsLabel()
        {
            var rect = NewRect("FPS", _safeRoot);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(12f, -8f);
            rect.sizeDelta = new Vector2(240f, 56f);
            _fpsLabel = AddText(rect, "FPS", 36f, Color.green);
            _fpsLabel.alignment = TextAlignmentOptions.Left;
            rect.gameObject.SetActive(false);
        }

        private void BuildPanel()
        {
            var dim = NewRect("Dim", transform);
            Stretch(dim);
            dim.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            _dim = dim.gameObject;

            _panelSafeRoot = NewRect("SafeArea", dim);
            Stretch(_panelSafeRoot);

            var card = NewRect("Card", _panelSafeRoot);
            Stretch(card);
            card.offsetMin = new Vector2(CardMargin, CardMargin);
            card.offsetMax = new Vector2(-CardMargin, -CardMargin);
            AddImage(card, CardColor);

            var title = NewRect("Title", card);
            title.anchorMin = new Vector2(0f, 1f);
            title.anchorMax = new Vector2(1f, 1f);
            title.pivot = new Vector2(0.5f, 1f);
            title.offsetMin = new Vector2(28f, -96f);
            title.offsetMax = new Vector2(-120f, -8f);
            var titleText = AddText(title, "Читы для тестирования", 44f, TitleColor);
            titleText.alignment = TextAlignmentOptions.Left;

            var close = NewRect("Close", card);
            close.anchorMin = close.anchorMax = close.pivot = new Vector2(1f, 1f);
            close.anchoredPosition = new Vector2(-14f, -10f);
            close.sizeDelta = new Vector2(92f, 88f);
            AddButton(close, "X", DangerColor, Close);

            var status = NewRect("Status", card);
            status.anchorMin = new Vector2(0f, 1f);
            status.anchorMax = new Vector2(1f, 1f);
            status.pivot = new Vector2(0.5f, 1f);
            status.offsetMin = new Vector2(28f, -240f);
            status.offsetMax = new Vector2(-28f, -100f);
            _status = AddText(status, string.Empty, 32f, Color.white);
            _status.alignment = TextAlignmentOptions.Left;

            var content = BuildScroll(card);
            BuildCheats(content);
        }

        private static RectTransform BuildScroll(RectTransform card)
        {
            var viewport = NewRect("Viewport", card);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(20f, 20f);
            viewport.offsetMax = new Vector2(-20f, -248f);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = Color.clear;

            var content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = card.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            return content;
        }

        private void BuildCheats(RectTransform content)
        {
            Section(content, "Валюта");
            Row(content,
                ("+1000 кристаллов", () => AddCurrency(CurrencyType.Diamond, 1000, "+1000 кристаллов")),
                ("+100 звёзд", () => AddCurrency(CurrencyType.Star, 100, "+100 звёзд")));
            Row(content,
                ("Обнулить кристаллы", () => ZeroCurrency(CurrencyType.Diamond, "Кристаллы: 0")),
                ("Обнулить звёзды", () => ZeroCurrency(CurrencyType.Star, "Звёзды: 0")));

            Section(content, "Жизни");
            Row(content,
                ("Полные жизни", () => ChangeLives(h => h.RestoreAllLives(), "Жизни восстановлены")),
                ("-1 жизнь", () => ChangeLives(h => h.AddLives(-1), "-1 жизнь")),
                ("0 жизней", () => ChangeLives(h => h.AddLives(-h.MaxLives), "Жизни: 0")));
            Row(content, ("Безлимит на 1 час", () => ChangeLives(h => h.AddUnlimitedLives(TimeSpan.FromHours(1)), "Безлимит на 1 час")));

            Section(content, "Бустеры");
            Row(content,
                ("+10 всех", () => ChangeBoosters(b => b.Add(10), "+10 всех бустеров")),
                ("Обнулить", () => ChangeBoosters(b => b.Count.Value = 0, "Бустеры: 0")));

            Section(content, "Переход на уровень (1-" + LevelProgressionHelper.LastLevel + ", дальше цикл)");
            BuildLevelRow(content);
            Row(content,
                ("Играть этот уровень", () => GoToLevel(InputLevel)),
                ("Сделать текущим и в меню", () =>
                {
                    PlayerPrefs.SetInt("OpenLevel", InputLevel);
                    PlayerPrefs.Save();
                    GoToMenu("Текущий уровень " + InputLevel);
                }));

            Section(content, "Текущий уровень");
            Row(content,
                ("Победа: 3 звезды", () => WinLevel(3)),
                ("Победа: 1 звезда", () => WinLevel(1)),
                ("Поражение", LoseLevel));
            Row(content,
                ("Лимит +5", () => AddLimit(5)),
                ("Лимит +20", () => AddLimit(20)),
                ("Заново", () => GoToLevel(OpenLevel)));

            Section(content, "Острова (из главного меню)");
            Row(content,
                ("Построить следующее", () => BuildIsland(false)),
                ("Достроить остров", () => BuildIsland(true)));
            ConfirmRow(content, "Сбросить острова", ResetIslands);

            Section(content, "Награды и задания");
            Row(content,
                ("Сбросить награду дня", ResetDaily),
                ("Обновить задания", ResetTasks));

            Section(content, "Реклама");
            Row(content,
                ("Показать interstitial", ShowInterstitial),
                ("Выдать Remove Ads", GrantRemoveAds));

            Section(content, "Прочее");
            Row(content,
                ("x1", () => SetSpeed(1f)),
                ("x2", () => SetSpeed(2f)),
                ("x5", () => SetSpeed(5f)),
                ("FPS", ToggleFps));
            Row(content,
                ("Следующий язык", NextLanguage),
                ("В меню", () => GoToMenu()));

            Section(content, "Сброс");
            ConfirmRow(content, "Сбросить весь прогресс и закрыть игру", ResetEverything);
        }

        private void BuildLevelRow(RectTransform content)
        {
            var row = NewRowRect(content);
            AddRowButton(row, "-10", ButtonColor, () => InputLevel -= 10);
            AddRowButton(row, "-1", ButtonColor, () => InputLevel -= 1);

            var field = NewRect("Level Input", row);
            field.gameObject.SetActive(false); // TMP_InputField must get its references before its first OnEnable.
            field.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.5f;
            var background = AddImage(field, FieldColor);

            var area = NewRect("Text Area", field);
            Stretch(area);
            area.gameObject.AddComponent<RectMask2D>();

            var textRect = NewRect("Text", area);
            Stretch(textRect);
            var text = AddText(textRect, string.Empty, 44f, Color.white);

            _levelInput = field.gameObject.AddComponent<TMP_InputField>();
            _levelInput.targetGraphic = background;
            _levelInput.textViewport = area;
            _levelInput.textComponent = text;
            _levelInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            _levelInput.characterLimit = 5;
            _levelInput.pointSize = 44f;
            field.gameObject.SetActive(true);

            AddRowButton(row, "+1", ButtonColor, () => InputLevel += 1);
            AddRowButton(row, "+10", ButtonColor, () => InputLevel += 10);
        }

        private static void Section(RectTransform content, string title)
        {
            var rect = NewRect("Section " + title, content);
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 66f;
            var text = AddText(rect, title, 34f, TitleColor);
            text.alignment = TextAlignmentOptions.BottomLeft;
        }

        private void Row(RectTransform content, params (string label, Action action)[] buttons)
        {
            var row = NewRowRect(content);
            foreach(var (label, action) in buttons)
            {
                AddRowButton(row, label, ButtonColor, action);
            }
        }

        /// <summary>A destructive action: the first tap only asks for a second one within a few seconds.</summary>
        private void ConfirmRow(RectTransform content, string label, Action action)
        {
            var row = NewRowRect(content);
            TextMeshProUGUI text = null;
            text = AddRowButton(row, label, DangerColor, () =>
            {
                if(_armedLabel == text)
                {
                    Disarm();
                    action();
                    return;
                }

                Disarm();
                _armedLabel = text;
                _armedText = label;
                _armedUntil = Time.unscaledTime + ConfirmSeconds;
                text.text = "Точно? Нажми ещё раз";
            });
        }

        private void Disarm()
        {
            if(_armedLabel != null)
            {
                _armedLabel.text = _armedText;
            }

            _armedLabel = null;
        }

        private static RectTransform NewRowRect(RectTransform content)
        {
            var row = NewRect("Row", content);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = RowHeight;
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return row;
        }

        private TextMeshProUGUI AddRowButton(RectTransform row, string label, Color color, Action action)
        {
            var rect = NewRect("Button " + label, row);
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;
            element.preferredWidth = 0f;
            return AddButton(rect, label, color, action);
        }

        private TextMeshProUGUI AddButton(RectTransform rect, string label, Color color, Action action)
        {
            var image = AddImage(rect, color);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => Run(action));

            var text = AddText(rect, label, 34f, Color.white);
            text.enableAutoSizing = true;
            text.fontSizeMin = 20f;
            text.fontSizeMax = 34f;
            text.margin = new Vector4(8f, 4f, 8f, 4f);
            return text;
        }

        // A broken cheat must tell the tester instead of failing silently.
        private static void Run(Action action)
        {
            try
            {
                action();
            }
            catch(Exception e)
            {
                UnityEngine.Debug.LogException(e);
                Toast.Show("Ошибка чита: " + e.Message);
            }
        }

        private Image AddImage(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = _rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI AddText(RectTransform parent, string value, float size, Color color)
        {
            var host = parent;
            if(parent.GetComponent<Graphic>() != null)
            {
                host = NewRect("Label", parent);
                Stretch(host);
            }

            var text = host.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Vector2 ClampToSafeArea(Vector2 position, RectTransform child, RectTransform parent)
        {
            var half = (parent.rect.size - child.rect.size) * 0.5f;
            return new Vector2(
                Mathf.Clamp(position.x, -Mathf.Max(0f, half.x), Mathf.Max(0f, half.x)),
                Mathf.Clamp(position.y, -Mathf.Max(0f, half.y), Mathf.Max(0f, half.y)));
        }

        private static Sprite MakeRoundedSprite(int radius)
        {
            var size = radius * 2 + 2;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            var center = size * 0.5f;
            for(var y = 0; y < size; y++)
            {
                for(var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                    var alpha = (byte)(Mathf.Clamp01(radius - distance + 0.5f) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        /// <summary>Tap opens the panel; a drag moves the button, which then sticks to the nearest screen side.</summary>
        private class FloatingButton : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            private const float EdgeSnapSeconds = 0.15f;

            public Action Clicked;
            public Action<Vector2> Dropped;

            private RectTransform Rect => (RectTransform)transform;
            private RectTransform Parent => (RectTransform)transform.parent;

            public void OnPointerClick(PointerEventData eventData)
            {
                if(!eventData.dragging)
                {
                    Clicked?.Invoke();
                }
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                Rect.DOKill();
            }

            public void OnDrag(PointerEventData eventData)
            {
                if(RectTransformUtility.ScreenPointToLocalPointInRectangle(Parent, eventData.position, null, out var local))
                {
                    Rect.anchoredPosition = ClampToSafeArea(local, Rect, Parent);
                }
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                var position = Rect.anchoredPosition;
                var halfX = Mathf.Max(0f, (Parent.rect.width - Rect.rect.width) * 0.5f);
                position.x = position.x >= 0f ? halfX : -halfX;
                Rect.DOAnchorPos(position, EdgeSnapSeconds).SetUpdate(true).SetLink(gameObject);
                Dropped?.Invoke(position);
            }
        }
    }
}
#endif

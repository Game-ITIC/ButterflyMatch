using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Configs;
using Cysharp.Threading.Tasks;
using JuiceFresh;
using JuiceFresh.Scripts;
using JuiceFresh.States;
using Models;
using Monobehaviours;
using Providers;
using Services;
using Systems;
using UI;
using UnityEngine.UI;
using VContainer;
using Views;

/// <summary>
/// The level scene's hub: holds the level's serialized setup and runtime state, and hands every job
/// to its own service (loading, board, bombs, timer, boosts, outline, states).
/// Legacy code reaches it through <see cref="THIS"/>; new code should take the service it needs instead.
/// </summary>
[RequireComponent(typeof(GridOutlineGenerator), typeof(GridPerimeterRenderer))]
public class LevelManager : MonoBehaviour, ILevelManagerActions
{
    #region Static References

    public static LevelManager THIS;
    public static LevelManager Instance;

    // Score of the current level.
    public static int Score;

    #endregion

    #region Prefabs and Objects

    public List<CollectedIngredients> collectedIngredients = new List<CollectedIngredients>();

    public GameObject itemPrefab;
    public GameObject squarePrefab;

    // Obstacle prefabs, see SquareTypes.
    public GameObject blockPrefab;
    public GameObject wireBlockPrefab;
    public GameObject solidBlockPrefab;
    public GameObject undesroyableBlockPrefab;
    public GameObject thrivingBlockPrefab;

    public LifeShop lifeShop;

    public Transform GameField;

    public Vector3 GameFieldTargetLocalPosition { get; private set; }

    public GameObject LevelsMap;

    // Level UI root.
    public GameObject Level;

    // UI objects for the targets.
    public GameObject ingrObject;
    public GameObject blocksObject;
    public GameObject scoreTargetObject;
    public GameObject cageTargetObject;
    public GameObject bombTargetObject;

    public Camera gameCamera;

    // Line through the chain the player drags.
    public Line line;

    [SerializeField] private GridOutlineGenerator gridOutlineGenerator;

    #endregion

    #region Sprites

    public Sprite squareSprite;
    public Sprite squareSprite1;
    public Sprite[] ingrediendSprites;
    public Sprite doubleBlock;
    public Sprite doubleSolidBlock;

    #endregion

    #region Level Configuration

    public bool enableInApps;

    // Moves or time.
    public LIMIT limitType;

    // Moves or seconds left.
    public int Limit = 30;

    public int currentLevel = 1;

    // Cost of continuing after a fail.
    public int FailedCost;
    public int ExtraFailedMoves = 5;
    public int ExtraFailedSecs = 30;

    public int maxRows = 8;
    public int maxCols = 7;

    public float squareWidth = 1.2f;
    public float squareHeight = 1.2f;

    public Vector2 firstSquarePosition;

    public int colorLimit;

    public string[] targetDiscriptions;

    public bool FacebookEnable;
    public bool PlayFab;

    // Every N pieces of a chain give an extra item (flower).
    public float extraItemEvery = 6;

    // Moves a bomb starts with.
    public int bombTimer;

    public int NumIngredients = 4;

    #endregion

    #region Game State and Targets

    public Target target;

    // Before InvokeStart there is no state machine yet; the status lives here.
    private GameState _statusBeforeStart;

    public int scoreForItem = 10;
    public int scoreForBlock = 100;
    public int scoreForWireBlock = 100;
    public int scoreForSolidBlock = 100;
    public int scoreForThrivingBlock = 100;

    // Colours of the score popups.
    public Color[] scoresColors;

    // Stars earned on the current level and the score needed for each.
    public int stars;
    public int star1;
    public int star2;
    public int star3;

    public CollectStars starsTargetCount;

    public int RequiredStars => Mathf.Clamp((int)starsTargetCount, 1, 3);

    public int targetBlocks;
    public int TargetCages;
    public int TargetBombs;

    #endregion

    #region Items and Squares

    public Square[] squaresArray;

    public List<List<Item>> combinedItems = new List<List<Item>>();

    public Item lastDraggedItem;

    // The chain the player selected; the cascade destroys it.
    public List<Item> destroyAnyway = new List<Item>();

    SquareBlocks[] levelSquaresFile = new SquareBlocks[81];

    internal SquareBlocks[] LevelSquaresFile => levelSquaresFile;

    public List<Item> highlightedItems;

    public int[] ingrCountTarget = new int[4];

    public List<CollectedIngredients> ingrTarget = new List<CollectedIngredients>();

    public CollectItems[] collectItems = new CollectItems[6];

    public List<GameObject> listIngredientsGUIObjects = new List<GameObject>();

    // Striped pieces in the current chain.
    public List<ItemsTypes> gatheredTypes = new List<ItemsTypes>();

    public List<Vector3> startPosFlowers = new List<Vector3>();

    #endregion

    #region Boosts

    public int BoostColorfullBomb;
    public int BoostPackage;
    public int BoostStriped;

    // Shown as the active boost when none is armed.
    public BoostIcon emptyBoostIcon;

    public BoostIcon activatedBoost;

    public BoostIcon[] InGameBoosts;

    // A boost whose effect animation is still playing.
    public BoostIcon waitingBoost;

    #endregion

    #region Game Data

    public List<GemProduct> gemsProducts = new List<GemProduct>();

    // Bomb timers saved for a board regeneration.
    public List<int> bombTimers = new List<int>();

    public int passLevelCounter;

    public int moveID;

    public int lastRandColor;

    public int selectedColor;

    public int nextExtraItems;

    private int cageHP;

    internal int CageHp => cageHP;

    public bool itemsHided;

    public bool onlyFalling;

    public bool levelLoaded;

    public Hashtable countedSquares;

    public bool testByPlay;

    public int extraCageAddItem;

    // Bombs to defuse on a bomb level.
    public int bombsCollect;

    public bool thrivingBlockDestroyed;

    // Blocks input while the board is busy.
    public bool dragBlocked;

    #endregion

    #region Components and Services

    private CollectedTargetFlyController _collectedTargetFlyController;
    private ScorePopupTweenSpawner _scorePopupTweenSpawner;
    private LevelEffectsController _levelEffectsController;
    private GridPerimeterRenderer _gridPerimeterRenderer;

    private BoardMechanicsService _boardMechanicsService;
    private ScoreTrackerService _scoreTrackerService;
    private BoardQueryService _boardQueryService;
    private BoardFactoryService _boardFactoryService;
    private WinLoseEvaluator _winLoseEvaluator;
    private CollectableTargetUIBuilder _collectableTargetUIBuilder;
    private BoardOutlineService _outline;
    private ChainLineDrawer _chainLine;
    private GameStateMachine _stateMachine;

    // Created on first use: some UI touches these before InvokeStart.
    private LevelLoaderService _levelLoader;
    private BoardItemsService _boardItems;
    private BombService _bombs;
    private LevelTimer _timer;
    private InGameBoostController _boosts;

    public BoardMechanicsService BoardMechanics => _boardMechanicsService;

    public LevelLoaderService LevelLoader => _levelLoader ??= new LevelLoaderService();

    public BoardItemsService BoardItems => _boardItems ??= new BoardItemsService(this);

    public BombService Bombs => _bombs ??= new BombService(this, () =>
        _collectedTargetFlyController != null ? _collectedTargetFlyController.PendingBombs : 0);

    public LevelTimer Timer => _timer ??= new LevelTimer(this);

    public InGameBoostController Boosts => _boosts ??= new InGameBoostController(this);

    /// <summary>The Playing state (input, chains, moves); null before InvokeStart.</summary>
    public PlayingState Playing => _stateMachine?.Get<PlayingState>();

    public ScorePopupTweenSpawner ScorePopupTweenSpawner
    {
        get
        {
            if (_scorePopupTweenSpawner == null)
                _scorePopupTweenSpawner = GetComponent<ScorePopupTweenSpawner>();

            return _scorePopupTweenSpawner;
        }
    }

    public LevelEffectsController LevelEffectsController
    {
        get
        {
            if (_levelEffectsController == null)
                _levelEffectsController = GetComponent<LevelEffectsController>();

            return _levelEffectsController;
        }
    }

    // A collected ingredient / defused bomb is still flying to its counter (it decides win or lose on landing).
    public bool ingredientFly => _collectedTargetFlyController != null && _collectedTargetFlyController.IsFlying;

    #endregion

    #region Properties

    public bool DragBlocked
    {
        get => dragBlocked;
        set => dragBlocked = value;
    }

    public int TargetBlocks
    {
        get => targetBlocks;
        set => targetBlocks = value;
    }

    public GameState gameStatus
    {
        get => _stateMachine != null ? _stateMachine.Status : _statusBeforeStart;
        set
        {
            _outline?.ApplyGameState(value);

            if (_stateMachine != null)
            {
                _stateMachine.TransitionTo(value);
                return;
            }

            StopLevelCoroutines();
            _statusBeforeStart = value;
            StatelessGameStates.Enter(value);
        }
    }

    [Inject] public BoostersProvider BoostersProvider;
    [Inject] public Systems.CurrencySystem.Interfaces.ICurrencyService CurrencyService;
    [Inject] public GameEventDispatcher GameEventDispatcher;
    [Inject] public AdRewardService AdRewardService;
    [Inject] public Itic.Scopes.SceneLoader SceneLoader;
    [Inject] public IronSourceManager IronSourceManager;
    [Inject] public GameProvider GameProvider;
    [Inject] public GameCompleteView GameCompleteView;
    [Inject] public GameConfig GameConfig;
    [Inject] public Configs.InAppConfig InAppConfig;

    [Inject] public HealthSystem HealthSystem;

    // The armed boost (the empty boost icon when none). Arming +5 moves applies it at once.
    public BoostIcon ActivatedBoost
    {
        get => Boosts.Active;
        set => Boosts.SetActive(value);
    }

    #endregion

    #region Events

    public delegate void GameStateEvents();

    public static event GameStateEvents OnMapState;
    public static event GameStateEvents OnEnterGame;
    public static event GameStateEvents OnLevelLoaded;
    public static event GameStateEvents OnMenuPlay;
    public static event GameStateEvents OnMenuComplete;
    public static event GameStateEvents OnTouchDetected;
    public static event GameStateEvents OnWin;
    public static event GameStateEvents OnLose;

    public static void TriggerOnMapState() => OnMapState?.Invoke();

    public static void TriggerOnEnterGame() => OnEnterGame?.Invoke();

    public static void TriggerOnLevelLoaded() => OnLevelLoaded?.Invoke();

    public static void TriggerOnMenuPlay() => OnMenuPlay?.Invoke();

    public static void TriggerOnMenuComplete() => OnMenuComplete?.Invoke();

    public static void TriggerOnTouchDetected() => OnTouchDetected?.Invoke();

    public static void TriggerOnWin() => OnWin?.Invoke();

    public static void TriggerOnLose() => OnLose?.Invoke();

    #endregion

    #region Lifecycle

    /// <summary>Called by GameInitializer once the scene's dependencies are injected.</summary>
    public void InvokeStart()
    {
        new TargetUIResolverService(this).Resolve();
        InitializeCollectedTargetFlyController();
        _scorePopupTweenSpawner = GetComponent<ScorePopupTweenSpawner>();
        _levelEffectsController = GetComponent<LevelEffectsController>();
        _levelEffectsController?.Initialize(this);
        _gridPerimeterRenderer = GetComponent<GridPerimeterRenderer>();
        _gridPerimeterRenderer.Initialize(this);
        _outline = new BoardOutlineService(GameField, ResolveGridOutlineGenerator(), _gridPerimeterRenderer);
        _chainLine = new ChainLineDrawer(line);

        if (GameFeelManager.Instance != null)
        {
            GameFeelManager.Instance.BindCamera(gameCamera);
            GameFeelManager.Instance.BindBoard(GameField);
        }
        else
        {
            GameFeelManager.EnsureInitialized(gameCamera, GameField);
        }

        _boardMechanicsService = new BoardMechanicsService(this);
        _scoreTrackerService = new ScoreTrackerService(this);
        _boardQueryService = new BoardQueryService(this);
        _boardFactoryService = new BoardFactoryService(this);
        _winLoseEvaluator = new WinLoseEvaluator(this);
        _collectableTargetUIBuilder = new CollectableTargetUIBuilder(this);

        ingrCountTarget = new int[NumIngredients];

        if (Level.gameObject.activeSelf)
            Level.gameObject.SetActive(false);

        // Create the DontDestroyOnLoad coroutine host now, not in the middle of a state change.
        _ = CoroutineManager.Instance;

        _stateMachine = new GameStateMachine(CreateStates(), StopLevelCoroutines, StatelessGameStates.Enter,
            _statusBeforeStart);

        THIS = this;
        Instance = this;

        passLevelCounter = 0;

        gameStatus = GameState.PrepareGame;
    }

    /// <summary>Called every frame by GameInitializer.</summary>
    public void InvokeUpdate()
    {
        _stateMachine?.Update();
        _chainLine?.Draw(destroyAnyway);
    }

    private Dictionary<GameState, GameStateBase> CreateStates()
    {
        return new Dictionary<GameState, GameStateBase>
        {
            { GameState.Map, new MapState(this) },
            { GameState.PrepareGame, new PrepareGameState(this) },
            { GameState.WaitForPopup, new WaitForPopupState(this) },
            { GameState.PrepareBoosts, new PrepareBoostsState(this) },
            { GameState.Playing, new PlayingState(this) },
            { GameState.GameOver, new GameOverState(this, AdRewardService, SceneLoader) },
            { GameState.PreWinAnimations, new PreWinAnimationsState(this) },
            { GameState.Win, new WinState(this) },
            { GameState.RegenLevel, new RegenLevelState(this) }
        };
    }

    // Every state change stops what the previous state started on this object.
    private void StopLevelCoroutines()
    {
        if (gameObject.activeInHierarchy)
            StopAllCoroutines();
    }

    private void InitializeCollectedTargetFlyController()
    {
        if (_collectedTargetFlyController == null)
            _collectedTargetFlyController = GetComponent<CollectedTargetFlyController>();

        if (_collectedTargetFlyController == null)
            _collectedTargetFlyController = gameObject.AddComponent<CollectedTargetFlyController>();

        _collectedTargetFlyController.Initialize(this);
    }

    private GridOutlineGenerator ResolveGridOutlineGenerator()
    {
        if (gridOutlineGenerator == null)
            gridOutlineGenerator = GetComponent<GridOutlineGenerator>();

        return gridOutlineGenerator;
    }

    private void OnDestroy()
    {
        _outline?.Clear();

        CoroutineManager.ExistingInstance?.StopAllManagedCoroutines();

        _stateMachine?.Dispose();

        THIS = null;
        Instance = null;

        OnMapState = null;
        OnEnterGame = null;
        OnLevelLoaded = null;
        OnMenuPlay = null;
        OnMenuComplete = null;
        OnTouchDetected = null;
        OnWin = null;
        OnLose = null;
    }

    #endregion

    #region State Management

    public void ResumeGame()
    {
        gameStatus = GameState.Playing;
        Time.timeScale = 1;
    }

    public void EnableMap(bool enable)
    {
        _stateMachine.Get<MapState>().EnableMap(enable);
    }

    public void LockBoosts() => Boosts.LockOthers();

    public void UnLockBoosts() => Boosts.UnlockAll();

    public void RestartTimer()
    {
        if (_stateMachine?.Current is PlayingState)
            Timer.Restart();
    }

    #endregion

    #region Level Loading

    public void LoadLevel()
    {
        currentLevel = LevelLoader.ResolveOpenLevel();
        LoadDataFromLocal(currentLevel);
        NumIngredients = ingrTarget.Count;
        ProgressBarScript.Instance?.ConfigureForLevel();
    }

    public void LoadDataFromLocal(int level)
    {
        levelLoaded = false;

        LevelData data = LevelLoader.Load(level, collectedIngredients);
        if (data != null)
            ApplyLevelData(data);
    }

    private void ApplyLevelData(LevelData data)
    {
        target = data.Target;
        maxCols = data.MaxCols;
        maxRows = data.MaxRows;
        squaresArray = new Square[maxCols * maxRows];
        levelSquaresFile = data.LevelSquares;
        limitType = data.LimitType;
        Limit = data.Limit;
        colorLimit = data.ColorLimit;
        star1 = data.Star1;
        star2 = data.Star2;
        star3 = data.Star3;
        ingrTarget = data.IngrTarget;
        collectItems = data.CollectItems;
        cageHP = data.CageHp;
        bombsCollect = data.BombsCollect;
        bombTimer = data.BombTimer;
        starsTargetCount = data.StarsTargetCount;
        TargetBlocks = data.TargetBlocks;
        TargetCages = data.TargetCages;
        levelLoaded = true;
    }

    public void GenerateLevel()
    {
        GameFieldTargetLocalPosition = _boardFactoryService.GenerateLevel();
    }

    public void CreateObstacles(int col, int row, GameObject square, SquareTypes type)
    {
        _boardFactoryService.CreateObstacles(col, row, square, type);
    }

    public void GenerateOutline() => _outline?.Draw();

    public void ClearOutline() => _outline?.Clear();

    #endregion

    #region Board Items

    public void GenerateNewItems(bool falling = true) => BoardItems.GenerateNewItems(falling);

    public async UniTask FindMatches()
    {
        if (_stateMachine?.Current is PlayingState playingState)
            await playingState.FindMatches();
    }

    public IEnumerator FindMatchDelay()
    {
        yield return new WaitForSeconds(0.2f);
        FindMatches().Forget();
    }

    public async UniTask ProcessMatchesAndFalling(bool freezeAnimations = true)
    {
        // Tied to this LevelManager: a cascade still running when the game scene is reloaded used to keep
        // going against destroyed objects (MissingReferenceException on GameField every move).
        await _boardMechanicsService.ProcessBoardAfterMatches(this.GetCancellationTokenOnDestroy(), freezeAnimations);
    }

    public async UniTask SettleBoardBeforePreWin()
    {
        await _boardMechanicsService.SettleBoardGravityOnly();
        ResumeBoardAmbientAnimations();
    }

    public void ResumeBoardAmbientAnimations() => BoardItems.ResumeAmbientAnimations();

    public void ClearHighlight(bool boost = false)
    {
        if (boost)
            BoardItems.ClearHighlight();
    }

    public void SetTypeByColor(int color, ItemsTypes nextType) => BoardItems.SetTypeByColor(color, nextType);

    public void SetColorToRandomItems() => LevelEffectsController?.PlayRandomColorReveal();

    #endregion

    #region Square and Item Queries

    public Square GetSquare(int col, int row, bool safe = false) => _boardQueryService.GetSquare(col, row, safe);

    public List<Item> GetRow(int row) => _boardQueryService.GetRow(row);

    public List<Square> GetRowSquare(int row) => _boardQueryService.GetRowSquare(row);

    public List<Item> GetColumn(int col) => _boardQueryService.GetColumn(col);

    public List<Square> GetColumnSquare(int col) => _boardQueryService.GetColumnSquare(col);

    public List<Item> GetRandomItems(int count) => _boardQueryService.GetRandomItems(count);

    public List<Item> GetAllExtraItems() => _boardQueryService.GetAllExtraItems();

    public List<Item> GetItemsAround(Square square) => _boardQueryService.GetItemsAround(square);

    public List<Square> GetSquaresAround(Square square) => _boardQueryService.GetSquaresAround(square);

    public List<Item> GetItems() => _boardQueryService.GetItems();

    public List<Square> GetSquares() => _boardQueryService.GetSquares();

    public List<Square> GetBottomRow() => _boardQueryService.GetBottomRow();

    public List<Item> GetIngredients(int i = -1) => _boardQueryService.GetIngredients(i);

    #endregion

    #region Special Effects

    public void DestroyDoubleBomb(int col) => LevelEffectsController?.PlayDoubleBombWave(col);

    public void StrippedShow(GameObject obj, bool horrizontal) => LevelEffectsController?.PlayStripedEffect(obj, horrizontal);

    public GameObject GetExplFromPool() =>
        LevelEffectsController != null ? LevelEffectsController.GetExplosion() : null;

    public GameObject GetFlowerFromPool() =>
        LevelEffectsController != null ? LevelEffectsController.GetFlower() : null;

    public bool CheckFlowerStillFly() =>
        LevelEffectsController != null && LevelEffectsController.HasFlyingFlowers();

    public void ForceCleanupFlowers() => LevelEffectsController?.ForceCleanupAllFlowers();

    #endregion

    #region Targets

    public void CreateCollectableTarget(GameObject parentTransform, Target tar, bool ForDialog = true)
    {
        _collectableTargetUIBuilder.Build(parentTransform, tar, ForDialog);
    }

    public void CheckCollectedTarget(GameObject item)
    {
        if (_collectedTargetFlyController == null)
            InitializeCollectedTargetFlyController();

        _collectedTargetFlyController.TryFly(item);
    }

    public int GetRestIngredients() => _winLoseEvaluator.GetRestIngredients();

    public void CheckWinLose()
    {
        if (gameStatus != GameState.Playing)
            return;

        // A goal reached on the last move is still a win, so evaluate it before the limit.
        switch (_winLoseEvaluator.Evaluate())
        {
            case WinLoseEvaluation.Win:
                gameStatus = GameState.PreWinAnimations;
                break;
            case WinLoseEvaluation.Lose:
                // A collected ingredient / defused bomb counts when its flight lands; one still in the air
                // can finish the goal, and the landing runs CheckWinLose again.
                if (ingredientFly)
                    break;
                Limit = 0;
                gameStatus = GameState.GameOver;
                break;
        }
    }

    public int GetScoresOfTargetStars() => _winLoseEvaluator.GetScoresOfTargetStars();

    public void PopupScore(int value, Vector3 pos, int color) => _scoreTrackerService.PopupScore(value, pos, color);

    #endregion

    #region Bombs

    public IEnumerator InitBombs() => Bombs.InitBombs();

    public void TopUpBombs() => Bombs.TopUp();

    public void RechargeBombs() => StartCoroutine(Bombs.InitBombs());

    #endregion
}

using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Threading;
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
using ZLinq;

[RequireComponent(typeof(GridOutlineGenerator), typeof(GridPerimeterRenderer))]
public class LevelManager : MonoBehaviour, ILevelManagerActions
{
    #region Static References

    // Instance of LevelManager for direct references
    public static LevelManager THIS;

    // Instance of LevelManager for direct references
    public static LevelManager Instance;

    // Global Score amount on current level
    public static int Score;

    #endregion

    #region Prefabs and Objects

    public List<CollectedIngredients> collectedIngredients = new List<CollectedIngredients>();

    // Prefab of item
    public GameObject itemPrefab;

    // Prefab of square
    public GameObject squarePrefab;

    // Prefab of block, for more info check enum SquareTypes
    public GameObject blockPrefab;

    // Prefab of wire, for more info check enum SquareTypes
    public GameObject wireBlockPrefab;

    // Prefab of solid block, for more info check enum SquareTypes
    public GameObject solidBlockPrefab;

    // Prefab of undestroyable block, for more info check enum SquareTypes
    public GameObject undesroyableBlockPrefab;

    // Prefab of growing block, for more info check enum SquareTypes
    public GameObject thrivingBlockPrefab;

    // Life shop scene object
    public LifeShop lifeShop;

    // Gamefield scene object
    public Transform GameField;

    public Vector3 GameFieldTargetLocalPosition { get; private set; }

    // Scene object
    public GameObject LevelsMap;

    // UI object
    public GameObject Level;

    // UI objects for targets
    public GameObject ingrObject;
    public GameObject blocksObject;
    public GameObject scoreTargetObject;
    public GameObject cageTargetObject;
    public GameObject bombTargetObject;

    // Camera
    public Camera gameCamera;

    // Line renderer
    public Line line;

    [SerializeField] private GridOutlineGenerator gridOutlineGenerator;

    #endregion

    #region Sprites

    // Sprite of square
    public Sprite squareSprite;

    // Second sprite of square
    public Sprite squareSprite1;

    // Sprites of collectable items
    public Sprite[] ingrediendSprites;

    // Double block sprite
    public Sprite doubleBlock;

    // Double solid block sprite
    public Sprite doubleSolidBlock;

    #endregion

    #region Level Configuration

    // Enabling iapps flag
    public bool enableInApps;

    // Type of game limit (moves or time)
    public LIMIT limitType;

    // Value of rest limit (moves or time)
    public int Limit = 30;

    // Current level number
    public int currentLevel = 1;

    // Cost of continue playing after fail
    public int FailedCost;

    // Extra moves that you get to continue game after fail
    public int ExtraFailedMoves = 5;

    // Extra seconds that you get to continue game after fail
    public int ExtraFailedSecs = 30;

    // Max rows of gamefield
    public int maxRows = 8;

    // Max cols of gamefield
    public int maxCols = 7;

    // Right square size for level generation
    public float squareWidth = 1.2f;

    // Right square size for level generation
    public float squareHeight = 1.2f;

    // Position of the first square on the game field
    public Vector2 firstSquarePosition;

    // Editor variable for limitation of colors
    public int colorLimit;

    // Editor values of description tasks
    public string[] targetDiscriptions;

    // Facebook enable
    public bool FacebookEnable;

    // PlayFab
    public bool PlayFab;

    // Extra item every
    public float extraItemEvery = 6;

    // Bomb timer
    public int bombTimer;

    // Number of ingredients
    public int NumIngredients = 4;

    #endregion

    #region Game State and Targets

    public Target target;

    // Inner using
    private GameState GameStatus;

    // Amount of scores for item
    public int scoreForItem = 10;

    // Amount of scores for block
    public int scoreForBlock = 100;

    // Amount of scores for wire block
    public int scoreForWireBlock = 100;

    // Amount of scores for solid block
    public int scoreForSolidBlock = 100;

    // Amount of scores for growing block
    public int scoreForThrivingBlock = 100;

    // Array of colors for popup scores
    public Color[] scoresColors;

    // Stars amount on current level
    public int stars;

    // Amount of scores is necessary for reaching first star
    public int star1;

    // Amount of scores is necessary for reaching second star
    public int star2;

    // Amount of scores is necessary for reaching third star
    public int star3;

    // Get stars
    public CollectStars starsTargetCount;

    public int RequiredStars => Mathf.Clamp((int)starsTargetCount, 1, 3);

    // Amount of blocks for collecting
    public int targetBlocks;

    // Target cages
    public int TargetCages;

    // Target bombs
    public int TargetBombs;

    #endregion

    #region Items and Squares

    // Array for holding squares data of the game field
    public Square[] squaresArray;

    // Array of combined items
    public List<List<Item>> combinedItems = new List<List<Item>>();

    // Latest touched item
    public Item lastDraggedItem;

    // Array for items prepeared to destory
    public List<Item> destroyAnyway = new List<Item>();

    // Level data from the file
    SquareBlocks[] levelSquaresFile = new SquareBlocks[81];

    internal SquareBlocks[] LevelSquaresFile => levelSquaresFile;

    // Array of highlighted items
    public List<Item> highlightedItems;

    // Necessary amount of collectable items
    public int[] ingrCountTarget = new int[4];

    // Necessary collectable items
    public List<CollectedIngredients> ingrTarget = new List<CollectedIngredients>();

    // Necessary collectable items
    public CollectItems[] collectItems = new CollectItems[6];

    // List of GameObject for UI ingredients
    public List<GameObject> listIngredientsGUIObjects = new List<GameObject>();

    // List of types
    public List<ItemsTypes> gatheredTypes = new List<ItemsTypes>();

    // List of vector3 for flowers
    public List<Vector3> startPosFlowers = new List<Vector3>();

    #endregion

    #region Boosts and Effects

    // Amount of boosts
    public int BoostColorfullBomb;

    // Amount of boosts
    public int BoostPackage;

    // Amount of boosts
    public int BoostStriped;

    // Inner using variable
    public BoostIcon emptyBoostIcon;

    // Currently active boost
    public BoostIcon activatedBoost;

    // Array of in-game boosts
    public BoostIcon[] InGameBoosts;

    // Waiting boost
    public BoostIcon waitingBoost;

    #endregion

    #region Game Data

    // Array of iapps products
    public List<GemProduct> gemsProducts = new List<GemProduct>();

    // List of bomb timers
    public List<int> bombTimers = new List<int>();

    // Level pass counter
    public int passLevelCounter;

    // Move ID
    public int moveID;

    // Last random color
    public int lastRandColor;

    // Selected color
    public int selectedColor;

    // Inner using
    public int nextExtraItems;

    // Cage HP
    private int cageHP;

    internal int CageHp => cageHP;

    // Deprecated
    private int linePoint;

    // Items hidden flag
    public bool itemsHided;

    // Only falling flag
    public bool onlyFalling;

    // Level loaded flag
    public bool levelLoaded;

    // Counted squares
    public Hashtable countedSquares;

    CollectedTargetFlyController _collectedTargetFlyController;
    ScorePopupTweenSpawner _scorePopupTweenSpawner;
    LevelEffectsController _levelEffectsController;
    GridPerimeterRenderer _gridPerimeterRenderer;

    public ScorePopupTweenSpawner ScorePopupTweenSpawner
    {
        get
        {
            if(_scorePopupTweenSpawner == null)
            {
                _scorePopupTweenSpawner = GetComponent<ScorePopupTweenSpawner>();
            }

            return _scorePopupTweenSpawner;
        }
    }

    public LevelEffectsController LevelEffectsController
    {
        get
        {
            if(_levelEffectsController == null)
            {
                _levelEffectsController = GetComponent<LevelEffectsController>();
            }

            return _levelEffectsController;
        }
    }

    // Kept as a property for the board state checks that wait for target flights to finish.
    public bool ingredientFly => _collectedTargetFlyController != null &&
                                 _collectedTargetFlyController.IsFlying;

    // Test by play flag
    public bool testByPlay;

    // Extra cage add item
    public int extraCageAddItem;

    // Bombs collect
    public int bombsCollect;

    // Stop sliding flag
    private bool stopSliding;

    // Offset
    private float offset;

    #endregion

    #region Board Mechanics

    private BoardMechanicsService _boardMechanicsService;
    private ScoreTrackerService _scoreTrackerService;
    private BoardQueryService _boardQueryService;
    private BoardFactoryService _boardFactoryService;
    private WinLoseEvaluator _winLoseEvaluator;
    private CollectableTargetUIBuilder _collectableTargetUIBuilder;

    public BoardMechanicsService BoardMechanics
    {
        get { return _boardMechanicsService; }
    }

    private Dictionary<GameState, GameStateBase> _states;
    private GameStateBase _currentState;

    #endregion

    #region Properties

    // Is any growing blocks destroyed in that turn
    public bool thrivingBlockDestroyed;

    // Is touch blocks
    public bool dragBlocked;

    public bool DragBlocked
    {
        get { return dragBlocked; }
        set
        {
            if(value)
            {
                List<Item> items = GetItems();

                foreach (Item item in items)
                {
                    // Items own and stop their visual tweens explicitly.
                }
            }
            else
            {
                // StartCoroutine(StartIdleCor());
            }

            dragBlocked = value;
        }
    }

    public int TargetBlocks
    {
        get { return targetBlocks; }
        set
        {
            if(targetBlocks < 0)
                targetBlocks = 0;
            targetBlocks = value;
        }
    }

    public GameState gameStatus
    {
        get { return GameStatus; }
        set
        {
            Debug.Log("Changing game state from " + GameStatus + " to " + value);

            if(GridPerimeterRenderer.ShouldHideForState(value))
                _gridPerimeterRenderer?.Clear();
            else if(value == GameState.Pause)
                _gridPerimeterRenderer?.SetVisible(false);
            else if(value == GameState.Playing)
                _gridPerimeterRenderer?.SetVisible(true);

            if(_currentState != null)
            {
                Debug.Log("Exiting current state: " + _currentState.GetType().Name);
                _currentState.ExitState();
            }

            if(gameObject.activeInHierarchy)
            {
                StopAllCoroutines();
            }

            GameStatus = value;

            if(_states != null && _states.ContainsKey(value))
            {
                _currentState = _states[value];

                Debug.Log("Entering new state: " + _currentState.GetType().Name);
                _currentState.EnterState();
            }
            else
            {
                Debug.Log("Handling legacy state: " + value);
                HandleLegacyState(value);
            }
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

    // Field of getting and setting currently activated boost
    public BoostIcon ActivatedBoost
    {
        get
        {
            if(activatedBoost == null)
            {
                return emptyBoostIcon;
            }
            else
                return activatedBoost;
        }
        set
        {
            if(value == null)
            {
                // if (activatedBoost != null && gameStatus == GameState.Playing)
                //     InitScript.Instance.SpendBoost(activatedBoost.type);
                UnLockBoosts();
            }

            activatedBoost = value;

            if(value != null)
            {
                LockBoosts();
            }

            if(activatedBoost != null)
            {
                if(activatedBoost.type == BoostType.ExtraMoves)
                {
                    //here is checking what kind of limitation in level - Moves or Time
                    if(LevelManager.Instance.limitType == LIMIT.MOVES)
                    {
                        var model = BoostersProvider.BoostersModels.AsValueEnumerable()
                            .First(t => t.Type == BoostType.ExtraMoves);

                        if(model.HasBooster())
                        {
                            LevelManager.THIS.Limit += 5;
                            model.Use();
                            BoostersProvider.Save();
                        }
                    }
                    // else
                    // LevelManager.THIS.Limit += 30;

                    ActivatedBoost = null;
                }
            }
        }
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

    // Methods to trigger each event
    public static void TriggerOnMapState()
    {
        OnMapState?.Invoke();
    }

    public static void TriggerOnEnterGame()
    {
        OnEnterGame?.Invoke();
    }

    public static void TriggerOnLevelLoaded()
    {
        OnLevelLoaded?.Invoke();
    }

    public static void TriggerOnMenuPlay()
    {
        OnMenuPlay?.Invoke();
    }

    public static void TriggerOnMenuComplete()
    {
        OnMenuComplete?.Invoke();
    }

    public static void TriggerOnTouchDetected()
    {
        OnTouchDetected?.Invoke();
    }

    public static void TriggerOnWin()
    {
        OnWin?.Invoke();
    }

    public static void TriggerOnLose()
    {
        OnLose?.Invoke();
    }

    #endregion

    #region Unity Lifecycle Methods

    public void InvokeStart()
    {
        new TargetUIResolverService(this).Resolve();
        InitializeCollectedTargetFlyController();
        _scorePopupTweenSpawner = GetComponent<ScorePopupTweenSpawner>();
        _levelEffectsController = GetComponent<LevelEffectsController>();
        _levelEffectsController?.Initialize(this);
        _gridPerimeterRenderer = GetComponent<GridPerimeterRenderer>();
        _gridPerimeterRenderer.Initialize(this);

        if(GameFeelManager.Instance != null)
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

        ingrCountTarget = new int[NumIngredients]; // Necessary amount of collectable items

        if(Level.gameObject.activeSelf)
            Level.gameObject.SetActive(false);

        CoroutineManager.Instance.GetType();

        _states = new Dictionary<GameState, GameStateBase>
        {
            {
                GameState.Map, new MapState(this)
            },
            {
                GameState.PrepareGame, new PrepareGameState(this)
            },
            {
                GameState.WaitForPopup, new WaitForPopupState(this)
            },
            {
                GameState.PrepareBoosts, new PrepareBoostsState(this)
            },
            {
                GameState.Playing, new PlayingState(this)
            },
            {
                GameState.GameOver, new GameOverState(this, AdRewardService, SceneLoader)
            },
            {
                GameState.PreWinAnimations, new PreWinAnimationsState(this)
            },
            {
                GameState.Win, new WinState(this)
            },
            {
                GameState.RegenLevel, new RegenLevelState(this)
            }
        };

        THIS = this;
        Instance = this;
        // gameStatus = GameState.Map;

        passLevelCounter = 0;

        gameStatus = GameState.PrepareGame;
    }

    void InitializeCollectedTargetFlyController()
    {
        if (_collectedTargetFlyController == null)
        {
            _collectedTargetFlyController = GetComponent<CollectedTargetFlyController>();
        }

        if (_collectedTargetFlyController == null)
        {
            _collectedTargetFlyController = gameObject.AddComponent<CollectedTargetFlyController>();
        }

        _collectedTargetFlyController.Initialize(this);
    }

    public void InvokeUpdate()
    {
        if(_currentState != null)
        {
            _currentState.UpdateState();
        }

        // Now draw the lines based on the updated destroyAnyway list
        if(destroyAnyway.Count > 0)
        {
            int i = 0;
            line.SetVertexCount(destroyAnyway.Count);

            foreach (Item item in destroyAnyway)
            {
                if(item != null)
                {
                    line.AddPoint(item.transform.position, i);
                    i++;
                }
            }
        }
        else
        {
            line.SetVertexCount(0);
        }
    }

    private void OnDestroy()
    {
        _gridPerimeterRenderer?.Clear();

        CoroutineManager coroutineManager = CoroutineManager.ExistingInstance;
        if(coroutineManager != null)
        {
            coroutineManager.StopAllManagedCoroutines();
        }

        if(_states != null)
        {
            foreach (var state in _states.Values)
            {
                (state as IDisposable)?.Dispose();
            }
        }

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

    private void HandleLegacyState(GameState state)
    {
        if(state == GameState.PreFailedBomb) { }
        else if(state == GameState.PreFailed)
        {
            GameObject.Find("CanvasGlobal").transform.Find("PreFailed").gameObject.SetActive(true);
        }
        else if(state == GameState.Pause)
        {
            Time.timeScale = 0;
        }
        else if(state == GameState.ToMap)
        {
            MusicBase.Instance.GetComponent<AudioSource>().Stop();
            SoundBase.Instance.PlaySound(SoundBase.Instance.gameOver[0]);
            GameObject.Find("CanvasGlobal").transform.Find("MenuFailed").gameObject.SetActive(true);
        }
    }

    public void EnableMap(bool enable)
    {
        MapState mapState = _states[GameState.Map] as MapState;
        mapState.EnableMap(enable);
    }

    public void LockBoosts()
    {
        foreach (BoostIcon item in InGameBoosts)
        {
            if(item != ActivatedBoost)
                item.LockBoost();
        }
    }

    public void UnLockBoosts()
    {
        foreach (BoostIcon item in InGameBoosts)
        {
            item.UnLockBoost();
        }
    }

    public void RestartTimer()
    {
        if(_currentState is PlayingState playingState)
        {
            playingState.RestartTimer();
        }
    }

    #endregion

    #region Level Management

    public void LoadLevel()
    {
        currentLevel = PlayerPrefs.GetInt("OpenLevel"); // TargetHolder.level;
        if(currentLevel == 0)
            currentLevel = 1;

        currentLevel = LevelProgressionHelper.CalculateLevel(currentLevel);
        LoadDataFromLocal(currentLevel);
        NumIngredients = ingrTarget.Count;
        ProgressBarScript.Instance?.ConfigureForLevel();
    }

    public void LoadDataFromLocal(int currentLevel)
    {
        levelLoaded = false;
        TextAsset mapText = Resources.Load("Levels/" + currentLevel) as TextAsset;

        if(mapText == null)
        {
            mapText = Resources.Load("Levels/" + currentLevel) as TextAsset;
        }

        ApplyLevelData(LevelParserService.Parse(mapText.text, collectedIngredients));
    }

    void ApplyLevelData(LevelData data)
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

    public void ReGenLevel()
    {
        itemsHided = false;
        DragBlocked = true;
        if(gameStatus != GameState.Playing && gameStatus != GameState.RegenLevel)
            DestroyItems();
        else if(gameStatus == GameState.RegenLevel)
            DestroyItems(true);
        GenerateNewItems(false);

        StartCoroutine(InitBombs());

        DragBlocked = false;
        gameStatus = GameState.Playing;
        OnLevelLoaded();
    }

    public void GenerateLevel()
    {
        GameFieldTargetLocalPosition = _boardFactoryService.GenerateLevel();
    }

    public void CreateObstacles(int col, int row, GameObject square, SquareTypes type)
    {
        _boardFactoryService.CreateObstacles(col, row, square, type);
    }

    public void GenerateOutline()
    {
        DisableAndClearSpriteOutlines();
        _gridPerimeterRenderer?.Draw();
    }

    public void ClearOutline()
    {
        _gridPerimeterRenderer?.Clear();
    }

    private GridOutlineGenerator ResolveGridOutlineGenerator()
    {
        if(gridOutlineGenerator == null)
            gridOutlineGenerator = GetComponent<GridOutlineGenerator>();

        return gridOutlineGenerator;
    }

    private void DisableAndClearSpriteOutlines()
    {
        GridOutlineGenerator generator = ResolveGridOutlineGenerator();
        if(generator != null)
            generator.enabled = false;

        // GridOutlineGenerator creates child objects named "outline" under squares.
        // Clear them so we don't keep the old visual border after switching to Line.prefab.
        if(GameField == null)
            return;

        for(int i = 0; i < GameField.childCount; i++)
        {
            Transform square = GameField.GetChild(i);
            if(square == null)
                continue;

            // Iterate backwards because we may destroy children.
            for(int j = square.childCount - 1; j >= 0; j--)
            {
                Transform child = square.GetChild(j);
                if(child != null && child.name == "outline")
                    Destroy(child.gameObject);
            }
        }
    }

    #endregion

    #region Item Management

    public void GenerateNewItems(bool falling = true)
    {
        for (int col = 0; col < maxCols; col++)
        {
            for (int row = maxRows - 1; row >= 0; row--)
            {
                if(GetSquare(col, row) != null)
                {
                    if(!GetSquare(col, row).IsNone() &&
                       GetSquare(col, row).CanGoInto() &&
                       GetSquare(col, row).item == null)
                    {
                        if((GetSquare(col, row).item == null && !GetSquare(col, row).IsHaveSolidAbove()) || !falling)
                        {
                            GetSquare(col, row).GenItem(falling);
                        }
                    }
                }
            }
        }
    }

    public void DestroyItems(bool withoutEffects = false)
    {
        GameObject[] items = GameObject.FindGameObjectsWithTag("Item");

        foreach (GameObject item in items)
        {
            if(item != null)
            {
                if(item.GetComponent<Item>().currentType != ItemsTypes.INGREDIENT)
                {
                    if(!withoutEffects)
                        item.GetComponent<Item>().DestroyItem();
                    else
                        item.GetComponent<Item>().SmoothDestroy();
                }
            }
        }
    }

    public async UniTask FindMatches()
    {
        if(_currentState is PlayingState playingState)
        {
            await playingState.FindMatches();
        }
    }

    public IEnumerator FindMatchDelay()
    {
        yield return new WaitForSeconds(0.2f);
        LevelManager.THIS.FindMatches();
    }

    public async UniTask ProcessMatchesAndFalling(bool freezeAnimations = true)
    {
        await _boardMechanicsService.ProcessBoardAfterMatches(CancellationToken.None, freezeAnimations);
    }

    public async UniTask SettleBoardBeforePreWin()
    {
        await _boardMechanicsService.SettleBoardGravityOnly();
        ResumeBoardAmbientAnimations();
    }

    public void ResumeBoardAmbientAnimations()
    {
        foreach (Item item in GetItems())
        {
            if (item != null && !item.destroying)
                item.SleepItem();
        }
    }

    void DestroyGatheredExtraItems(Item item)
    {
        if(gatheredTypes.Count > 1)
        {
            item.DestroyHorizontal();
            item.DestroyVertical();
        }

        foreach (ItemsTypes itemType in gatheredTypes)
        {
            if(itemType == ItemsTypes.HORIZONTAL_STRIPPED)
                item.DestroyHorizontal();
            else
                item.DestroyVertical();
        }
    }

    public void ClearHighlight(bool boost = false)
    {
        if(!boost)
            return;
        highlightedItems.Clear();

        for (int col = 0; col < maxCols; col++)
        {
            for (int row = 0; row < maxRows; row++)
            {
                if(GetSquare(col, row) != null)
                {
                    GetSquare(col, row).SetActiveCage(false);
                    GetSquare(col, row).HighLight(false);
                }
            }
        }
    }

    public void SetTypeByColor(int p, ItemsTypes nextType)
    {
        GameObject[] items = GameObject.FindGameObjectsWithTag("Item");

        foreach (GameObject item in items)
        {
            if(item.GetComponent<Item>().color == p)
            {
                if(nextType == ItemsTypes.HORIZONTAL_STRIPPED || nextType == ItemsTypes.VERTICAL_STRIPPED)
                    item.GetComponent<Item>().nextType = (ItemsTypes)UnityEngine.Random.Range(1, 3);
                else
                    item.GetComponent<Item>().nextType = nextType;

                item.GetComponent<Item>().ChangeType();
                if(nextType == ItemsTypes.NONE)
                    destroyAnyway.Add(item.GetComponent<Item>());
            }
        }
    }

    public void SetColorToRandomItems()
    {
        LevelEffectsController?.PlayRandomColorReveal();
    }

    #endregion

    #region Square and Item Queries

    public Square GetSquare(int col, int row, bool safe = false) =>
        _boardQueryService.GetSquare(col, row, safe);

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

    public void DestroyDoubleBomb(int col)
    {
        LevelEffectsController?.PlayDoubleBombWave(col);
    }

    public void StrippedShow(GameObject obj, bool horrizontal)
    {
        LevelEffectsController?.PlayStripedEffect(obj, horrizontal);
    }

    public GameObject GetExplFromPool()
    {
        return LevelEffectsController != null ? LevelEffectsController.GetExplosion() : null;
    }

    public GameObject GetFlowerFromPool()
    {
        return LevelEffectsController != null ? LevelEffectsController.GetFlower() : null;
    }

    public bool CheckFlowerStillFly()
    {
        return LevelEffectsController != null && LevelEffectsController.HasFlyingFlowers();
    }

    public void ForceCleanupFlowers()
    {
        LevelEffectsController?.ForceCleanupAllFlowers();
    }

    #endregion

    #region Target Management

    public IEnumerator TimeTick()
    {
        while (true)
        {
            if(gameStatus == GameState.Playing)
            {
                if(LevelManager.Instance.limitType == LIMIT.TIME)
                {
                    LevelManager.THIS.Limit--;
                    CheckWinLose();
                }
            }

            if(gameStatus == GameState.Map || LevelManager.THIS.Limit <= 0 || gameStatus == GameState.GameOver)
                yield break;

            yield return new WaitForSeconds(1);
        }
    }

    public void CreateCollectableTarget(GameObject parentTransform, Target tar, bool ForDialog = true)
    {
        _collectableTargetUIBuilder.Build(parentTransform, tar, ForDialog);
    }

    public void CheckCollectedTarget(GameObject _item)
    {
        if (_collectedTargetFlyController == null)
        {
            InitializeCollectedTargetFlyController();
        }

        _collectedTargetFlyController.TryFly(_item);
    }

    public int GetRestIngredients() => _winLoseEvaluator.GetRestIngredients();

    public void CheckWinLose()
    {
        if(gameStatus != GameState.Playing)
            return;

        // A goal reached on the last move is still a win, so evaluate it before the limit.
        switch (_winLoseEvaluator.Evaluate())
        {
            case WinLoseEvaluation.Win:
                gameStatus = GameState.PreWinAnimations;
                break;
            case WinLoseEvaluation.Lose:
                Limit = 0;
                gameStatus = GameState.GameOver;
                break;
        }
    }

    public int GetScoresOfTargetStars() => _winLoseEvaluator.GetScoresOfTargetStars();

    #endregion

    #region Score and Progression

    public void PopupScore(int value, Vector3 pos, int color)
    {
        _scoreTrackerService.PopupScore(value, pos, color);
    }

    #endregion

    #region Bomb Management

    public IEnumerator InitBombs()
    {
        yield return new WaitUntil(() => !TipsManager.THIS.gotTip); // 1.3
        yield return new WaitForSeconds(1);
        int bombsOnField = 0;
        List<Item> items = GetItems();

        foreach (Item item in items)
        {
            if(item.currentType == ItemsTypes.BOMB)
                bombsOnField++;
        }

        List<Item> itemsRand = GetRandomItems(bombsCollect - bombsOnField - LevelManager.THIS.TargetBombs); // 1.3
        int i = 0;

        foreach (Item item in itemsRand)
        {
            item.nextType = ItemsTypes.BOMB;

            if(bombTimers.Count > 0) // 1.3
                item.bombTimer = bombTimers[i];
            i++;
            item.ChangeType();
        }
    }

    public void RechargeBombs()
    {
        // 1.3
        StartCoroutine(InitBombs());
    }

    #endregion
}

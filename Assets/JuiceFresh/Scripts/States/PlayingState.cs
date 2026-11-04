using Cysharp.Threading.Tasks;
using JuiceFresh;
using JuiceFresh.Scripts;
using JuiceFresh.States;
using JuiceFresh.States.Playing;
using UnityEngine;

/// <summary>
/// The level is being played: routes touches to the chain or to an armed booster and runs a move
/// (spend the move, cascade, check for a dead board and for win/lose).
/// </summary>
public class PlayingState : GameStateBase
{
    private readonly BoardPointer _pointer;
    private readonly ChainSelection _chain;
    private readonly BoosterTargetApplier _boosters;
    private readonly PossibleMovesDetector _possibleMoves;

    private int _movesInProgress;

    public PlayingState(LevelManager levelManager) : base(levelManager)
    {
        _pointer = new BoardPointer(Camera.main);
        _chain = new ChainSelection(levelManager);
        _boosters = new BoosterTargetApplier(levelManager);
        _possibleMoves = new PossibleMovesDetector(levelManager);
    }

    /// <summary>True from the moment a move is released until its cascade has settled.</summary>
    public bool IsMoveInProgress => _movesInProgress > 0;

    public override void EnterState()
    {
        Time.timeScale = 1;
        _chain.ResetGesture();

        // Every state change stops LevelManager's coroutines, so a bomb spawn started before entering
        // Playing (level start, Continue, board regen) can be cancelled and the level becomes unwinnable.
        // InitBombs only tops up what is missing, so it is safe to run on each entry.
        if (levelManager.target == Target.BOMBS)
            levelManager.StartCoroutine(levelManager.InitBombs());

        if (levelManager.limitType == LIMIT.TIME)
            RestartTimer();
    }

    public override void UpdateState()
    {
        HandleInput();
    }

    public override void ExitState()
    {
        levelManager.ClearHighlight(true);
        _chain.Clear();
        levelManager.Timer.Stop();
    }

    public void RestartTimer() => levelManager.Timer.Restart();

    public async UniTask FindMatches()
    {
        await levelManager.ProcessMatchesAndFalling();
    }

    #region Public move API (also used by the editor AutoPlayBot)

    /// <summary>Drags over a piece: adds it to the chain or steps the chain back.</summary>
    public void SelectItem(Item item) => _chain.Select(item);

    /// <summary>Drops the chain without playing it.</summary>
    public void ClearSelection() => _chain.Clear();

    /// <summary>Plays the selected chain as a move.</summary>
    public async UniTask ProcessMatchedItems()
    {
        levelManager.DragBlocked = true;
        _movesInProgress++;

        // The move is spent when it is released. It used to be taken only after the whole cascade, and
        // DragBlocked drops a frame before that, so quick follow-up moves were queued and some were free.
        if (levelManager.limitType == LIMIT.MOVES)
            levelManager.Limit--;

        levelManager.moveID++;

        try
        {
            await levelManager.FindMatches();
        }
        finally
        {
            _movesInProgress--;
        }

        if (!_possibleMoves.HasPossibleMoves())
        {
            HandleNoMatches();
            return;
        }

        levelManager.CheckWinLose();
    }

    #endregion

    #region Input

    private void HandleInput()
    {
        if (levelManager.gameStatus != GameState.Playing || levelManager.DragBlocked || IsMoveInProgress)
            return;

        // Out of moves but still Playing: the last ingredient / defused bomb is flying to its counter and
        // decides win or lose when it lands. Without this the player got free moves in that window.
        if (levelManager.limitType == LIMIT.MOVES && levelManager.Limit <= 0)
            return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Utils.DebugTools.CheatPanel.BlocksGameInput)
            return;
#endif

        if (_pointer.IsPressed)
            HandleTouchDown();
        else if (_pointer.WasReleased)
            HandleTouchUp();
    }

    private void HandleTouchDown()
    {
        LevelManager.TriggerOnTouchDetected();

        Item item = _pointer.ItemUnderPointer();
        if (item == null || item.currentType == ItemsTypes.INGREDIENT || levelManager.DragBlocked)
            return;

        // An armed booster takes the tap on release.
        if (_boosters.IsBoardBoosterArmed)
            return;

        if (_chain.AcceptsColor(item.color))
            _chain.Select(item);
    }

    private void HandleTouchUp()
    {
        _boosters.TryApply(_pointer.SquareUnderPointer());

        _chain.ResetGesture();

        if (_chain.Count >= 3)
        {
            GameFeelManager.Instance?.OnMatchReleased(_chain.Count, _chain.CenterOfChain());
            ProcessMatchedItems().Forget();
        }
        else
        {
            if (_chain.Count > 0)
                GameFeelManager.Instance?.OnInvalidMove();

            _chain.Clear();
        }

        TryFireStripedPiece(_pointer.ItemUnderPointer());

        HighlightManager.StopAndClearAll();
    }

    // A single tap on a striped piece fires it.
    private void TryFireStripedPiece(Item item)
    {
        if (item == null || _chain.Count != 0)
            return;

        if (item.currentType == ItemsTypes.HORIZONTAL_STRIPPED)
            item.DestroyHorizontal();
        else if (item.currentType == ItemsTypes.VERTICAL_STRIPPED)
            item.DestroyVertical();
        else
            return;

        levelManager.DragBlocked = true;
        ProcessMatchedItems().Forget();
    }

    #endregion

    // No chain of 3 is left: say so and rebuild the board.
    private void HandleNoMatches()
    {
        if (levelManager.gameStatus != GameState.Playing)
            return;

        SoundBase.Instance.PlaySound(SoundBase.Instance.noMatch);
        GameSceneUi.FindInLevelCanvas(GameSceneUi.NoMoreMatches).gameObject.SetActive(true);
        levelManager.gameStatus = GameState.RegenLevel;
    }
}

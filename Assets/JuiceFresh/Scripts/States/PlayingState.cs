using System.Collections.Generic;
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
    // New pieces take their colour in Start, so the board is checked a moment after entering the state.
    private const float StartCheckDelay = 1f;

    // A board whose shape allows no chain at all must not rebuild forever.
    private const int MaxDeadStartRebuilds = 5;

    private readonly BoardPointer _pointer;
    private readonly ChainSelection _chain;
    private readonly BoosterTargetApplier _boosters;
    private readonly PossibleMovesDetector _possibleMoves;
    private readonly MoveHint _hint;

    private readonly List<Item> _foundChain = new List<Item>();

    private int _movesInProgress;
    private bool _startCheckPending;
    private float _startCheckAt;
    private int _deadStartRebuilds;

    public PlayingState(LevelManager levelManager) : base(levelManager)
    {
        _pointer = new BoardPointer(Camera.main);
        _chain = new ChainSelection(levelManager);
        _boosters = new BoosterTargetApplier(levelManager);
        _possibleMoves = new PossibleMovesDetector(levelManager);
        _hint = new MoveHint(_possibleMoves);
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

        _startCheckPending = true;
        _startCheckAt = Time.time + StartCheckDelay;
    }

    public override void UpdateState()
    {
        if (RebuildIfStartedDead())
            return;

        // Before the input: the hint must not take the mark off a cell the player has just selected.
        // A tutorial points at its own pieces, so the hint waits.
        _hint.Tick(IsWaitingForPlayer() && !GameTutorials.TutorialGate.IsShowing);
        HandleInput();
    }

    public override void ExitState()
    {
        _hint.Tick(false);
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
        _chain.ClearMarks();

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

    // The board is at rest and the player is not touching it.
    private bool IsWaitingForPlayer() =>
        levelManager.gameStatus == GameState.Playing
        && !levelManager.DragBlocked
        && !IsMoveInProgress
        && !_pointer.IsPressed
        && _chain.Count == 0
        && !_boosters.IsBoardBoosterArmed
        && (levelManager.limitType != LIMIT.MOVES || levelManager.Limit > 0);

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

        // A tutorial step waits for a button or a tap on its dim.
        if (GameTutorials.TutorialGate.BlocksBoard)
            return;

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

        // The chain tutorial lets only its shown pieces join.
        if (_chain.AcceptsColor(item.color) && GameTutorials.TutorialGate.Allows(item))
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

    // The check after a move never runs if the board has no chain from the start (walls split level 36 into
    // single rows), and the player could not make that first move.
    private bool RebuildIfStartedDead()
    {
        if (!_startCheckPending || Time.time < _startCheckAt || !IsWaitingForPlayer())
            return false;

        _startCheckPending = false;

        if (_possibleMoves.TryFindChain(_foundChain))
        {
            _deadStartRebuilds = 0;
            return false;
        }

        if (_deadStartRebuilds >= MaxDeadStartRebuilds)
            return false;

        _deadStartRebuilds++;
        HandleNoMatches();
        return true;
    }

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

// PlayingState.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JuiceFresh;
using JuiceFresh.Scripts;
using JuiceFresh.States;

public class PlayingState : GameStateBase
{
    // Constants
    private const float ITEM_SELECTION_DISTANCE_THRESHOLD = 2f;

    // References
    private Camera gameCamera;

    // State tracking variables
    private int selectedColor = -1;
    private bool stopSliding = false;
    private float offset = 0f;

    public PlayingState(LevelManager levelManager) : base(levelManager)
    {
        gameCamera = Camera.main; // Ensure we get the main camera
    }

    public override void EnterState()
    {
        // Set time scale to normal
        Time.timeScale = 1;

        // Reset state tracking variables
        selectedColor = -1;
        stopSliding = false;
        offset = 0f;

        // Check for possible matches
        levelManager.StartCoroutine(TipsManager.THIS.CheckPossibleCombines());

        // Every state change stops LevelManager's coroutines, so a bomb spawn started before entering
        // Playing (level start, Continue, board regen) can be cancelled and the level becomes unwinnable.
        // InitBombs only tops up what is missing, so it is safe to run on each entry.
        if(levelManager.target == Target.BOMBS)
        {
            levelManager.StartCoroutine(levelManager.InitBombs());
        }

        // If it's a timed level, make sure the timer is running
        if(levelManager.limitType == LIMIT.TIME)
        {
            RestartTimer();
        }
    }

    public override void UpdateState()
    {
        // Handle input in the Update method
        HandleInput();

        // Check for bombs that need to tick down
        ProcessBombTimers();

    }

    public override void ExitState()
    {
        // Clear any highlights or selections when exiting the state
        levelManager.ClearHighlight(true);
        ClearSelection();

        CoroutineManager.Instance.StopManagedCoroutine("TimeTick");
    }

    private void HandleInput()
    {
        if(levelManager.gameStatus != GameState.Playing || levelManager.DragBlocked || IsMoveInProgress)
            return;

        // Out of moves but still Playing: the last ingredient / defused bomb is flying to its counter and
        // decides win or lose when it lands. Without this the player got free moves in that window.
        if(levelManager.limitType == LIMIT.MOVES && levelManager.Limit <= 0)
            return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if(Utils.DebugTools.CheatPanel.BlocksGameInput)
            return;
#endif

        // Handle touch input
        if(Input.GetMouseButton(0))
        {
            HandleTouchDown();
        }
        else if(Input.GetMouseButtonUp(0))
        {
            HandleTouchUp();
        }
    }

    private void HandleTouchDown()
    {
        // Make sure we're in playing state and drag is not blocked
        if(levelManager.gameStatus != GameState.Playing || levelManager.DragBlocked)
            return;

        // Invoke the touch detected event
        LevelManager.TriggerOnTouchDetected();

        // Get the world position of the mouse cursor
        Vector2 worldPos = gameCamera.ScreenToWorldPoint(Input.mousePosition);

        // Check if we hit an item
        Collider2D hit = Physics2D.OverlapPoint(worldPos, 1 << LayerMask.NameToLayer("Item"));

        if(hit != null)
        {
            Item item = hit.gameObject.GetComponent<Item>();

            if(item != null)
            {
                ProcessItemTouch(item);
            }
        }
    }

    private void HandleTouchUp()
    {
        // Handle boost activation on squares
        ProcessBoostActivation();

        // Reset selection state
        selectedColor = -1;
        stopSliding = false;
        offset = 0;

        // Process the selected items if we have enough of them
        if(levelManager.destroyAnyway.Count >= 3)
        {
            int matchedCount = levelManager.destroyAnyway.Count;
            Vector3 matchCenter = Vector3.zero;
            foreach(Item matchedItem in levelManager.destroyAnyway)
            {
                matchCenter += matchedItem.transform.position;
            }

            matchCenter /= matchedCount;
            GameFeelManager.Instance?.OnMatchReleased(matchedCount, matchCenter);
            ProcessMatchedItems().Forget();
        }
        else
        {
            if(levelManager.destroyAnyway.Count > 0)
            {
                GameFeelManager.Instance?.OnInvalidMove();
            }

            ClearSelection();
        }

        Vector2 worldPos = gameCamera.ScreenToWorldPoint(Input.mousePosition);

        // Check if we hit an item
        Collider2D hit = Physics2D.OverlapPoint(worldPos, 1 << LayerMask.NameToLayer("Item"));

        if(hit != null)
        {
            Item item = hit.gameObject.GetComponent<Item>();

            if(item != null)
            {
                if(item.currentType == ItemsTypes.HORIZONTAL_STRIPPED && levelManager.destroyAnyway.Count == 0)
                {
                    item.DestroyHorizontal();
                    levelManager.DragBlocked = true;
                    ProcessMatchedItems().Forget();
                    // return;
                }
                else if(item.currentType == ItemsTypes.VERTICAL_STRIPPED && levelManager.destroyAnyway.Count == 0)
                {
                    item.DestroyVertical();
                    levelManager.DragBlocked = true;
                    ProcessMatchedItems().Forget();
                    // return;
                }
            }
        }


        // Clear highlights
        HighlightManager.StopAndClearAll();
    }

    private void ProcessItemTouch(Item item)
    {
        // Skip if item is null
        if(item == null)
        {
            return;
        }

        // Skip if the item is an ingredient or the game is blocked
        if(item.currentType == ItemsTypes.INGREDIENT || levelManager.DragBlocked)
        {
            return;
        }

        // Handle boost activation
        if(ProcessBoostSelection(item))
        {
            return;
        }

        // Regular item selection logic
        if(selectedColor == -1 || selectedColor == item.color)
        {
            SelectItem(item);
        }
    }

    private bool ProcessBoostSelection(Item item)
    {
        // Check if a boost is active - используем оригинальную проверку
        BoostType? activeBoostType = levelManager.ActivatedBoost?.type ?? null;

        if(activeBoostType == BoostType.Bomb && item.currentType != ItemsTypes.INGREDIENT)
        {
            return true;
        }
        else if(activeBoostType == BoostType.Shovel && item.currentType != ItemsTypes.INGREDIENT)
        {
            return true;
        }
        else if(activeBoostType == BoostType.Energy && item.currentType != ItemsTypes.INGREDIENT)
        {
            return true;
        }

        // Нет активного буста, возвращаем false
        return false;
    }

    private void SelectItem(Item item)
    {
        if(levelManager.extraCageAddItem < 0)
            levelManager.extraCageAddItem = 0;

        selectedColor = item.color;

        // Don't proceed if dragging is blocked or the game isn't in playing state
        if(levelManager.DragBlocked || levelManager.gameStatus != GameState.Playing || stopSliding)
            return;

        // Check distance between this item and the last selected item
        if(levelManager.destroyAnyway.Count > 1)
        {
            Vector2 pos1 = new Vector2(levelManager.destroyAnyway[levelManager.destroyAnyway.Count - 1].square.col,
                                       levelManager.destroyAnyway[levelManager.destroyAnyway.Count - 1].square.row);
            Vector2 pos2 = new Vector2(item.square.col, item.square.row);
            offset = Vector2.Distance(pos1, pos2);
        }

        // Add item to selection if it's not already selected and is close enough
        if(levelManager.destroyAnyway.IndexOf(item) < 0 && offset < ITEM_SELECTION_DISTANCE_THRESHOLD)
        {
            AddItemToSelection(item);
        }
        // If the item is already in selection, allow stepping back
        else if(levelManager.destroyAnyway.IndexOf(item) > -1)
        {
            RemoveLastItemFromSelection(item);
        }
    }

    private void AddItemToSelection(Item item)
    {
        if(levelManager.destroyAnyway.Count > 0)
        {
            Vector2 pos1 = new Vector2(levelManager.destroyAnyway[levelManager.destroyAnyway.Count - 1].square.col,
                                       levelManager.destroyAnyway[levelManager.destroyAnyway.Count - 1].square.row);
            Vector2 pos2 = new Vector2(item.square.col, item.square.row);
            offset = Vector2.Distance(pos1, pos2);

            if(offset >= ITEM_SELECTION_DISTANCE_THRESHOLD)
            {
                offset = 0;
                return;
            }
        }

        levelManager.destroyAnyway.Add(item);

        int selectingSoundNum = Mathf.Clamp(levelManager.destroyAnyway.Count - 1, 0, 9);
        SoundBase.Instance.PlaySound(SoundBase.Instance.selecting[selectingSoundNum]);
        GameFeelManager.Instance?.OnItemSelected(item.transform, levelManager.destroyAnyway.Count);

        int extraItemEvery = 6; // This should come from level manager

        if((levelManager.destroyAnyway.Count % (extraItemEvery + levelManager.extraCageAddItem) == 0) &&
           item.square.cageHP <= 0)
        {
            // This would highlight or mark the item for special treatment
            item.SetLight();
        }
        else if((levelManager.destroyAnyway.Count % (extraItemEvery + levelManager.extraCageAddItem) == 0) &&
                item.square.cageHP > 0)
        {
            levelManager.extraCageAddItem += 1;
        }

        // Track special item types for combo effects
        if(item.currentType == ItemsTypes.HORIZONTAL_STRIPPED)
            levelManager.gatheredTypes.Add(item.currentType);
        else if(item.currentType == ItemsTypes.VERTICAL_STRIPPED)
            levelManager.gatheredTypes.Add(item.currentType);

        // Highlight the item
        HighlightManager.SelectItem(item);

        // Activate the cage if applicable
        item.square.SetActiveCage(true);

        // Wake up the item (visual effect)
        item.AwakeItem();
    }

    private void RemoveLastItemFromSelection(Item item)
    {
        // Handle extra items and cages
        int extraItemEvery = 6; // This should come from level manager

        if((levelManager.destroyAnyway.Count % (extraItemEvery + levelManager.extraCageAddItem) == 0) &&
           item.square.cageHP > 0)
        {
            levelManager.extraCageAddItem -= 1;
        }

        // Only allow removing the most recent item
        if(levelManager.destroyAnyway.Count > 1 &&
           levelManager.destroyAnyway[levelManager.destroyAnyway.Count - 2] == item)
        {
            Item lastItem = levelManager.destroyAnyway[levelManager.destroyAnyway.Count - 1];

            // Remove any gathered types from this item
            if(lastItem.currentType == ItemsTypes.HORIZONTAL_STRIPPED && levelManager.gatheredTypes.Count > 0)
                levelManager.gatheredTypes.RemoveAt(levelManager.gatheredTypes.Count - 1);
            else if(lastItem.currentType == ItemsTypes.VERTICAL_STRIPPED && levelManager.gatheredTypes.Count > 0)
                levelManager.gatheredTypes.RemoveAt(levelManager.gatheredTypes.Count - 1);

            // Deactivate the item
            lastItem.SleepItem();
            lastItem.square.SetActiveCage(false);

            // Remove highlight
            HighlightManager.DeselectItem(lastItem, item);

            // Remove from selection
            levelManager.destroyAnyway.Remove(lastItem);
            GameFeelManager.Instance?.OnItemDeselected(
                lastItem.transform,
                item.transform,
                levelManager.destroyAnyway.Count);
        }
    }

    private void ProcessBoostActivation()
    {
        Collider2D hit = Physics2D.OverlapPoint(gameCamera.ScreenToWorldPoint(Input.mousePosition),
                                                1 << LayerMask.NameToLayer("Default"));

        if(hit != null)
        {
            Square square = hit.gameObject.GetComponent<Square>();
            if(square == null)
                return;

            Item item = square.item;

            // Проверка из оригинального кода
            bool isIngredient = false;

            if(item)
            {
                if(item.currentType == ItemsTypes.INGREDIENT)
                {
                    isIngredient = true;
                }

                if(!isIngredient)
                {
                    // Обработка разных типов бустов
                    BoostType? activeBoostType = levelManager.ActivatedBoost?.type ?? null;

                    var boosterModel =
                        levelManager.BoostersProvider.BoostersModels.Find(v => v.Type == activeBoostType);

                    if(activeBoostType == BoostType.Bomb && item.currentType != ItemsTypes.INGREDIENT)
                    {
                        ActivateBombBoost(square);
                        boosterModel.Use();
                    }
                    else if(activeBoostType == BoostType.Shovel && item.currentType != ItemsTypes.INGREDIENT)
                    {
                        ActivateShovelBoost(square);
                        boosterModel.Use();
                    }
                    else if(activeBoostType == BoostType.Energy && item.currentType != ItemsTypes.INGREDIENT)
                    {
                        ActivateEnergyBoost(square);
                        boosterModel.Use();
                    }

                    levelManager.BoostersProvider.Save();
                }
            }
        }
    }

    private void ActivateBombBoost(Square square)
    {
        SoundBase.Instance.PlaySound(SoundBase.Instance.boostBomb);
        levelManager.DragBlocked = true;

        // Create bomb effect
        GameObject obj = Object.Instantiate(Resources.Load("Prefabs/Effects/bomb"),
                                            square.transform.position,
                                            square.transform.rotation) as GameObject;
        obj.GetComponent<SpriteRenderer>().sortingOrder = 5;
        obj.GetComponent<BoostAnimation>().square = square;

        // Save the boost for later and clear the current active boost
        levelManager.waitingBoost = levelManager.ActivatedBoost;
        levelManager.ActivatedBoost = null;
    }

    private void ActivateShovelBoost(Square square)
    {
        levelManager.DragBlocked = true;

        // Create shovel effect
        GameObject obj = Object.Instantiate(Resources.Load("Prefabs/Effects/shovel"),
                                            square.transform.position,
                                            square.transform.rotation) as GameObject;
        obj.GetComponent<SpriteRenderer>().sortingOrder = 5;
        obj.GetComponent<BoostAnimation>().square = square;

        // Save the boost for later and clear the current active boost
        levelManager.waitingBoost = levelManager.ActivatedBoost;
        levelManager.ActivatedBoost = null;
    }

    private void ActivateEnergyBoost(Square square)
    {
        SoundBase.Instance.PlaySound(SoundBase.Instance.boostBomb);
        levelManager.DragBlocked = true;

        // Create energy effect
        GameObject obj = Object.Instantiate(Resources.Load("Prefabs/Effects/energy"),
                                            square.transform.position,
                                            square.transform.rotation) as GameObject;
        obj.GetComponent<SpriteRenderer>().sortingOrder = 5;
        obj.GetComponent<BoostAnimation>().square = square;

        // Save the boost for later and clear the current active boost
        levelManager.waitingBoost = levelManager.ActivatedBoost;
        levelManager.ActivatedBoost = null;
    }

    /// <summary>True from the moment a move is released until its cascade has settled.</summary>
    public bool IsMoveInProgress => _movesInProgress > 0;

    private int _movesInProgress;

    private async UniTask ProcessMatchedItems()
    {
        levelManager.DragBlocked = true;
        _movesInProgress++;

        // The move is spent when it is released. It used to be taken only after the whole cascade, and
        // DragBlocked drops a frame before that, so quick follow-up moves were queued and some were free.
        if(levelManager.limitType == LIMIT.MOVES)
        {
            levelManager.Limit--;
        }

        levelManager.moveID++;

        try
        {
            // Start the matching process
            await levelManager.FindMatches();
        }
        finally
        {
            _movesInProgress--;
        }

        if(!HasPossibleMoves())
        {
            HandleNoMatches();
            return;
        }

        // Check win/lose conditions after the move
        levelManager.CheckWinLose();
    }

    private void ClearSelection()
    {
        foreach (Item item in levelManager.destroyAnyway)
        {
            if(item != null)
            {
                item.SleepItem();
                item.square.SetActiveCage(false);
            }
        }

        levelManager.destroyAnyway.Clear();
        levelManager.gatheredTypes.Clear();
    }

    private void ProcessBombTimers()
    {
        if(levelManager.gameStatus != GameState.Playing)
            return;

        // Check bomb timers and update them
        // This is called by BombTick in the original code, typically after item matching and falling
        // We'll add this here to ensure bombs are properly managed

        // Note: The actual bomb processing is handled in the FallingDown coroutine
        // in the original code with: GameField.BroadcastMessage("BombTick");
    }

    // Methods to handle game mechanics after matching
    public void HandleNoMatches()
    {
        if(levelManager.gameStatus == GameState.Playing)
        {
            // Show "No more matches" message and regenerate the level
            SoundBase.Instance.PlaySound(SoundBase.Instance.noMatch);
            GameObject.Find("Level/Canvas").transform.Find("NoMoreMatches").gameObject.SetActive(true);
            levelManager.gameStatus = GameState.RegenLevel;
        }
    }

    public void HandleSpecialItems(Item triggerItem)
    {
        // Check for special combinations like matching 4+ items
        if(levelManager.lastDraggedItem != null)
        {
            // For 4 items, create a striped item
            if(levelManager.destroyAnyway.Count == 4)
            {
                triggerItem.nextType = (ItemsTypes)Random.Range(1, 3); // Random horizontal or vertical
            }
            // For 5+ items, create a color bomb
            else if(levelManager.destroyAnyway.Count >= 5)
            {
                triggerItem.nextType = ItemsTypes.CHOCOBOMB;
            }
        }
    }

    // Methods to handle win/lose conditions
    public void HandleGameOver()
    {
        // Game over logic
        levelManager.gameStatus = GameState.GameOver;
    }

    public void HandleWin()
    {
        // Pre-win animations and then transition to Win state
        levelManager.gameStatus = GameState.PreWinAnimations;
    }

    // Вспомогательные методы для корутин
    private void DestroyGatheredExtraItems(Item item)
    {
        if(levelManager.gatheredTypes.Count > 1)
        {
            item.DestroyHorizontal();
            item.DestroyVertical();
        }

        foreach (ItemsTypes itemType in levelManager.gatheredTypes)
        {
            if(itemType == ItemsTypes.HORIZONTAL_STRIPPED)
                item.DestroyHorizontal();
            else
                item.DestroyVertical();
        }
    }

    private bool IsAllDestoyFinished()
    {
        GameObject[] items = GameObject.FindGameObjectsWithTag("Item");

        foreach (GameObject item in items)
        {
            Item itemComponent = item.GetComponent<Item>();

            if(itemComponent == null)
            {
                return false;
            }

            if(itemComponent.destroying && !itemComponent.animationFinished)
                return false;
        }

        return true;
    }

    private bool IsAllItemsFallDown()
    {
        if(levelManager.gameStatus == GameState.PreWinAnimations)
            return true;
        GameObject[] items = GameObject.FindGameObjectsWithTag("Item");

        foreach (GameObject item in items)
        {
            Item itemComponent = item.GetComponent<Item>();

            if(itemComponent == null)
            {
                return false;
            }

            if(itemComponent.falling)
                return false;
        }

        return true;
    }

    private void CheckIngredient()
    {
        int row = levelManager.maxRows;
        List<Square> sqList = levelManager.GetBottomRow();

        foreach (Square sq in sqList)
        {
            if(sq.item != null)
            {
                if(sq.item.currentType == ItemsTypes.INGREDIENT)
                {
                    levelManager.destroyAnyway.Add(sq.item);
                }
            }
        }
    }

    public async UniTask FindMatches()
    {
        // CoroutineManager.Instance.StartManagedCoroutine("FallingDown", FallingDown());
        await levelManager.ProcessMatchesAndFalling();
    }

    public IEnumerator TimeTick()
    {
        while (true)
        {
            if(levelManager.gameStatus == GameState.Playing)
            {
                if(levelManager.limitType == LIMIT.TIME)
                {
                    levelManager.Limit--;
                    levelManager.CheckWinLose();
                }
            }

            // Выход из корутины при определенных условиях
            if(levelManager.gameStatus == GameState.Map ||
               levelManager.Limit <= 0 ||
               levelManager.gameStatus == GameState.GameOver)
            {
                yield break;
            }

            yield return new WaitForSeconds(1);
        }
    }

    // Обновляем RestartTimer в PlayingState
    public void RestartTimer()
    {
        if(levelManager.limitType == LIMIT.TIME)
        {
            // Остановить предыдущую корутину таймера, если есть
            CoroutineManager.Instance.StopManagedCoroutine("TimeTick");

            // Запустить новую корутину таймера
            CoroutineManager.Instance.StartManagedCoroutine("TimeTick", TimeTick());
        }
    }

    private bool HasPossibleMoves()
    {
        // Проверить все возможные стартовые позиции
        for (int row = 0; row < levelManager.maxRows; row++)
        {
            for (int col = 0; col < levelManager.maxCols; col++)
            {
                Square startSquare = levelManager.GetSquare(col, row);

                if(startSquare?.item != null && startSquare.item.currentType == ItemsTypes.NONE)
                {
                    // Проверить все 8 направлений от этой позиции
                    if(CanMakeLineFromPosition(col, row, startSquare.item.color))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
    private bool CanMakeLineFromPosition(int col, int row, int color)
    {
        // Horizontal (→ & ←)
        if (1 
            + CountMatches(col, row,  1,  0, color) 
            + CountMatches(col, row, -1,  0, color) 
            >= 3)
            return true;

        // Vertical (↓ & ↑)
        if (1 
            + CountMatches(col, row,  0,  1, color) 
            + CountMatches(col, row,  0, -1, color) 
            >= 3)
            return true;

        // Diagonal “\” (down-right & up-left)
        if (1 
            + CountMatches(col, row,  1,  1, color) 
            + CountMatches(col, row, -1, -1, color) 
            >= 3)
            return true;

        // Diagonal “/” (up-right & down-left)
        if (1 
            + CountMatches(col, row,  1, -1, color) 
            + CountMatches(col, row, -1,  1, color) 
            >= 3)
            return true;

        return false;
    }
    private int CountMatches(int startCol, int startRow, int dCol, int dRow, int color)
    {
        int count = 0;
        int c = startCol + dCol;
        int r = startRow + dRow;

        while (true)
        {
            var sq = levelManager.GetSquare(c, r);
            if (sq == null || sq.item == null) 
                break;

            // Stop on color mismatch
            if (sq.item.color != color) 
                break;

            // Optionally treat certain types as non-matchable:
            if (sq.item.currentType == ItemsTypes.INGREDIENT) 
                break;

            count++;
            c += dCol;
            r += dRow;
        }

        return count;
    }


    // private bool CanMakeLineFromPosition(int startCol, int startRow, int color)
    // {
    //     // 8 направлений: горизонталь, вертикаль, диагонали
    //     int[,] directions =
    //     {
    //         {
    //             -1, 0
    //         }, // влево
    //         {
    //             1, 0
    //         }, // вправо  
    //         {
    //             0, -1
    //         }, // вверх
    //         {
    //             0, 1
    //         }, // вниз
    //         {
    //             -1, -1
    //         }, // влево-вверх
    //         {
    //             1, -1
    //         }, // вправо-вверх
    //         {
    //             -1, 1
    //         }, // влево-вниз
    //         {
    //             1, 1
    //         } // вправо-вниз
    //     };
    //
    //     for (int dir = 0; dir < 8; dir++)
    //     {
    //         int deltaCol = directions[dir, 0];
    //         int deltaRow = directions[dir, 1];
    //
    //         if(CountItemsInDirection(startCol, startRow, deltaCol, deltaRow, color) >= 3)
    //         {
    //             return true;
    //         }
    //     }
    //
    //     return false;
    // }
    //
    // private int CountItemsInDirection(int startCol, int startRow, int deltaCol, int deltaRow, int color)
    // {
    //     int count = 1; // Считаем стартовый элемент
    //     int currentCol = startCol;
    //     int currentRow = startRow;
    //
    //     // Идем в заданном направлении
    //     while (true)
    //     {
    //         currentCol += deltaCol;
    //         currentRow += deltaRow;
    //
    //         Square square = levelManager.GetSquare(currentCol, currentRow);
    //
    //         // Проверяем границы и наличие подходящего элемента
    //         if(square?.item == null ||
    //            square.item.currentType != ItemsTypes.NONE ||
    //            square.item.color != color)
    //         {
    //             break;
    //         }
    //
    //         count++;
    //
    //         // Проверяем расстояние (как в оригинальной логике)
    //         Vector2 pos1 = new Vector2(startCol, startRow);
    //         Vector2 pos2 = new Vector2(currentCol, currentRow);
    //
    //         if(Vector2.Distance(pos1, pos2) >= ITEM_SELECTION_DISTANCE_THRESHOLD * (count - 1))
    //         {
    //             break;
    //         }
    //     }
    //
    //     return count;
    // }
}

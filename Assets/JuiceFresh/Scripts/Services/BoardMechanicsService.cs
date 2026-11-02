using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;

namespace JuiceFresh.Scripts
{
    public class BoardMechanicsService
    {
        private LevelManager levelManager;
        private UniTask _activeBoardProcess = UniTask.CompletedTask;

        public BoardMechanicsService(LevelManager levelManager)
        {
            this.levelManager = levelManager;
        }

        public async UniTask ProcessBoardAfterMatches(CancellationToken cancellationToken = default, bool freezeAnimations = true)
        {
            var previous = _activeBoardProcess;
            var completion = new UniTaskCompletionSource();

            _activeBoardProcess = completion.Task;
            await previous;

            try
            {
                await ProcessBoardAfterMatchesAsync(cancellationToken, freezeAnimations);
                completion.TrySetResult();
            }
            catch (System.Exception ex)
            {
                completion.TrySetException(ex);
                throw;
            }
        }

        public async UniTask SettleBoardGravityOnly(CancellationToken cancellationToken = default)
        {
            await _activeBoardProcess;

            int safetyIterations = 30;
            bool nearEmptySquareDetected;

            do
            {
                ProcessFallingItems();
                await UniTask.Delay(100, cancellationToken: cancellationToken);

                for (int col = 0; col < levelManager.maxCols; col++)
                {
                    for (int row = levelManager.maxRows - 1; row >= 0; row--)
                    {
                        Square square = levelManager.GetSquare(col, row);
                        if (square != null && !square.IsNone() && square.item != null)
                            square.item.StartFalling();
                    }
                }

                levelManager.GenerateNewItems();
                await UniTask.Delay(100, cancellationToken: cancellationToken);

                while (!IsAllItemsFallDown())
                    await UniTask.Delay(50, cancellationToken: cancellationToken);

                nearEmptySquareDetected = FindEmptySquares();

                while (!IsAllItemsFallDown())
                    await UniTask.Delay(50, cancellationToken: cancellationToken);

                safetyIterations--;
            }
            while (nearEmptySquareDetected && safetyIterations > 0);
        }

        async UniTask ProcessBoardAfterMatchesAsync(CancellationToken cancellationToken, bool freezeAnimations)
        {
            bool throwflower = false;
            levelManager.extraCageAddItem = 0;
            bool nearEmptySquareDetected = false;
            int combo = 0;

            List<Item> items = levelManager.GetItems();
            if (freezeAnimations)
            {
                foreach (Item item in items)
                {
                    if (item != null && !item.destroying)
                        item.StopVisualAnimations();
                }
            }

            while (true)
            {
                await UniTask.Delay(100, cancellationToken: cancellationToken); // 0.1f seconds = 100ms

                levelManager.combinedItems.Clear();
                combo = levelManager.destroyAnyway.Count;

                // Process special items
                foreach (List<Item> destroyItems in levelManager.combinedItems)
                {
                    if (levelManager.lastDraggedItem == null)
                    {
                        if (destroyItems.Count == 4)
                        {
                            if (levelManager.lastDraggedItem == null)
                                levelManager.lastDraggedItem = destroyItems[Random.Range(0, destroyItems.Count)];
                            levelManager.lastDraggedItem.nextType = (ItemsTypes)Random.Range(1, 3);
                        }

                        if (destroyItems.Count >= 5)
                        {
                            if (levelManager.lastDraggedItem == null)
                                levelManager.lastDraggedItem = destroyItems[Random.Range(0, destroyItems.Count)];
                            levelManager.lastDraggedItem.nextType = ItemsTypes.CHOCOBOMB;
                        }
                    }
                }

                // Handle extra items generation
                if (levelManager.destroyAnyway.Count >= levelManager.extraItemEvery)
                {
                    levelManager.nextExtraItems = levelManager.destroyAnyway.Count / (int)levelManager.extraItemEvery;
                }

                // Process items to be destroyed
                List<Item> itemsToDestroy = new List<Item>(levelManager.destroyAnyway);
                int destroyArrayCount = itemsToDestroy.Count;
                int iCounter = 0;
                foreach (Item item in itemsToDestroy)
                {
                    iCounter++;
                    if (item.nextType == ItemsTypes.NONE)
                    {
                        if (item.square.IsCageGoingToBroke())
                        {
                            if (iCounter == destroyArrayCount)
                            {
                                DestroyGatheredExtraItems(item);
                            }

                            if (iCounter % levelManager.extraItemEvery == 0)
                            {
                                levelManager.startPosFlowers.Add(item.transform.position);
                                List<Item> itemsRand = levelManager.GetRandomItems(1);
                                int cc = 0;
                                foreach (Item item1 in itemsRand)
                                {
                                    levelManager.DragBlocked = true;
                                    throwflower = true;
                                    GameObject flowerParticle = levelManager.GetFlowerFromPool();
                                    flowerParticle.GetComponent<Flower>().StartFly(item.transform.position);
                                    cc++;
                                }
                            }

                            await UniTask.Delay(30, cancellationToken: cancellationToken); // 0.03f seconds = 30ms
                            item.DestroyItem(true, "", true);
                        }
                        else
                        {
                            if (iCounter == destroyArrayCount)
                            {
                                DestroyGatheredExtraItems(item);
                            }

                            item.SleepItem();
                        }
                    }
                }

                levelManager.destroyAnyway.Clear();

                // Process special items after destruction
                if (levelManager.lastDraggedItem != null)
                {
                    if (levelManager.lastDraggedItem.nextType != ItemsTypes.NONE)
                    {
                        await UniTask.Delay(500, cancellationToken: cancellationToken); // 0.5f seconds = 500ms
                    }

                    levelManager.lastDraggedItem = null;
                }

                // Wait for all destroy animations to finish
                await WaitUntilDestroyFinished(cancellationToken);

                // Process falling items
                ProcessFallingItems();

                if (!nearEmptySquareDetected)
                    await UniTask.Delay(200, cancellationToken: cancellationToken); // 0.2f seconds = 200ms

                // Check for ingredients
                CheckIngredient();

                // Start falling animations
                for (int col = 0; col < levelManager.maxCols; col++)
                {
                    for (int row = levelManager.maxRows - 1; row >= 0; row--)
                    {
                        if (levelManager.GetSquare(col, row) != null && !levelManager.GetSquare(col, row).IsNone() &&
                            levelManager.GetSquare(col, row).item != null)
                        {
                            levelManager.GetSquare(col, row).item.StartFalling();
                        }
                    }
                }

                await UniTask.Delay(200, cancellationToken: cancellationToken); // 0.2f seconds = 200ms

                // Generate new items
                levelManager.GenerateNewItems();
                await UniTask.Delay(100, cancellationToken: cancellationToken); // 0.1f seconds = 100ms

                // Wait for all items to fall
                while (!IsAllItemsFallDown())
                {
                    await UniTask.Delay(100, cancellationToken: cancellationToken); // 0.1f seconds = 100ms
                }

                // Check for empty squares that need items to fall into
                nearEmptySquareDetected = FindEmptySquares();

                while (!IsAllItemsFallDown())
                {
                    await UniTask.Delay(100, cancellationToken: cancellationToken); // 0.1f seconds = 100ms
                }

                if (levelManager.destroyAnyway.Count > 0)
                    nearEmptySquareDetected = true;

                if (!nearEmptySquareDetected)
                    break;
            }

            // Clean up any invalid items
            CleanupInvalidItems(items);

            // Process thriving blocks
            ProcessThrivingBlocks();

            // Check win/lose condition
            if (levelManager.gameStatus == GameState.Playing && !levelManager.ingredientFly)
                levelManager.CheckWinLose();

            if (levelManager.gameStatus == GameState.PreWinAnimations)
                levelManager.ResumeBoardAmbientAnimations();

            // Show achievement text based on combo
            ShowComboText(combo);

            // Reset state variables
            levelManager.nextExtraItems = 0;
            levelManager.gatheredTypes.Clear();
            levelManager.startPosFlowers.Clear();

            // IMPORTANT: This is where dragBlocked gets reset
            levelManager.DragBlocked = false;

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken); // WaitForEndOfFrame equivalent

            // Update bombs
            levelManager.GameField.BroadcastMessage("BombTick");

            // Save bomb timers
            UpdateBombTimers();

            // Check for possible combines in playing state
            if (levelManager.gameStatus == GameState.Playing)
            {
                // Convert the coroutine call to UniTask as well
                // _ = TipsManager.THIS.CheckPossibleCombinesAsync(cancellationToken);
            }
        }

        public IEnumerator ProcessBoardAfterMatches()
        {
            bool throwflower = false;
            levelManager.extraCageAddItem = 0;
            bool nearEmptySquareDetected = false;
            int combo = 0;

            // Freeze animations of items
            List<Item> items = levelManager.GetItems();
            foreach (Item item in items)
            {
                if (item != null && !item.destroying)
                {
                    item.StopVisualAnimations();
                }
            }

            while (true)
            {
                yield return new WaitForSeconds(0.1f);

                levelManager.combinedItems.Clear();
                combo = levelManager.destroyAnyway.Count;

                // Process special items
                foreach (List<Item> destroyItems in levelManager.combinedItems)
                {
                    if (levelManager.lastDraggedItem == null)
                    {
                        if (destroyItems.Count == 4)
                        {
                            if (levelManager.lastDraggedItem == null)
                                levelManager.lastDraggedItem = destroyItems[Random.Range(0, destroyItems.Count)];
                            levelManager.lastDraggedItem.nextType = (ItemsTypes)Random.Range(1, 3);
                        }

                        if (destroyItems.Count >= 5)
                        {
                            if (levelManager.lastDraggedItem == null)
                                levelManager.lastDraggedItem = destroyItems[Random.Range(0, destroyItems.Count)];
                            levelManager.lastDraggedItem.nextType = ItemsTypes.CHOCOBOMB;
                        }
                    }
                }

                // Handle extra items generation
                if (levelManager.destroyAnyway.Count >= levelManager.extraItemEvery)
                {
                    levelManager.nextExtraItems = levelManager.destroyAnyway.Count / (int)levelManager.extraItemEvery;
                }

                // Process items to be destroyed
                List<Item> itemsToDestroy = new List<Item>(levelManager.destroyAnyway);
                int destroyArrayCount = itemsToDestroy.Count;
                int iCounter = 0;
                foreach (Item item in itemsToDestroy)
                {
                    iCounter++;
                    if (item.nextType == ItemsTypes.NONE)
                    {
                        if (item.square.IsCageGoingToBroke())
                        {
                            if (iCounter == destroyArrayCount)
                            {
                                DestroyGatheredExtraItems(item);
                            }

                            if (iCounter % levelManager.extraItemEvery == 0)
                            {
                                levelManager.startPosFlowers.Add(item.transform.position);
                                List<Item> itemsRand = levelManager.GetRandomItems(1);
                                int cc = 0;
                                foreach (Item item1 in itemsRand)
                                {
                                    levelManager.DragBlocked = true;
                                    throwflower = true;
                                    GameObject flowerParticle = levelManager.GetFlowerFromPool();
                                    flowerParticle.GetComponent<Flower>().StartFly(item.transform.position);
                                    cc++;
                                }
                            }

                            yield return new WaitForSeconds(0.03f);
                            item.DestroyItem(true, "", true);
                        }
                        else
                        {
                            if (iCounter == destroyArrayCount)
                            {
                                DestroyGatheredExtraItems(item);
                            }

                            item.SleepItem();
                        }
                    }
                }

                levelManager.destroyAnyway.Clear();

                // Process special items after destruction
                if (levelManager.lastDraggedItem != null)
                {
                    if (levelManager.lastDraggedItem.nextType != ItemsTypes.NONE)
                    {
                        yield return new WaitForSeconds(0.5f);
                    }

                    levelManager.lastDraggedItem = null;
                }

                // Wait for all destroy animations to finish
                float destroyWaitElapsed = 0f;
                while (!IsAllDestroyFinished())
                {
                    destroyWaitElapsed += 0.1f;
                    if (destroyWaitElapsed >= 2f)
                    {
                        ForceFinishStuckDestroyingItems();
                        break;
                    }

                    yield return new WaitForSeconds(0.1f);
                }

                // Process falling items
                ProcessFallingItems();

                if (!nearEmptySquareDetected)
                    yield return new WaitForSeconds(0.2f);

                // Check for ingredients
                CheckIngredient();

                // Start falling animations
                for (int col = 0; col < levelManager.maxCols; col++)
                {
                    for (int row = levelManager.maxRows - 1; row >= 0; row--)
                    {
                        if (levelManager.GetSquare(col, row) != null && !levelManager.GetSquare(col, row).IsNone() &&
                            levelManager.GetSquare(col, row).item != null)
                        {
                            levelManager.GetSquare(col, row).item.StartFalling();
                        }
                    }
                }

                yield return new WaitForSeconds(0.2f);

                // Generate new items
                levelManager.GenerateNewItems();
                yield return new WaitForSeconds(0.1f);

                // Wait for all items to fall
                while (!IsAllItemsFallDown())
                {
                    yield return new WaitForSeconds(0.1f);
                }

                // Check for empty squares that need items to fall into
                nearEmptySquareDetected = FindEmptySquares();

                while (!IsAllItemsFallDown())
                {
                    yield return new WaitForSeconds(0.1f);
                }

                if (levelManager.destroyAnyway.Count > 0)
                    nearEmptySquareDetected = true;

                if (!nearEmptySquareDetected)
                    break;
            }

            // Clean up any invalid items
            CleanupInvalidItems(items);

            // Process thriving blocks
            ProcessThrivingBlocks();

            // Check win/lose condition
            if (levelManager.gameStatus == GameState.Playing && !levelManager.ingredientFly)
                levelManager.CheckWinLose();

            // Show achievement text based on combo
            ShowComboText(combo);

            // Reset state variables
            levelManager.nextExtraItems = 0;
            levelManager.gatheredTypes.Clear();
            levelManager.startPosFlowers.Clear();

            // IMPORTANT: This is where dragBlocked gets reset
            levelManager.DragBlocked = false;

            yield return new WaitForEndOfFrame();

            // Update bombs
            levelManager.GameField.BroadcastMessage("BombTick");

            // Save bomb timers
            UpdateBombTimers();

            // Check for possible combines in playing state
            if (levelManager.gameStatus == GameState.Playing)
                CoroutineManager.Instance.StartManagedCoroutine("CheckPossibleCombines",
                    TipsManager.THIS.CheckPossibleCombines());
        }

        // Helper methods from the original FallingDown implementation
        private void DestroyGatheredExtraItems(Item item)
        {
            if (levelManager.gatheredTypes.Count > 1)
            {
                item.DestroyHorizontal();
                item.DestroyVertical();
            }

            foreach (ItemsTypes itemType in levelManager.gatheredTypes)
            {
                if (itemType == ItemsTypes.HORIZONTAL_STRIPPED)
                    item.DestroyHorizontal();
                else
                    item.DestroyVertical();
            }
        }

        private async UniTask WaitUntilDestroyFinished(CancellationToken cancellationToken)
        {
            const int maxWaitMs = 2000;
            int waitedMs = 0;
            while (!IsAllDestroyFinished())
            {
                if (waitedMs >= maxWaitMs)
                {
                    ForceFinishStuckDestroyingItems();
                    break;
                }

                await UniTask.Delay(100, cancellationToken: cancellationToken);
                waitedMs += 100;
            }
        }

        private bool IsAllDestroyFinished()
        {
            GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
            foreach (GameObject item in items)
            {
                Item itemComponent = item.GetComponent<Item>();
                if (itemComponent == null || (itemComponent.destroying && !itemComponent.animationFinished))
                    return false;
            }

            return true;
        }

        private void ForceFinishStuckDestroyingItems()
        {
            GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
            foreach (GameObject item in items)
            {
                Item itemComponent = item.GetComponent<Item>();
                if (itemComponent == null)
                {
                    continue;
                }

                if (itemComponent.destroying && !itemComponent.animationFinished)
                {
                    itemComponent.SetAnimationDestroyingFinished();
                }
            }
        }

        private void ProcessFallingItems()
        {
            for (int i = 0; i < 20; i++)
            {
                for (int col = 0; col < levelManager.maxCols; col++)
                {
                    for (int row = levelManager.maxRows - 1; row >= 0; row--)
                    {
                        if (levelManager.GetSquare(col, row) != null)
                            levelManager.GetSquare(col, row).FallOut();
                    }
                }
            }
        }

        private void CheckIngredient()
        {
            List<Square> sqList = levelManager.GetBottomRow();
            foreach (Square sq in sqList)
            {
                if (sq.item != null && sq.item.currentType == ItemsTypes.INGREDIENT)
                {
                    levelManager.destroyAnyway.Add(sq.item);
                }
            }
        }

        private bool IsAllItemsFallDown()
        {
            GameObject[] items = GameObject.FindGameObjectsWithTag("Item");
            foreach (GameObject item in items)
            {
                Item itemComponent = item.GetComponent<Item>();
                if (itemComponent == null || itemComponent.falling)
                    return false;
            }

            return true;
        }

        private bool FindEmptySquares()
        {
            bool nearEmptySquareDetected = false;
            for (int col = 0; col < levelManager.maxCols; col++)
            {
                for (int row = levelManager.maxRows - 1; row >= 0; row--)
                {
                    Square square = levelManager.GetSquare(col, row);
                    if (square != null && !square.IsNone() && square.item != null)
                    {
                        if (square.item.GetNearEmptySquares())
                            nearEmptySquareDetected = true;
                    }
                }
            }

            return nearEmptySquareDetected;
        }

        private void CleanupInvalidItems(List<Item> originalItems)
        {
            List<Item> currentItems = levelManager.GetItems();
            for (int i = 0; i < originalItems.Count; i++)
            {
                if (currentItems.Count > i)
                {
                    Item item = currentItems[i];
                    if (item != null && item != item.square.item)
                    {
                        Object.Destroy(item.gameObject);
                    }
                }
            }
        }

        private void ProcessThrivingBlocks()
        {
            if (!levelManager.thrivingBlockDestroyed)
            {
                bool thrivingBlockSelected = false;
                for (int col = 0; col < levelManager.maxCols && !thrivingBlockSelected; col++)
                {
                    for (int row = levelManager.maxRows - 1; row >= 0 && !thrivingBlockSelected; row--)
                    {
                        Square square = levelManager.GetSquare(col, row);
                        if (square != null && square.type == SquareTypes.THRIVING)
                        {
                            List<Square> sqList = levelManager.GetSquaresAround(square);
                            foreach (Square sq in sqList)
                            {
                                if (sq.CanFallInto() && Random.Range(0, 5) == 0 && sq.type == SquareTypes.EMPTY &&
                                    sq.item != null && sq.item.currentType == ItemsTypes.NONE)
                                {
                                    levelManager.CreateObstacles(sq.col, sq.row, sq.gameObject, SquareTypes.THRIVING);
                                    thrivingBlockSelected = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            levelManager.thrivingBlockDestroyed = false;
        }

        private void ShowComboText(int combo)
        {
            if (levelManager.gameStatus != GameState.Playing)
                return;

            ComboTextFeedbackView.ShowForCombo(combo);
            GameFeelManager.Instance?.OnCombo(combo);
        }

        private void UpdateBombTimers()
        {
            List<Item> items = levelManager.GetItems();
            levelManager.bombTimers.Clear();
            foreach (Item item in items)
            {
                if (item.currentType == ItemsTypes.BOMB)
                    levelManager.bombTimers.Add(item.bombTimer);
            }
        }
    }
}

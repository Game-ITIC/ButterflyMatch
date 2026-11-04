using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JuiceFresh.Board;
using UI;
using UnityEngine;

namespace JuiceFresh.Scripts
{
    /// <summary>
    /// Runs the cascade after a move: destroy the chain, let the board fall and refill, repeat while new
    /// matches or holes appear, then check win/lose and unlock input. Passes are serialized.
    /// </summary>
    public class BoardMechanicsService
    {
        private readonly LevelManager levelManager;
        private readonly BoardGravityService _gravity;
        private readonly DestroyCompletionWaiter _destroyWaiter = new DestroyCompletionWaiter();
        private readonly ThrivingBlockSpreader _thriving;
        private UniTask _activeBoardProcess = UniTask.CompletedTask;

        // Bomb timers count player moves. One move can run several board passes (every 6th piece of a chain
        // sends a flower that triggers another pass), which used to tick the bombs 2-3 times per move.
        private int _lastBombTickMove; // moveID starts at 0: nothing ticks before the first move

        private void TickBombsOncePerMove()
        {
            if (levelManager.GameField == null || _lastBombTickMove == levelManager.moveID)
                return;

            _lastBombTickMove = levelManager.moveID;
            levelManager.GameField.BroadcastMessage("BombTick");
        }

        public BoardMechanicsService(LevelManager levelManager)
        {
            this.levelManager = levelManager;
            _gravity = new BoardGravityService(levelManager);
            _thriving = new ThrivingBlockSpreader(levelManager);
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
            catch (System.OperationCanceledException)
            {
                completion.TrySetResult();
                throw;
            }
            catch (System.Exception ex)
            {
                // Never leave the board locked: a failed pass used to keep DragBlocked = true and, stored in
                // _activeBoardProcess, made every later move throw too (the level froze until restart).
                Debug.LogException(ex);
                completion.TrySetResult();
                if (levelManager != null)
                {
                    levelManager.destroyAnyway.Clear();
                    levelManager.gatheredTypes.Clear();
                    levelManager.DragBlocked = false;
                }
            }
        }

        public async UniTask SettleBoardGravityOnly(CancellationToken cancellationToken = default)
        {
            await _activeBoardProcess;

            int safetyIterations = 30;
            bool slid;

            do
            {
                _gravity.ApplyFalls();
                await UniTask.Delay(100, cancellationToken: cancellationToken);

                _gravity.StartFallingAll();
                _gravity.Refill();
                await UniTask.Delay(100, cancellationToken: cancellationToken);

                await _gravity.WaitAllLanded(50, cancellationToken);
                slid = _gravity.SlideIntoHoles();
                await _gravity.WaitAllLanded(50, cancellationToken);

                safetyIterations--;
            }
            while (slid && safetyIterations > 0);
        }

        async UniTask ProcessBoardAfterMatchesAsync(CancellationToken cancellationToken, bool freezeAnimations)
        {
            levelManager.extraCageAddItem = 0;
            bool repeatPass = false;
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
                await UniTask.Delay(100, cancellationToken: cancellationToken);

                levelManager.combinedItems.Clear();
                combo = levelManager.destroyAnyway.Count;

                // Every extraItemEvery pieces of the chain give one extra item (flower).
                if (levelManager.destroyAnyway.Count >= levelManager.extraItemEvery)
                {
                    levelManager.nextExtraItems = levelManager.destroyAnyway.Count / (int)levelManager.extraItemEvery;
                }

                await DestroyChain(cancellationToken);

                levelManager.destroyAnyway.Clear();

                // A special piece is being created from the chain: give its transform animation time.
                if (levelManager.lastDraggedItem != null)
                {
                    if (levelManager.lastDraggedItem.nextType != ItemsTypes.NONE)
                    {
                        await UniTask.Delay(500, cancellationToken: cancellationToken);
                    }

                    levelManager.lastDraggedItem = null;
                }

                await _destroyWaiter.WaitAsync(cancellationToken);

                _gravity.ApplyFalls();

                if (!repeatPass)
                    await UniTask.Delay(200, cancellationToken: cancellationToken);

                // Ingredients that reached the bottom row are collected in the next pass.
                CheckIngredient();

                _gravity.StartFallingAll();
                await UniTask.Delay(200, cancellationToken: cancellationToken);

                _gravity.Refill();
                await UniTask.Delay(100, cancellationToken: cancellationToken);

                await _gravity.WaitAllLanded(100, cancellationToken);
                repeatPass = _gravity.SlideIntoHoles();
                await _gravity.WaitAllLanded(100, cancellationToken);

                if (levelManager.destroyAnyway.Count > 0)
                    repeatPass = true;

                if (!repeatPass)
                    break;
            }

            CleanupInvalidItems(items);

            _thriving.SpreadAfterMove();

            if (levelManager.gameStatus == GameState.Playing && !levelManager.ingredientFly)
                levelManager.CheckWinLose();

            if (levelManager.gameStatus == GameState.PreWinAnimations)
                levelManager.ResumeBoardAmbientAnimations();

            ShowComboText(combo);

            levelManager.nextExtraItems = 0;
            levelManager.gatheredTypes.Clear();
            levelManager.startPosFlowers.Clear();

            // Input unlocks here.
            levelManager.DragBlocked = false;

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, cancellationToken); // WaitForEndOfFrame equivalent

            TickBombsOncePerMove();
            levelManager.TopUpBombs();
            UpdateBombTimers();
        }

        private async UniTask DestroyChain(CancellationToken cancellationToken)
        {
            List<Item> itemsToDestroy = new List<Item>(levelManager.destroyAnyway);
            int destroyArrayCount = itemsToDestroy.Count;
            int iCounter = 0;
            Item lastAlive = null;
            foreach (Item item in itemsToDestroy)
            {
                iCounter++;
                // A striped piece earlier in this chain may already have blown this one up (the loop awaits
                // between pieces); touching it then threw and left the board DragBlocked for good.
                if (item == null || item.square == null)
                {
                    if (iCounter == destroyArrayCount)
                        DestroyGatheredExtraItems(lastAlive);
                    continue;
                }

                lastAlive = item;
                if (item.nextType != ItemsTypes.NONE)
                    continue;

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
                        foreach (Item unused in itemsRand)
                        {
                            levelManager.DragBlocked = true;
                            GameObject flowerParticle = levelManager.GetFlowerFromPool();
                            flowerParticle.GetComponent<Flower>().StartFly(item.transform.position);
                        }
                    }

                    await UniTask.Delay(30, cancellationToken: cancellationToken);
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

        private void DestroyGatheredExtraItems(Item item)
        {
            if (item == null)
                return;

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

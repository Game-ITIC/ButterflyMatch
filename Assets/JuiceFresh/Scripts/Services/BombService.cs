using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JuiceFresh.Scripts
{
    /// <summary>
    /// Bomb levels (Target.BOMBS): puts the level's bombs on the board, keeps their count topped up,
    /// ticks their timers once per player move and remembers the timers for a board regeneration.
    /// </summary>
    public sealed class BombService
    {
        private readonly LevelManager _levelManager;
        private readonly Func<int> _bombsInFlight;

        // moveID starts at 0: nothing ticks before the first move.
        private int _lastTickedMove;

        public BombService(LevelManager levelManager, Func<int> bombsInFlight)
        {
            _levelManager = levelManager;
            _bombsInFlight = bombsInFlight;
        }

        /// <summary>
        /// Turns random pieces into the bombs that are still missing. Only tops up what is missing,
        /// so it is safe to run on every entry into Playing.
        /// </summary>
        public IEnumerator InitBombs()
        {
            yield return new WaitUntil(() => !TipsManager.THIS.gotTip);
            yield return new WaitForSeconds(1);

            int bombsOnField = 0;
            foreach (Item item in _levelManager.GetItems())
            {
                if (IsBombOrBecomingBomb(item))
                    bombsOnField++;
            }

            List<Item> itemsRand = _levelManager.GetRandomItems(
                _levelManager.bombsCollect - bombsOnField - _levelManager.TargetBombs);

            int i = 0;
            foreach (Item item in itemsRand)
            {
                item.nextType = ItemsTypes.BOMB;

                // Timers saved before a board regeneration go back onto the new bombs.
                if (i < _levelManager.bombTimers.Count)
                    item.bombTimer = _levelManager.bombTimers[i];
                i++;
                item.ChangeType();
            }
        }

        /// <summary>
        /// After a move: puts back bombs that were lost (a piece marked to become a bomb was destroyed
        /// before it turned), so a bomb level can always reach its target.
        /// </summary>
        public void TopUp()
        {
            if (_levelManager.target != Target.BOMBS || _levelManager.gameStatus != GameState.Playing)
                return;

            int onField = 0;
            foreach (Item item in _levelManager.GetItems())
            {
                if (item != null && !item.destroying && IsBombOrBecomingBomb(item))
                    onField++;
            }

            int missing = _levelManager.bombsCollect - _levelManager.TargetBombs - _bombsInFlight() - onField;
            foreach (Item item in _levelManager.GetRandomItems(missing))
            {
                item.nextType = ItemsTypes.BOMB;
                item.ChangeType();
            }
        }

        /// <summary>
        /// Bomb timers count player moves. One move can run several board passes (every 6th piece of a chain
        /// sends a flower that triggers another pass), which used to tick the bombs 2-3 times per move.
        /// </summary>
        public void TickOncePerMove()
        {
            if (_levelManager.GameField == null || _lastTickedMove == _levelManager.moveID)
                return;

            _lastTickedMove = _levelManager.moveID;
            _levelManager.GameField.BroadcastMessage("BombTick");
        }

        /// <summary>Remembers the timers of the bombs on the board (InitBombs restores them).</summary>
        public void SnapshotTimers()
        {
            _levelManager.bombTimers.Clear();
            foreach (Item item in _levelManager.GetItems())
            {
                if (item.currentType == ItemsTypes.BOMB)
                    _levelManager.bombTimers.Add(item.bombTimer);
            }
        }

        private static bool IsBombOrBecomingBomb(Item item) =>
            item.currentType == ItemsTypes.BOMB || item.nextType == ItemsTypes.BOMB;
    }
}

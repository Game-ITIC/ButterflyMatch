using System.Collections;
using UnityEngine;

namespace JuiceFresh.Scripts
{
    /// <summary>
    /// Countdown of timed levels (LIMIT.TIME): takes one second off the limit per second while Playing.
    /// Runs on CoroutineManager, so LevelManager state changes (which stop its coroutines) don't kill it.
    /// </summary>
    public sealed class LevelTimer
    {
        private const string CoroutineKey = "TimeTick";

        private readonly LevelManager _levelManager;

        public LevelTimer(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }

        public void Restart()
        {
            if (_levelManager.limitType != LIMIT.TIME)
                return;

            CoroutineManager.Instance.StartManagedCoroutine(CoroutineKey, Tick());
        }

        public void Stop()
        {
            CoroutineManager.ExistingInstance?.StopManagedCoroutine(CoroutineKey);
        }

        private IEnumerator Tick()
        {
            var oneSecond = new WaitForSeconds(1);
            while (true)
            {
                if (_levelManager.gameStatus == GameState.Playing && _levelManager.limitType == LIMIT.TIME)
                {
                    _levelManager.Limit--;
                    _levelManager.CheckWinLose();
                }

                if (_levelManager.gameStatus == GameState.Map ||
                    _levelManager.Limit <= 0 ||
                    _levelManager.gameStatus == GameState.GameOver)
                {
                    yield break;
                }

                yield return oneSecond;
            }
        }
    }
}

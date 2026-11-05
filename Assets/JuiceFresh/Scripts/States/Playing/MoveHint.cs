using System.Collections.Generic;
using UnityEngine;

namespace JuiceFresh.States.Playing
{
    /// <summary>
    /// When the player has been idle for a while, shows a chain they can drag: its pieces pulse and its cells
    /// light up. Any touch or move hides it and restarts the wait.
    /// </summary>
    public sealed class MoveHint
    {
        private const float FirstDelay = 5f;
        private const float RepeatDelay = 6f;
        private const float MarkTime = 1.2f;

        private readonly PossibleMovesDetector _detector;
        private readonly List<Item> _chain = new List<Item>();

        private bool _waiting;
        private float _showAt;
        private float _hideAt;

        public MoveHint(PossibleMovesDetector detector)
        {
            _detector = detector;
        }

        public void Tick(bool playerIsIdle)
        {
            if (!playerIsIdle)
            {
                _waiting = false;
                HideMarks();
                return;
            }

            if (!_waiting)
            {
                _waiting = true;
                _showAt = Time.time + FirstDelay;
                return;
            }

            if (_chain.Count > 0 && Time.time >= _hideAt)
                HideMarks();

            if (Time.time >= _showAt)
            {
                _showAt = Time.time + RepeatDelay;
                Show();
            }
        }

        private void Show()
        {
            if (!_detector.TryFindChain(_chain))
                return;

            _hideAt = Time.time + MarkTime;
            foreach (Item item in _chain)
            {
                item.PlayHintAnimation();
                item.square.SetChainMark(true);
            }
        }

        private void HideMarks()
        {
            foreach (Item item in _chain)
            {
                // A piece of the chain may have been played and destroyed already.
                if (item != null && item.square != null)
                    item.square.SetChainMark(false);
            }

            _chain.Clear();
        }
    }
}

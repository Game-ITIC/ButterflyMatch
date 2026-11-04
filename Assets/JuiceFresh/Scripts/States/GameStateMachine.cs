using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuiceFresh.States
{
    /// <summary>
    /// Owns the game states of a level and switches between them.
    /// States without a class (Pause, PreFailed, ...) are handed to a fallback and keep the current state object.
    /// </summary>
    public sealed class GameStateMachine : IDisposable
    {
        private readonly Dictionary<GameState, GameStateBase> _states;
        private readonly Action _beforeEnter;
        private readonly Action<GameState> _stateWithoutClass;

        private GameStateBase _current;

        /// <param name="states">State objects by game state.</param>
        /// <param name="beforeEnter">Runs after the old state exits and before the new one enters.</param>
        /// <param name="stateWithoutClass">Handles game states that have no state object.</param>
        /// <param name="initialStatus">Status before the first transition.</param>
        public GameStateMachine(
            Dictionary<GameState, GameStateBase> states,
            Action beforeEnter,
            Action<GameState> stateWithoutClass,
            GameState initialStatus = default)
        {
            _states = states;
            _beforeEnter = beforeEnter;
            _stateWithoutClass = stateWithoutClass;
            Status = initialStatus;
        }

        public GameState Status { get; private set; }

        /// <summary>The last state object entered (unchanged by states without a class).</summary>
        public GameStateBase Current => _current;

        public T Get<T>() where T : GameStateBase
        {
            foreach (GameStateBase state in _states.Values)
            {
                if (state is T typed)
                    return typed;
            }

            return null;
        }

        public void TransitionTo(GameState next)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("Changing game state from " + Status + " to " + next);
#endif
            _current?.ExitState();
            _beforeEnter?.Invoke();

            Status = next;

            if (_states.TryGetValue(next, out GameStateBase state))
            {
                _current = state;
                _current.EnterState();
            }
            else
            {
                _stateWithoutClass?.Invoke(next);
            }
        }

        public void Update()
        {
            _current?.UpdateState();
        }

        public void Dispose()
        {
            foreach (GameStateBase state in _states.Values)
                (state as IDisposable)?.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Infrastructure.GameStates
{
    public class SimpleStateMachine<T> : IGameStateMachine<T>, IPayloadStateMachine<T>
    {

        private T _currentState;
        private Dictionary<Type, T> _states = new();

        public void Register(T state)
        {
            Debug.Log($" Register state {state.GetType().Name}");
            _states.Add(state.GetType(), state);
        }

        public void Enter<TState>() where TState : class, T, IState
        {
            Debug.Log($"Tryint to change state from  {_currentState?.GetType().Name} to {typeof(TState).Name}");
            IState state = ChangeCurrentState<TState>();
            state.Enter();
        }

        public void Enter<TState, TPayload>(TPayload payload) where TState : class, T, IPayloadState<TPayload>
        {
            TState state = ChangeCurrentState<TState>();
            state.Enter(payload);
        }

        private TState ChangeCurrentState<TState>() where TState : class, T, IExitableState
        {
            if (_currentState is IExitableState state)
                state.Exit();

            var newState = _states[typeof(TState)] as TState;
            _currentState = newState;
            return newState;
        }

    }

}
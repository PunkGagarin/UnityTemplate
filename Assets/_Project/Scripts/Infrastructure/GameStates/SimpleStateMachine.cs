using _Project.Scripts.Infrastructure.GameStates.Factory;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace _Project.Scripts.Infrastructure.GameStates
{
    public class SimpleStateMachine<T> : IGameStateMachine<T>, IPayloadStateMachine<T>
    {
        private T _currentState;

        [Inject] private IStateFactory _stateFactory;

        protected T CurrentState => _currentState;

        public async void Enter<TState>() where TState : class, T, IState
        {
            Debug.Log($"Trying to change state from {_currentState?.GetType().Name} to {typeof(TState).Name}");
            IState state = await ChangeCurrentState<TState>();
            state.Enter();
        }

        public async void Enter<TState, TPayload>(TPayload payload) where TState : class, T, IPayloadState<TPayload>
        {
            TState state = await ChangeCurrentState<TState>();
            state.Enter(payload);
        }

        private async UniTask<TState> ChangeCurrentState<TState>() where TState : class, T, IExitableState
        {
            if (_currentState is IDeferredExitState deferredExitState)
            {
                deferredExitState.BeginExit();
                await UniTask.WaitUntil(() => deferredExitState.ExitCompleted);
                deferredExitState.EndExit();
            }
            else if (_currentState is IExitableState state)
            {
                state.Exit();
            }

            var newState = _stateFactory.GetState<TState>();
            _currentState = newState;
            return newState;
        }
    }
}

namespace _Project.Scripts.Infrastructure.GameStates
{
    public interface IGameStateMachine<T>
    {
        void Enter<TState>() where TState : class, T, IState;
    }

    public interface IPayloadStateMachine<in T>
    {
        void Enter<TState, TPayload>(TPayload payload) where TState : class, T, IPayloadState<TPayload>;
    }
}

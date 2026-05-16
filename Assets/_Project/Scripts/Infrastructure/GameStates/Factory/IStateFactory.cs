using _Project.Scripts.Infrastructure.GameStates;

namespace _Project.Scripts.Infrastructure.GameStates.Factory
{
    public interface IStateFactory
    {
        TState GetState<TState>() where TState : class, IExitableState;
    }
}

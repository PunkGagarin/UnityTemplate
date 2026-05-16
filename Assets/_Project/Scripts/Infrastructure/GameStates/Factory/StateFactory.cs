using _Project.Scripts.Infrastructure.GameStates;
using Zenject;

namespace _Project.Scripts.Infrastructure.GameStates.Factory
{
    public class StateFactory : IStateFactory
    {
        [Inject] private DiContainer _container;

        public TState GetState<TState>() where TState : class, IExitableState =>
            _container.Resolve<TState>();
    }
}

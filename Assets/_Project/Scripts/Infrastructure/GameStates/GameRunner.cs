using _Project.Scripts.Infrastructure.GameStates.States;
using Zenject;

namespace _Project.Scripts.Infrastructure.GameStates
{
    public class GameRunner : IInitializable
    {
        [Inject] private GameStateMachine _stateMachine;

        public void Initialize()
        {
            _stateMachine.Enter<BootstrapState>();
        }
    }
}

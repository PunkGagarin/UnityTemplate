using _Project.Scripts.Gameplay.Level;
using _Project.Scripts.Gameplay.Units;
using Zenject;

namespace _Project.Scripts.Infrastructure.GameStates.States
{
    public class GameplayEnterState : IState, IGameState
    {
        [Inject] private GameStateMachine _stateMachine;
        [Inject] private ILevelStartPointProvider _levelStartPointProvider;
        [Inject] private IExampleUnitFactory _exampleUnitFactory;

        public void Enter()
        {
            CreateExampleUnit();
            _stateMachine.Enter<GameplayState>();
        }

        private void CreateExampleUnit()
        {
            _exampleUnitFactory.Create(_levelStartPointProvider.StartPoint);
        }

        public void Exit()
        {
        }
    }
}

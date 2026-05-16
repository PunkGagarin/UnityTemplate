using Zenject;
using _Project.Scripts.Gameplay.Enemies;
using _Project.Scripts.Gameplay.Units;
using _Project.Scripts.Utils.Pause;

namespace _Project.Scripts.Infrastructure.GameStates.States
{
    internal class GameplayState : EndOfFrameExitState
    {
        [Inject] private IEnemySpawner _enemySpawner;
        [Inject] private IExampleUnitFactory _exampleUnitFactory;
        [Inject] private PauseService _pauseService;

        public override void Enter()
        {
            _enemySpawner.Start(_exampleUnitFactory.CurrentUnit != null
                ? _exampleUnitFactory.CurrentUnit.transform
                : null);
        }

        protected override void OnUpdate()
        {
            if (_pauseService.IsPaused)
                return;

            _enemySpawner.Update();
        }

        protected override void ExitOnEndOfFrame()
        {
            _enemySpawner.Cleanup();
            _exampleUnitFactory.Cleanup();
        }
    }
}

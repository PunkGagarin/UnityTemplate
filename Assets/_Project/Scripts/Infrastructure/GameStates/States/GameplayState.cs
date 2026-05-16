using _Project.Scripts.Gameplay.Units;
using Zenject;

namespace _Project.Scripts.Infrastructure.GameStates.States
{
    internal class GameplayState : EndOfFrameExitState
    {
        [Inject] private IExampleUnitFactory _exampleUnitFactory;

        public override void Enter()
        {
        }

        protected override void OnUpdate()
        {
        }

        protected override void ExitOnEndOfFrame()
        {
            _exampleUnitFactory.Cleanup();
        }
    }
}

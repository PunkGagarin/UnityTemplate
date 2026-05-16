using Zenject;

namespace _Project.Scripts.Infrastructure.GameStates
{
    public class GameStateMachine : SimpleStateMachine<IGameState>, ITickable
    {
        public void Tick()
        {
            if (CurrentState is IUpdateable updateable)
                updateable.Update();
        }
    }
}

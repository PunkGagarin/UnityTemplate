namespace _Project.Scripts.Infrastructure.GameStates
{



    public interface IState : IExitableState
    {
        void Enter();
    }

    public interface IExitableState
    {
        void Exit();
    }

    public interface IPayloadState<TPayload> : IExitableState
    {
        void Enter(TPayload payload);
    }
    
    public interface IGameState : IExitableState
    {
    }
}
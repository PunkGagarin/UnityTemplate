namespace _Project.Scripts.Infrastructure.GameStates
{
    public abstract class EndOfFrameExitState : IState, IGameState, IUpdateable, IDeferredExitState
    {
        private bool _exitWasRequested;

        public bool ExitCompleted { get; private set; }

        public virtual void Enter()
        {
        }

        public void Exit()
        {
            BeginExit();
            EndExit();
        }

        public void BeginExit()
        {
            _exitWasRequested = true;
            ExitCompleted = false;
        }

        public void EndExit()
        {
            ExitOnEndOfFrame();
            ClearExitRequest();
        }

        public void Update()
        {
            if (!_exitWasRequested)
                OnUpdate();

            if (_exitWasRequested)
                ExitCompleted = true;
        }

        protected virtual void OnUpdate()
        {
        }

        protected virtual void ExitOnEndOfFrame()
        {
        }

        private void ClearExitRequest()
        {
            _exitWasRequested = false;
            ExitCompleted = false;
        }
    }
}

using _Project.Scripts.Infrastructure.SceneManagement;
using _Project.Scripts.Utils.Pause;
using Zenject;

namespace _Project.Scripts.Infrastructure.GameStates.States
{
    public class LoadMainMenuState : IState, IGameState
    {
        [Inject] private SceneLoader _sceneLoader;
        [Inject] private GameStateMachine _stateMachine;
        [Inject] private LoadingCurtain _loadingCurtain;
        [Inject] private PauseService _pauseService;

        public async void Enter()
        {
            _pauseService.SetPaused(false);
            _loadingCurtain.Show();
            await _sceneLoader.LoadScene(SceneEnum.MainMenu);
            _loadingCurtain.Hide();

            _stateMachine.Enter<MainMenuState>();
        }

        public void Exit()
        {
        }
    }
}

using _Project.Scripts.Gameplay.Windows;
using _Project.Scripts.Infrastructure.GameStates;
using _Project.Scripts.Infrastructure.GameStates.States;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using _Project.Scripts.Utils.Pause;

namespace _Project.Scripts
{
    public class Restart : BaseWindow
    {
        [field: SerializeField] private GameObject Content { get; set; }

        [field: SerializeField]
        private Button RestartButton { get; set; }

        [field: SerializeField]
        private Button MainMenuButton { get; set; }

        [field: SerializeField]
        private Button CloseButton { get; set; }

        [Inject] private GameStateMachine _stateMachine;
        [Inject] private PauseService _pauseService;
        [Inject] private IWindowService _windowService;

        protected override void OnAwake()
        {
            Id = WindowId.GameplayMenuWindow;
        }

        protected override void Initialize() =>
            Content.SetActive(true);

        protected override void SubscribeUpdates()
        {
            RestartButton.onClick.AddListener(RestartGameplay);
            MainMenuButton.onClick.AddListener(OpenMainMenu);
            CloseButton.onClick.AddListener(Close);
        }

        protected override void UnsubscribeUpdates()
        {
            RestartButton.onClick.RemoveListener(RestartGameplay);
            MainMenuButton.onClick.RemoveListener(OpenMainMenu);
            CloseButton.onClick.RemoveListener(Close);
        }

        private void OpenMainMenu()
        {
            _pauseService.SetPaused(false);
            _windowService.Close(Id);
            _stateMachine.Enter<LoadMainMenuState>();
        }

        private void RestartGameplay()
        {
            _pauseService.SetPaused(false);
            _windowService.Close(Id);
            _stateMachine.Enter<LoadGameplayState>();
        }

        private void Close()
        {
            _pauseService.SetPaused(false);
            _windowService.Close(Id);
        }
    }
}

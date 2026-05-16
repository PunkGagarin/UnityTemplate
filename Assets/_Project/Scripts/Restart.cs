using _Project.Scripts.Gameplay.Windows;
using _Project.Scripts.Infrastructure.GameStates;
using _Project.Scripts.Infrastructure.GameStates.States;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace _Project.Scripts
{
    public class Restart : BaseWindow
    {
        [field: SerializeField] private GameObject Content { get; set; }

        [field: SerializeField]
        private Button RestartButton { get; set; }

        [field: SerializeField]
        private Button MainMenuButton { get; set; }

        [Inject] private GameStateMachine _stateMachine;
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
        }

        protected override void UnsubscribeUpdates()
        {
            RestartButton.onClick.RemoveListener(RestartGameplay);
            MainMenuButton.onClick.RemoveListener(OpenMainMenu);
        }

        private void OpenMainMenu()
        {
            _windowService.Close(Id);
            _stateMachine.Enter<LoadMainMenuState>();
        }

        private void RestartGameplay()
        {
            _windowService.Close(Id);
            _stateMachine.Enter<LoadGameplayState>();
        }
    }
}

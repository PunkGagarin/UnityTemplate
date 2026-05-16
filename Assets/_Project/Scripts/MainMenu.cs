using _Project.Scripts.Audio.Domain;
using _Project.Scripts.Gameplay.Windows;
using _Project.Scripts.Infrastructure.GameStates;
using _Project.Scripts.Infrastructure.GameStates.States;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace _Project.Scripts
{
    public class MainMenu : MonoBehaviour
    {
        [field: SerializeField]
        private Button StartGameButton { get; set; }

        [field: SerializeField]
        private Button SettingsButton { get; set; }

        [field: SerializeField]
        private Button CreditsButton { get; set; }

        [Inject] private GameStateMachine _stateMachine;
        [Inject] private AudioService _audio;
        [Inject] private IWindowService _windowService;

        private void Awake()
        {
            StartGameButton.onClick.AddListener(StartGame);
            SettingsButton.onClick.AddListener(OpenSettings);
            CreditsButton.onClick.AddListener(OpenCredits);
        }

        private void OnDestroy()
        {
            StartGameButton.onClick.RemoveListener(StartGame);
            SettingsButton.onClick.RemoveListener(OpenSettings);
            CreditsButton.onClick.RemoveListener(OpenCredits);
        }

        private void StartGame()
        {
            _audio.PlaySound(Sounds.buttonClick);
            _stateMachine.Enter<LoadGameplayState>();
        }

        private void OpenSettings()
        {
            _audio.PlaySound(Sounds.buttonClick);
            _windowService.Open(WindowId.SettingsWindow);
        }

        private void OpenCredits()
        {
            _audio.PlaySound(Sounds.buttonClick);
        }
    }
}

using _Project.Scripts.Audio.Domain;
using _Project.Scripts.Audio.View;
using _Project.Scripts.Infrastructure.GameStates;
using _Project.Scripts.Infrastructure.GameStates.States;
using _Project.Scripts.Infrastructure.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace _Project.Scripts
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField]
        private Button _startGame;

        [SerializeField]
        private Button _settings;

        [SerializeField]
        private Button _credits;


        [Inject] private SceneLoader _sceneLoader;
        [Inject] private GameStateMachine _stateMachine;
        [Inject] private AudioService _audio;
        [Inject] private SettingsView _settingsView;


        private void Awake()
        {
            _startGame.onClick.AddListener(StartGame);
            _settings.onClick.AddListener(OpenSettings);
            _credits.onClick.AddListener(OpenCredits);
        }

        private void OnDestroy()
        {
            _startGame.onClick.RemoveListener(StartGame);
            _settings.onClick.RemoveListener(OpenSettings);
            _credits.onClick.RemoveListener(OpenCredits);
        }

        private void StartGame()
        {
            _audio.PlaySound(Sounds.buttonClick);
            _stateMachine.Enter<LoadGameplayState>();
        }

        private void OpenSettings()
        {
            _settingsView.Open();
        }

        private void OpenCredits()
        {
        }
    }
}
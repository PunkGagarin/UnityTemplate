using _Project.Scripts.Infrastructure.GameStates;
using _Project.Scripts.Infrastructure.GameStates.States;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class Restart : MonoBehaviour
{
    
    [Inject] private  GameStateMachine _stateMachine;
    
    [SerializeField]
    private Button _restartButton;
    
    [SerializeField]
    private Button _mainMenuButton;

    private void Start()
    {
        _restartButton.onClick.AddListener(RestartGameplay);
        
        _mainMenuButton.onClick.AddListener(OpenMainMenu);
    }

    private void OpenMainMenu()
    {
        _stateMachine.Enter<MainMenuState>();
    }

    private void RestartGameplay()
    {
        _stateMachine.Enter<LoadGameplayState>();
    }
}

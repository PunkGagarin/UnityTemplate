using _Project.Scripts.Infrastructure.GameStates;
using _Project.Scripts.Infrastructure.GameStates.States;
using _Project.Scripts.Utils;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class Restart : ContentUi
{
    
    [Inject] private  GameStateMachine _stateMachine;
    
    [field: SerializeField]
    private Button RestartButton { get; set; }
    
    [field: SerializeField]
    private Button MainMenuButton{ get; set; }

    private void Start()
    {
        RestartButton.onClick.AddListener(RestartGameplay);
        
        MainMenuButton.onClick.AddListener(OpenMainMenu);
    }

    private void OpenMainMenu()
    {
        _stateMachine.Enter<LoadMainMenuState>();
    }

    private void RestartGameplay()
    {
        _stateMachine.Enter<LoadGameplayState>();
    }
}

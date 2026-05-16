using UnityEngine;
using UnityEngine.UI;
using Zenject;
using _Project.Scripts.Utils.Pause;

namespace _Project.Scripts.Gameplay.Windows
{
    public class GameplayMenuButton : MonoBehaviour
    {
        [field: SerializeField] private Button Button { get; set; }

        [Inject] private PauseService _pauseService;
        [Inject] private IWindowService _windowService;

        private void Awake() =>
            Button.onClick.AddListener(OpenGameplayMenu);

        private void OnDestroy() =>
            Button.onClick.RemoveListener(OpenGameplayMenu);

        private void OpenGameplayMenu()
        {
            _pauseService.SetPaused(true);
            _windowService.Open(WindowId.GameplayMenuWindow);
        }
    }
}

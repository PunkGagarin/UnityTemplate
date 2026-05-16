using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace _Project.Scripts.Gameplay.Windows
{
    public class GameplayMenuButton : MonoBehaviour
    {
        [field: SerializeField] private Button Button { get; set; }

        [Inject] private IWindowService _windowService;

        private void Awake() =>
            Button.onClick.AddListener(OpenGameplayMenu);

        private void OnDestroy() =>
            Button.onClick.RemoveListener(OpenGameplayMenu);

        private void OpenGameplayMenu() =>
            _windowService.Open(WindowId.GameplayMenuWindow);
    }
}

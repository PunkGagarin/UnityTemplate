using UnityEngine;
using Zenject;
using _Project.Scripts.Gameplay.Windows;

namespace _Project.Scripts.Infrastructure.Installers
{
    public class UIInitializer : MonoBehaviour, IInitializable
    {
        [field: SerializeField] private RectTransform UIRoot { get; set; }

        [Inject] private IWindowFactory _windowFactory;

        public void Initialize() =>
            _windowFactory.SetUIRoot(UIRoot);
    }
}

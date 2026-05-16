using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace _Project.Scripts.Infrastructure.SceneManagement
{
    public class SceneInitializationInstaller : MonoInstaller
    {
        [field: SerializeField] private List<MonoBehaviour> Initializers { get; set; }

        public override void InstallBindings()
        {
            foreach (MonoBehaviour initializer in Initializers)
                Container.BindInterfacesTo(initializer.GetType()).FromInstance(initializer).AsSingle();
        }
    }
}

using _Project.Scripts.Infrastructure.GameStates;
using Zenject;

namespace _Project.Scripts.Infrastructure
{
    public class BootstrapInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<GameRunner>().AsSingle().NonLazy();
        }
    }
}

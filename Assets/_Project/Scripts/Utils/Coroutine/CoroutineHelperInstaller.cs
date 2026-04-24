using Zenject;

namespace _Project.Scripts.Utils.Coroutine
{
    public class CoroutineHelperInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<CoroutineHelper>()
                .FromNewComponentOnNewGameObject()
                .WithGameObjectName("CoroutineHelper")
                .AsSingle()
                .NonLazy();
        }
    }
}

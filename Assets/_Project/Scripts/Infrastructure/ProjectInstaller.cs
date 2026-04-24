using _Project.Scripts.Infrastructure.Configs;
using _Project.Scripts.Infrastructure.GameStates.States;
using _Project.Scripts.Infrastructure.GameStates;
using UnityEngine;
using Zenject;

namespace _Project.Scripts.Infrastructure
{
    public class ProjectInstaller : MonoInstaller
    {

        public override void InstallBindings()
        {
            BindStateMachine();
            BindConfigs();
        }

        private void BindConfigs()
        {
            Container.Bind<RemoteConfigs>().AsSingle();
        }

        private void BindStateMachine()
        {
            Debug.Log(" BindStateMachine from installer");
            Container.BindInterfacesAndSelfTo<BootstrapState>().AsSingle();
            Container.BindInterfacesAndSelfTo<LoadGameplayState>().AsSingle();
            Container.BindInterfacesAndSelfTo<MainMenuState>().AsSingle();
            Container.BindInterfacesAndSelfTo<GameplayState>().AsSingle();

            Container.BindInterfacesAndSelfTo<GameStateMachine>().AsSingle().NonLazy();
        }
    }

}
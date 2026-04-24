using _Project.Scripts.Audio.View;
using UnityEngine;
using Zenject;

namespace _Project.Scripts.Audio.Domain
{
    public class SettingsInstaller : MonoInstaller
    {
        [SerializeField] private SettingsView _settingsView;

        public override void InstallBindings()
        {
            InstallSettingsView();
        }

        private void InstallSettingsView()
        {
            Container.Bind<SettingsView>().FromInstance(_settingsView).AsSingle();
        }
    }
}
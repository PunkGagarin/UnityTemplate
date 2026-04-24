using UnityEngine;
using Zenject;

namespace _Project.Scripts.Localization
{
    public class LanguageServiceInstaller : MonoInstaller
    {
        [SerializeField] private LanguageService _languageServicePrefab;

        public override void InstallBindings()
        {
            InstallLanguageModel();
            InstallLanguageService();
            InstallLocalization();
        }

        private void InstallLanguageModel()
        {
            Container.Bind<LanguageModel>()
                .FromNew()
                .AsSingle()
                .NonLazy();
        }

        private void InstallLanguageService()
        {
            Container.Bind<LanguageService>()
                .FromComponentInNewPrefab(_languageServicePrefab)
                .AsSingle()
                .NonLazy();
        }

        private void InstallLocalization()
        {
            Container.Bind<LocalizationTool>()
                .AsSingle()
                .NonLazy();
        }
    }
}
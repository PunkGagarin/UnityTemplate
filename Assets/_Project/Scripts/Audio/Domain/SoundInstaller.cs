using _Project.Scripts.Audio.Data;
using UnityEngine;
using UnityEngine.Audio;
using Zenject;

namespace _Project.Scripts.Audio.Domain
{
    public class SoundInstaller : MonoInstaller
    {
        [SerializeField]
        private AudioMixerGroup _musicMixer;

        [SerializeField]
        private AudioMixerGroup _soundMixer;

        [SerializeField]
        private SoundRepository _soundRepository;

        [SerializeField]
        private AudioService _audioService;

        public override void InstallBindings()
        {
            AudioSettingsModelInstall();
            AudioMixerInstall();
            AudioSettingsPresenterInstall();
            SoundServiceInstall();
        }

        private void AudioSettingsModelInstall()
        {
            Container.Bind<AudioSettingsModel>()
                .AsSingle()
                .NonLazy();
        }

        private void AudioMixerInstall()
        {
            Container.BindInterfacesAndSelfTo<AudioMixerService>()
                .AsSingle()
                .WithArguments(_musicMixer, _soundMixer)
                .NonLazy();
        }

        private void AudioSettingsPresenterInstall()
        {
            Container.BindInterfacesAndSelfTo<SettingsPresenter>()
                .AsSingle()
                .NonLazy();
        }

        private void SoundServiceInstall()
        {
            Container.BindInterfacesAndSelfTo<SoundRepository>()
                .FromInstance(_soundRepository)
                .AsSingle();

            Container.BindInterfacesAndSelfTo<AudioService>()
                .FromInstance(_audioService)
                .AsSingle();
        }
    }
}

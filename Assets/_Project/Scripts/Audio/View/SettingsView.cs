using _Project.Scripts.Audio.Domain;
using _Project.Scripts.Gameplay.Windows;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace _Project.Scripts.Audio.View
{
    public class SettingsView : BaseWindow
    {
        [field: SerializeField] private GameObject Content { get; set; }

        [SerializeField] private Slider _masterVolumeSlider, _musicVolumeSlider, _soundVolumeSlider;
        [SerializeField] private Button _closeButton, _applyButton;
        [SerializeField] private Toggle _enToggle, _ruToggle;
        [SerializeField] private ToggleGroup _toggleGroup;

        [Inject] private SettingsPresenter _settingsPresenter;
        [Inject] private AudioService _audioService;
        [Inject] private IWindowService _windowService;

        protected override void OnAwake()
        {
            Id = WindowId.SettingsWindow;
        }

        protected override void Initialize()
        {
            _settingsPresenter.AttachView(this);
            _settingsPresenter.OnOpen();
            Content.SetActive(true);
        }

        protected override void SubscribeUpdates()
        {
            _masterVolumeSlider.onValueChanged.AddListener(UpdateMasterVolume);
            _musicVolumeSlider.onValueChanged.AddListener(UpdateMusicVolume);
            _soundVolumeSlider.onValueChanged.AddListener(UpdateSoundVolume);
            _closeButton.onClick.AddListener(UndoChanges);
            _applyButton.onClick.AddListener(SaveSettings);
            _enToggle.onValueChanged.AddListener(OnEnToggleValueChanged);
            _ruToggle.onValueChanged.AddListener(OnRuToggleValueChanged);
        }

        protected override void UnsubscribeUpdates()
        {
            _masterVolumeSlider.onValueChanged.RemoveListener(UpdateMasterVolume);
            _musicVolumeSlider.onValueChanged.RemoveListener(UpdateMusicVolume);
            _soundVolumeSlider.onValueChanged.RemoveListener(UpdateSoundVolume);
            _closeButton.onClick.RemoveListener(UndoChanges);
            _applyButton.onClick.RemoveListener(SaveSettings);
            _enToggle.onValueChanged.RemoveListener(OnEnToggleValueChanged);
            _ruToggle.onValueChanged.RemoveListener(OnRuToggleValueChanged);
        }

        private void SaveSettings()
        {
            _audioService.PlaySound(Sounds.buttonClick.ToString());
            _settingsPresenter.SaveChanges();
            Close();
        }

        private void UndoChanges()
        {
            _audioService.PlaySound(Sounds.buttonClick.ToString());
            _settingsPresenter.UndoChanges();
            Close();
        }

        private void Close() =>
            _windowService.Close(Id);

        private void UpdateSoundVolume(float newVolume)
        {
            _audioService.PlaySoundInSingleAudioSource(Sounds.buttonClickShortHigh.ToString());
            _settingsPresenter.SetSoundVolume(newVolume);
        }

        private void UpdateMusicVolume(float newVolume)
        {
            _audioService.PlaySoundInSingleAudioSource(Sounds.buttonClickShortHigh.ToString());
            _settingsPresenter.SetMusicVolume(newVolume);
        }

        private void UpdateMasterVolume(float newVolume)
        {
            _audioService.PlaySoundInSingleAudioSource(Sounds.buttonClickShortHigh.ToString());
            _settingsPresenter.SetMasterVolume(newVolume);
        }

        private void OnEnToggleValueChanged(bool isOn)
        {
            _audioService.PlaySound(Sounds.buttonClick.ToString());
            _settingsPresenter.OnEnToggleValueChanged(isOn);
        }

        private void OnRuToggleValueChanged(bool isOn)
        {
            _audioService.PlaySound(Sounds.buttonClick.ToString());
            _settingsPresenter.OnRuToggleValueChanged(isOn);
        }

        public void SetMasterVolume(float masterVolume) =>
            _masterVolumeSlider.value = masterVolume;

        public void SetSoundVolume(float soundVolume) =>
            _soundVolumeSlider.value = soundVolume;

        public void SetMusicVolume(float musicVolume) =>
            _musicVolumeSlider.value = musicVolume;

        public void EnableRuToggle(bool isOn)
        {
            _ruToggle.SetIsOnWithoutNotify(isOn);
            _toggleGroup.allowSwitchOff = false;
        }

        public void EnableEnToggle(bool isOn)
        {
            _enToggle.SetIsOnWithoutNotify(isOn);
            _toggleGroup.allowSwitchOff = false;
        }
    }
}

using _Project.Scripts.Audio.Domain;
using _Project.Scripts.Gameplay.Windows;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace _Project.Scripts.Audio.View
{
    public class CreditsPopup : BaseWindow
    {
        [SerializeField] private Button _closeButton;

        [Inject] private AudioService _audioService;
        [Inject] private IWindowService _windowService;

        protected override void OnAwake()
        {
            Id = WindowId.CreditsPopupWindow;
        }

        protected override void SubscribeUpdates()
        {
            _closeButton.onClick.AddListener(Close);
        }

        protected override void UnsubscribeUpdates()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(Close);
        }

        private void Close()
        {
            _audioService.PlaySound(Sounds.buttonClick);
            _windowService.Close(Id);
        }
    }
}

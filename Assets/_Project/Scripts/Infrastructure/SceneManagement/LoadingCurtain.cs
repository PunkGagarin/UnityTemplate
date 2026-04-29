using _Project.Scripts.Utils;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Infrastructure.SceneManagement
{
    public class LoadingCurtain : ContentUi
    {
        [field: SerializeField]
        public float HideDuration { get; private set; } = 1.5f;

        [field: SerializeField]
        public Image image;

        private TweenerCore<Color, Color, ColorOptions> _tween;

        public override void Show()
        {
            _tween?.Kill();
            
            var color = image.color;
            color.a = 1;
            image.color = color;
            content.SetActive(true);
        }
        
        public override void Hide()
        {
            _tween = image.DOFade(0, HideDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() => { content.SetActive(false); });
        }
    }
}
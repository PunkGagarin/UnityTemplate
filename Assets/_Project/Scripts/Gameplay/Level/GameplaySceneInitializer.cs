using _Project.Scripts.Gameplay.Cameras.Provider;
using UnityEngine;
using Zenject;

namespace _Project.Scripts.Gameplay.Level
{
    public class GameplaySceneInitializer : MonoBehaviour, IInitializable
    {
        [field: SerializeField] private Camera MainCamera { get; set; }
        [field: SerializeField] private Transform StartPoint { get; set; }

        [Inject] private ICameraProvider _cameraProvider;
        [Inject] private ILevelStartPointProvider _levelStartPointProvider;

        public void Initialize()
        {
            if (MainCamera != null)
                _cameraProvider.SetMainCamera(MainCamera);

            if (StartPoint != null)
                _levelStartPointProvider.SetStartPoint(StartPoint.position);
        }
    }
}

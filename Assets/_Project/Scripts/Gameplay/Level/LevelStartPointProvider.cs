using UnityEngine;

namespace _Project.Scripts.Gameplay.Level
{
    public class LevelStartPointProvider : ILevelStartPointProvider
    {
        public Vector3 StartPoint { get; private set; }

        public void SetStartPoint(Vector3 startPoint)
        {
            StartPoint = startPoint;
        }
    }
}

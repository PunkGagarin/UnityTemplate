using UnityEngine;

namespace _Project.Scripts.Gameplay.Level
{
    public interface ILevelStartPointProvider
    {
        Vector3 StartPoint { get; }
        void SetStartPoint(Vector3 startPoint);
    }
}

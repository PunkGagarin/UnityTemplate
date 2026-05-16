using UnityEngine;

namespace _Project.Scripts.Gameplay.Enemies
{
    public interface IEnemySpawner
    {
        void Start(Transform target);
        void Update();
        void Cleanup();
    }
}

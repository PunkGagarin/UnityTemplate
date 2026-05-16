using UnityEngine;

namespace _Project.Scripts.Gameplay.Enemies
{
    public interface IEnemyFactory
    {
        EnemyUnit Create(Vector3 at);
        void Cleanup();
    }
}

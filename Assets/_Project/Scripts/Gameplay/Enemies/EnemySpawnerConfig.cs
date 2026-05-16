using UnityEngine;

namespace _Project.Scripts.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "EnemySpawnerConfig", menuName = "Game Resources/Configs/Enemy Spawner")]
    public class EnemySpawnerConfig : ScriptableObject
    {
        [field: SerializeField, Min(0.01f)] public float SpawnIntervalSeconds { get; private set; } = 3f;
    }
}

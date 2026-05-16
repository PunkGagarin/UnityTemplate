using System.Collections.Generic;
using UnityEngine;
using Zenject;
using _Project.Scripts.Infrastructure.AssetManagement;

namespace _Project.Scripts.Gameplay.Enemies
{
    public class EnemyFactory : IEnemyFactory
    {
        private const string ENEMY_UNIT_PREFAB_PATH = "Gameplay/Enemies/EnemyUnit";

        private readonly List<EnemyUnit> _createdEnemies = new();

        [Inject] private IAssetProvider _assetProvider;
        [Inject] private IInstantiator _instantiator;

        public EnemyUnit Create(Vector3 at)
        {
            EnemyUnit prefab = _assetProvider.LoadAsset<EnemyUnit>(ENEMY_UNIT_PREFAB_PATH);

            if (prefab == null)
            {
                Debug.LogError($"EnemyUnit prefab is missing at Resources path '{ENEMY_UNIT_PREFAB_PATH}'.");
                return null;
            }

            EnemyUnit enemy = _instantiator.InstantiatePrefabForComponent<EnemyUnit>(
                prefab,
                at,
                Quaternion.identity,
                parentTransform: null);

            enemy.name = nameof(EnemyUnit);
            _createdEnemies.Add(enemy);

            return enemy;
        }

        public void Cleanup()
        {
            foreach (EnemyUnit enemy in _createdEnemies)
            {
                if (enemy != null)
                    Object.Destroy(enemy.gameObject);
            }

            _createdEnemies.Clear();
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;
using _Project.Scripts.Gameplay.Common.Random;
using _Project.Scripts.Gameplay.Common.Time;

namespace _Project.Scripts.Gameplay.Enemies
{
    public class EnemySpawner : IEnemySpawner
    {
        private const float MIN_SPAWN_RADIUS = 3f;
        private const float MAX_SPAWN_RADIUS = 5f;

        private Transform _target;
        private bool _isActive;
        private bool _spawnWasStarted;
        private float _timeToNextSpawn;

        [Inject] private IEnemyFactory _enemyFactory;
        [Inject] private EnemySpawnerConfig _config;
        [Inject] private IRandomService _random;
        [Inject] private ITimeService _time;

        public void Start(Transform target)
        {
            _target = target;
            _isActive = target != null;
            _spawnWasStarted = false;
            _timeToNextSpawn = 0;
        }

        public void Update()
        {
            if (!_isActive || _target == null)
                return;

            if (!_spawnWasStarted)
            {
                if (FirstGameplayClickWasReceived())
                    _spawnWasStarted = true;

                return;
            }

            _timeToNextSpawn -= _time.DeltaTime;

            if (_timeToNextSpawn > 0)
                return;

            SpawnEnemy();
            _timeToNextSpawn = _config.SpawnIntervalSeconds;
        }

        public void Cleanup()
        {
            _target = null;
            _isActive = false;
            _spawnWasStarted = false;
            _timeToNextSpawn = 0;

            _enemyFactory.Cleanup();
        }

        private bool FirstGameplayClickWasReceived()
        {
            if (!Input.GetMouseButtonDown(0))
                return false;

            return EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject();
        }

        private void SpawnEnemy()
        {
            float angle = _random.Range(0f, Mathf.PI * 2f);
            float radius = _random.Range(MIN_SPAWN_RADIUS, MAX_SPAWN_RADIUS);
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;

            _enemyFactory.Create(_target.position + offset);
        }
    }
}

using System.Collections.Generic;
using _Project.Scripts.Infrastructure.AssetManagement;
using UnityEngine;
using Zenject;

namespace _Project.Scripts.Gameplay.Units
{
    public class ExampleUnitFactory : IExampleUnitFactory
    {
        private const string ExampleUnitPrefabPath = "Gameplay/Units/ExampleUnit";

        private readonly List<ExampleUnit> _createdUnits = new();

        [Inject] private IAssetProvider _assetProvider;
        [Inject] private IInstantiator _instantiator;

        public ExampleUnit Create(Vector3 at)
        {
            ExampleUnit prefab = _assetProvider.LoadAsset<ExampleUnit>(ExampleUnitPrefabPath);

            if (prefab == null)
            {
                Debug.LogError($"ExampleUnit prefab is missing at Resources path '{ExampleUnitPrefabPath}'.");
                return null;
            }

            ExampleUnit unit = _instantiator.InstantiatePrefabForComponent<ExampleUnit>(
                prefab,
                at,
                Quaternion.identity,
                parentTransform: null);

            unit.name = nameof(ExampleUnit);
            _createdUnits.Add(unit);

            return unit;
        }

        public void Cleanup()
        {
            foreach (ExampleUnit unit in _createdUnits)
            {
                if (unit != null)
                    Object.Destroy(unit.gameObject);
            }

            _createdUnits.Clear();
        }
    }
}

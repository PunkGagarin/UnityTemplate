using UnityEngine;

namespace _Project.Scripts.Gameplay.Units
{
    public interface IExampleUnitFactory
    {
        ExampleUnit Create(Vector3 at);
        void Cleanup();
    }
}

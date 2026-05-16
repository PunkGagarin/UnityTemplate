using UnityEngine;

namespace _Project.Scripts.Gameplay.Units
{
    public interface IExampleUnitFactory
    {
        ExampleUnit CurrentUnit { get; }
        ExampleUnit Create(Vector3 at);
        void Cleanup();
    }
}

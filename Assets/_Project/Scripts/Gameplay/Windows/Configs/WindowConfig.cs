using System;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Windows.Configs
{
    [Serializable]
    public class WindowConfig
    {
        [field: SerializeField] public WindowId Id { get; private set; }
        [field: SerializeField] public GameObject Prefab { get; private set; }
    }
}

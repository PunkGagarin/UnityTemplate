using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Audio.Data
{
    [CreateAssetMenu(fileName = "SoundsHolder", menuName = "Game Resources/Audio/SoundsHolder")]
    public class SoundsHolder : ScriptableObject
    {
        [field: SerializeField] public List<AudioClip> Sounds { get; private set; }
    }
}

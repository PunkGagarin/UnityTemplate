using System.Collections.Generic;
using _Project.Scripts.GameplayData.Definitions;
using UnityEngine;

namespace _Project.Scripts.GameplayData.Repositories
{
    public abstract class Repository<T> : ScriptableObject where T : Definition
    {
        [SerializeField] private List<T> _definitions;
        
        public List<T> Definitions => _definitions;
    }
}

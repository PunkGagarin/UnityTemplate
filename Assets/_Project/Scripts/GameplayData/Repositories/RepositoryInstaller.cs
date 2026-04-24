using UnityEngine;
using Zenject;

namespace _Project.Scripts.GameplayData.Repositories
{
    [CreateAssetMenu(fileName = "Repository Installer", menuName = "Game Resources/Repository Installer")]
    public class RepositoryInstaller : ScriptableObjectInstaller
    {
        public override void InstallBindings()
        {
        }
    }
}
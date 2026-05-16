using UnityEngine;

namespace _Project.Scripts.Infrastructure.AssetManagement
{
    public interface IAssetProvider
    {
        GameObject LoadAsset(string path);
        T LoadAsset<T>(string path) where T : Component;
    }
}

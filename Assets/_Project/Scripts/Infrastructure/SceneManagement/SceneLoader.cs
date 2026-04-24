using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.Infrastructure.SceneManagement
{
    public class SceneLoader
    {
        private AsyncOperation _asyncOperation;
        private UniTask _loadingTask;

        public async UniTask LoadScene(SceneEnum scene)
        {
            if (_loadingTask.Status == UniTaskStatus.Pending)
                await _loadingTask;

            await LoadSceneAsync(scene);
        }

        private async UniTask LoadSceneAsync(SceneEnum scene)
        {
            _asyncOperation = SceneManager.LoadSceneAsync(scene.ToString());
            await _asyncOperation.ToUniTask();
        }

        public float GetLoadingProcess() =>
            _asyncOperation?.progress ?? 0f;
    }
}
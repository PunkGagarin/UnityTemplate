using Cysharp.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;

namespace _Project.Scripts.Infrastructure.Configs
{
    public class RemoteConfigs
    {

        public async UniTask InitializeAsync()
        {
            // 1. Инициализация Unity Services
            await UnityServices.InitializeAsync();

            // 2. Авторизация игрока (anonymous)
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();


            var tcs = new UniTaskCompletionSource();

            void OnFetchCompleted(ConfigResponse response)
            {
                RemoteConfigService.Instance.FetchCompleted -= OnFetchCompleted;
                tcs.TrySetResult();
            }

            RemoteConfigService.Instance.FetchCompleted += OnFetchCompleted;
            RemoteConfigService.Instance.FetchConfigs(new userAttributes(), new appAttributes());
            await tcs.Task;
        }


        struct userAttributes
        {
        }

        struct appAttributes
        {
        }


        public string GetString(string key, string fallback = "")
        {
            return RemoteConfigService.Instance.appConfig.HasKey(key)
                ? RemoteConfigService.Instance.appConfig.GetString(key)
                : fallback;
        }

        public int GetInt(string key, int fallback = 0)
        {
            return RemoteConfigService.Instance.appConfig.HasKey(key)
                ? RemoteConfigService.Instance.appConfig.GetInt(key)
                : fallback;
        }

        public float GetFloat(string key, float fallback = 0f)
        {
            return RemoteConfigService.Instance.appConfig.HasKey(key)
                ? RemoteConfigService.Instance.appConfig.GetFloat(key)
                : fallback;
        }

        public bool GetBool(string key, bool fallback = false)
        {
            return RemoteConfigService.Instance.appConfig.HasKey(key)
                ? RemoteConfigService.Instance.appConfig.GetBool(key)
                : fallback;
        }
    }
}
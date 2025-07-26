using com.onlineobject.objectnet;
using UnityEngine;

namespace HMSUnitySDK.ObjectNet
{
    public static class HMSObjectNetBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeSceneLoad()
        {
            HMSObjectNetConfig.ClearCache();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            var config = HMSObjectNetConfig.Get();
            if (!config.ShouldInstantiateManager) return;
            var networkManager = Object.Instantiate(config.NetworkManagerPrefab);
            networkManager.StopNetwork();
            Object.DontDestroyOnLoad(networkManager);

            var hmsNetworkManager = HMSLocator.Get<HMSNetworkManager>();
            hmsNetworkManager.Init(networkManager);
        }
    }
}
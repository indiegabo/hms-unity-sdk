using com.onlineobject.objectnet;
using UnityEngine;

namespace HMSUnitySDK.ObjectNet
{
    public static class HMSObjectNetBootstrapper
    {
        [HMSInit(RuntimeInitializeLoadType.BeforeSceneLoad, order: -100)]
        private static void ResetConfigCache()
        {
            HMSObjectNetConfig.ClearCache();
        }

        [HMSInit(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeObjectNetManager()
        {
            var config = HMSObjectNetConfig.Get();
            if (config == null)
            {
                HMSLogger.LogWarning(
                    $"{nameof(HMSObjectNetBootstrapper)} could not load " +
                    $"{nameof(HMSObjectNetConfig)}."
                );
                return;
            }

            if (!config.ShouldInstantiateManager)
            {
                return;
            }

            if (config.NetworkManagerPrefab == null)
            {
                HMSLogger.LogWarning(
                    $"{nameof(HMSObjectNetBootstrapper)} skipped manager instantiation " +
                    "because NetworkManagerPrefab is null."
                );
                return;
            }

            var networkManager = Object.Instantiate(config.NetworkManagerPrefab);
            if (networkManager == null)
            {
                HMSLogger.LogError(
                    $"{nameof(HMSObjectNetBootstrapper)} failed to instantiate " +
                    "NetworkManagerPrefab."
                );
                return;
            }

            networkManager.StopNetwork();
            Object.DontDestroyOnLoad(networkManager);

            if (!HMSLocator.TryGet(out HMSNetworkManager hmsNetworkManager, out var reason))
            {
                HMSLogger.LogError(
                    $"{nameof(HMSObjectNetBootstrapper)} could not resolve " +
                    $"{nameof(HMSNetworkManager)}. {reason}"
                );
                Object.Destroy(networkManager.gameObject);
                return;
            }

            hmsNetworkManager.Init(networkManager);
        }
    }
}
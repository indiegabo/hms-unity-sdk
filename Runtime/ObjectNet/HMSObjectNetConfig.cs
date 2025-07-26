using UnityEngine;
using System.Collections.Generic;
using com.onlineobject.objectnet;
using HMSUnitySDK.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HMSUnitySDK.ObjectNet
{
    [CreateAssetMenu(fileName = "HMSObjectNetConfig", menuName = "HMSUnitySDK/ObjectNet/Config", order = 1)]
    public class HMSObjectNetConfig : ScriptableObject
    {
        private static HMSObjectNetConfig _config;

        public static HMSObjectNetConfig Get()
        {
            var config = Resources.Load<HMSObjectNetConfig>("HMSUnitySDK/HMSObjectNetConfig");

            if (config == null)
            {
#if UNITY_EDITOR
                var directory = new System.IO.DirectoryInfo(Application.dataPath + "/Resources/HMSUnitySDK");
                if (!directory.Exists) directory.Create();

                config = ScriptableObject.CreateInstance<HMSObjectNetConfig>();
                AssetDatabase.CreateAsset(
                    config,
                    "Assets/Resources/HMSUnitySDK/HMSObjectNetConfig.asset"
                );
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
#else
                config = ScriptableObject.CreateInstance<HMSObjectNetConfig>();
#endif
            }

            return config;
        }

        public static void ClearCache()
        {
            _config = null;
        }

        [SerializeField]
        private NetworkManager _networkManagerPrefab;

        [SerializeField]
        private bool _shouldInstantiateManager;

        public NetworkManager NetworkManagerPrefab => _networkManagerPrefab;
        public bool ShouldInstantiateManager => _shouldInstantiateManager;
    }
}
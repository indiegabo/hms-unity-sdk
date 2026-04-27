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
            if (_config != null)
            {
                return _config;
            }

            _config = Resources.Load<HMSObjectNetConfig>("HMSUnitySDK/HMSObjectNetConfig");

            if (_config == null)
            {
#if UNITY_EDITOR
                var directory = new System.IO.DirectoryInfo(Application.dataPath + "/Resources/HMSUnitySDK");
                if (!directory.Exists) directory.Create();

                _config = ScriptableObject.CreateInstance<HMSObjectNetConfig>();
                AssetDatabase.CreateAsset(
                    _config,
                    "Assets/Resources/HMSUnitySDK/HMSObjectNetConfig.asset"
                );
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
#else
                _config = ScriptableObject.CreateInstance<HMSObjectNetConfig>();
#endif
            }

            return _config;
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
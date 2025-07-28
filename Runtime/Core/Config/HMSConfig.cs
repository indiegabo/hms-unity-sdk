using UnityEngine;
using System.Collections.Generic;
using HMSUnitySDK.Utils;
using System.Reflection;
using System.Linq;

namespace HMSUnitySDK
{
    // [CreateAssetMenu(fileName = "HMSConfig", menuName = "HMSUnitySDK/Config", order = 1)]
    public class HMSConfig : ScriptableObject
    {
        #region Static

        private static readonly string SOName = "HMSConfig";
        private static HMSConfig _hmsConfig;
        private static List<Assembly> _cachedAssemblies;

        public static HMSConfig Get()
        {
            return _hmsConfig != null ? _hmsConfig : (
                _hmsConfig = Resources.Load<HMSConfig>($"{HMSResources.Path}/{SOName}")
            );
        }

        public static void ClearCache()
        {
            _hmsConfig = null;
            _cachedAssemblies = null;
        }

#if UNITY_EDITOR
        public static HMSConfig GetFromResources()
        {
            return Resources.Load<HMSConfig>($"{HMSResources.Path}/{SOName}");
        }
#endif

        #endregion
    }
}
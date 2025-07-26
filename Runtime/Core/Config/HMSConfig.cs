using UnityEngine;
using System.Collections.Generic;
using HMSUnitySDK.Utils;
using System.Reflection;

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

        private static HashSet<string> _defaultAssembliesNames = new()
        {
            "Assembly-CSharp",
            "Assembly-CSharp-firstpass",
            "HMSUnitySDK.Runtime",
            "SGUnitySDK.Runtime", // TODO: Remove this assembly as it is not HMS
        };

        #endregion

        [SerializeField] private List<string> _projectSpecificAssemblies = new();

        public List<Assembly> GetTargetAssemblies()
        {
            if (_cachedAssemblies != null)
            {
                return _cachedAssemblies;
            }

            List<string> allAssemblyNames = new(_defaultAssembliesNames);
            allAssemblyNames.AddRange(_projectSpecificAssemblies);

            var assemblies = new List<Assembly>();
            foreach (string assemblyName in allAssemblyNames)
            {
                try
                {
                    assemblies.Add(Assembly.Load(assemblyName));
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to load assembly {assemblyName}. Skipping. Error: {e.Message}");
                }
            }

            _cachedAssemblies = assemblies;
            return _cachedAssemblies;
        }
    }
}
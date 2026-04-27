using UnityEngine;
using System;
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

        public List<Assembly> GetAssemblies()
        {
            if (_cachedAssemblies != null)
            {
                return _cachedAssemblies;
            }

            var assemblyMap = new Dictionary<string, Assembly>(
                StringComparer.OrdinalIgnoreCase
            );

            AddAssembly(assemblyMap, Assembly.GetExecutingAssembly());

            foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                AddAssembly(assemblyMap, loadedAssembly);
            }

            var knownAssemblies = assemblyMap.Values.ToList();
            foreach (var assembly in knownAssemblies)
            {
                AssemblyName[] references;
                try
                {
                    references = assembly.GetReferencedAssemblies();
                }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        $"Failed to get references for assembly {assembly.GetName().Name}. " +
                        $"Skipping references. Error: {e.Message}"
                    );
                    continue;
                }

                foreach (var reference in references)
                {
                    AddAssembly(assemblyMap, TryLoadAssembly(reference));
                }
            }

            // Keep Assembly-CSharp loading as a compatibility fallback for projects
            // that still use default script compilation assemblies.
            AddAssembly(assemblyMap, TryLoadAssembly(new AssemblyName("Assembly-CSharp")));

            _cachedAssemblies = assemblyMap.Values.ToList();

            return _cachedAssemblies;
        }

        private static void AddAssembly(
            IDictionary<string, Assembly> assemblyMap,
            Assembly assembly
        )
        {
            if (assembly == null || assembly.IsDynamic)
            {
                return;
            }

            var fullName = assembly.FullName;
            if (string.IsNullOrEmpty(fullName) || assemblyMap.ContainsKey(fullName))
            {
                return;
            }

            assemblyMap.Add(fullName, assembly);
        }

        private static Assembly TryLoadAssembly(AssemblyName assemblyName)
        {
            try
            {
                return Assembly.Load(assemblyName);
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    $"Failed to load assembly {assemblyName.Name}. " +
                    $"Skipping. Error: {e.Message}"
                );
                return null;
            }
        }
    }
}

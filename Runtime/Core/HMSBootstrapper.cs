using System.Linq;
using HMSUnitySDK.LauncherInteroperations;
using UnityEngine;

namespace HMSUnitySDK
{
    public static class HMSBootstrapper
    {
        /// <summary>
        /// IMPORTANT TO BE RUN BEFORE THE SPLASH SCREEN <br /><br />
        /// Subsystems might be initialized before scene load wich 
        /// occurs after the splash screen.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Bootstrap()
        {
            // We clear the cache so the Runtime Info is forced to be reloaded in case 
            // we are in the Editor.
            HMSRuntimeInfo.ClearCache();
            var hmsRuntimeInfo = HMSRuntimeInfo.Get();
            hmsRuntimeInfo.Profile.ClearCache();

            HMSConfig.ClearCache();
            Debug.Log($"-HMSUnitySDK | {nameof(HMSBootstrapper)} | HMSRuntimeInfo: {hmsRuntimeInfo.Role}");

            GameObject sdkObject = new("HMSUnitySDK");
            Object.DontDestroyOnLoad(sdkObject);

            InitializeLocator(hmsRuntimeInfo, sdkObject.transform);
            CreateHMSLauncherHandlers(hmsRuntimeInfo, sdkObject.transform);
        }

        private static void InitializeLocator(HMSRuntimeInfo hmsRuntimeInfo, Transform parent)
        {
            GameObject servicesContainer = new("[HMS] Services");
            servicesContainer.transform.SetParent(parent);

            var hmsLocator = servicesContainer.AddComponent<HMSLocator>();
            hmsLocator.InitializeServices(hmsRuntimeInfo, servicesContainer.transform);
        }

        private static void CreateHMSLauncherHandlers(HMSRuntimeInfo hmsRuntimeInfo, Transform parent)
        {
            if (hmsRuntimeInfo.Role != HMSRuntimeRole.LaunchedClient) return;
            var hmsConfig = HMSConfig.Get();

            var launcherInteropsService = HMSLocator.Get<HMSLauncherInteropsService>();
            var handlersGO = new GameObject("[HMS] Launcher Interops Handlers");
            handlersGO.transform.SetParent(parent);

            var handlerBaseType = typeof(HMSLauncherInteropsHandler);

            var childrenTypes = hmsConfig.GetTargetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch
                    {
                        return System.Array.Empty<System.Type>();
                    }
                })
                .Where(t => t.IsClass
                    && !t.IsAbstract
                    && handlerBaseType.IsAssignableFrom(t)
                );

            foreach (System.Type childType in childrenTypes)
            {
                GameObject handlerObject = new($"{childType.Name}");
                handlerObject.transform.SetParent(handlersGO.transform);
                var handler = handlerObject.AddComponent(childType) as HMSLauncherInteropsHandler;
                launcherInteropsService.RegisterHandler(handler);
            }

        }
    }
}
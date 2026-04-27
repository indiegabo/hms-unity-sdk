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
            HMSInitPipeline.ResetForBootstrap();

            try
            {
                // We clear the cache so runtime assets are reloaded consistently in
                // both editor and player initialization paths.
                HMSRuntimeInfo.ClearCache();
                HMSConfig.ClearCache();

                if (!TryValidateRuntimeConfiguration(out var hmsRuntimeInfo, out var error))
                {
                    HMSLogger.LogError(
                        $"{nameof(HMSBootstrapper)} | Bootstrap aborted. {error}"
                    );
                    return;
                }

                hmsRuntimeInfo.Profile.ClearCache();
                HMSLogger.Log(
                    $"{nameof(HMSBootstrapper)} | HMSRuntimeInfo: {hmsRuntimeInfo.Role}"
                );

                GameObject sdkObject = new("HMSUnitySDK");
                Object.DontDestroyOnLoad(sdkObject);

                if (!InitializeLocator(hmsRuntimeInfo, sdkObject.transform))
                {
                    Object.Destroy(sdkObject);
                    return;
                }

                if (!CreateHMSLauncherHandlers(hmsRuntimeInfo, sdkObject.transform))
                {
                    Object.Destroy(sdkObject);
                    return;
                }

                HMSInitPipeline.MarkBootstrapCompleted();
            }
            catch (System.Exception exception)
            {
                HMSLogger.LogException(exception);
                HMSLogger.LogError(
                    $"{nameof(HMSBootstrapper)} | Unexpected bootstrap failure. " +
                    "Initialization was aborted safely."
                );
            }
        }

        private static bool TryValidateRuntimeConfiguration(
            out HMSRuntimeInfo hmsRuntimeInfo,
            out string error
        )
        {
            hmsRuntimeInfo = HMSRuntimeInfo.Get();
            if (hmsRuntimeInfo == null)
            {
                error =
                    "HMSRuntimeInfo asset could not be loaded from " +
                    "Resources/HMSResources/HMSRuntimeInfo.";
                return false;
            }

            if (hmsRuntimeInfo.Profile == null)
            {
                error =
                    "HMSRuntimeInfo has no runtime profile assigned. " +
                    "Assign a valid HMSRuntimeProfile before running the game.";
                return false;
            }

            bool requiresBuildMode =
                hmsRuntimeInfo.Role == HMSRuntimeRole.LaunchedClient ||
                hmsRuntimeInfo.Role == HMSRuntimeRole.Server;
            if (!Application.isEditor &&
                requiresBuildMode &&
                hmsRuntimeInfo.Profile.RuntimeMode != HMSRuntimeMode.Build)
            {
                error =
                    $"Runtime profile mode {hmsRuntimeInfo.Profile.RuntimeMode} " +
                    $"is invalid for runtime role {hmsRuntimeInfo.Role}. " +
                    "Use HMSRuntimeMode.Build for launched players and servers.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool InitializeLocator(
            HMSRuntimeInfo hmsRuntimeInfo,
            Transform parent
        )
        {
            try
            {
                GameObject servicesContainer = new("[HMS] Services");
                servicesContainer.transform.SetParent(parent);

                var hmsLocator = servicesContainer.AddComponent<HMSLocator>();
                hmsLocator.InitializeServices(hmsRuntimeInfo, servicesContainer.transform);
                return true;
            }
            catch (System.Exception exception)
            {
                HMSLogger.LogException(exception);
                HMSLogger.LogError(
                    $"{nameof(HMSBootstrapper)} | Failed to initialize {nameof(HMSLocator)}."
                );
                return false;
            }
        }

        private static bool CreateHMSLauncherHandlers(HMSRuntimeInfo hmsRuntimeInfo, Transform parent)
        {
            if (hmsRuntimeInfo.Role != HMSRuntimeRole.LaunchedClient)
            {
                return true;
            }

            if (!HMSLocator.IsInitialized)
            {
                HMSLogger.LogError(
                    $"{nameof(HMSBootstrapper)} | Cannot create launcher handlers " +
                    "because HMSLocator is not initialized."
                );
                return false;
            }

            if (!HMSLocator.TryGet(
                    out HMSLauncherInteropsService launcherInteropsService,
                    out string resolutionReason
                ))
            {
                HMSLogger.LogError(
                    $"{nameof(HMSBootstrapper)} | Cannot create launcher handlers. " +
                    $"{resolutionReason}"
                );
                return false;
            }

            var hmsConfig = HMSConfig.Get();
            if (hmsConfig == null)
            {
                HMSLogger.LogError(
                    $"{nameof(HMSBootstrapper)} | HMSConfig could not be loaded " +
                    "from Resources/HMSResources/HMSConfig."
                );
                return false;
            }

            var handlersGO = new GameObject("[HMS] Launcher Interops Handlers");
            handlersGO.transform.SetParent(parent);

            var handlerBaseType = typeof(HMSLauncherInteropsHandler);

            var childrenTypes = hmsConfig.GetAssemblies()
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
                )
                .Distinct();

            foreach (System.Type childType in childrenTypes)
            {
                GameObject handlerObject = new($"{childType.Name}");
                handlerObject.transform.SetParent(handlersGO.transform);
                var handler = handlerObject.AddComponent(childType) as HMSLauncherInteropsHandler;

                if (handler == null)
                {
                    HMSLogger.LogWarning(
                        $"{nameof(HMSBootstrapper)} | Failed to instantiate " +
                        $"launcher handler of type {childType.Name}."
                    );
                    continue;
                }

                launcherInteropsService.RegisterHandler(handler);
            }

            return true;

        }
    }
}
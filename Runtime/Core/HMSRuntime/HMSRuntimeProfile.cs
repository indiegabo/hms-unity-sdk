using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using HMSUnitySDK.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HMSUnitySDK
{
    [CreateAssetMenu(fileName = "HMSRuntimeProfile", menuName = "HMSUnitySDK/Runtime/Profile", order = 1)]
    public class HMSRuntimeProfile : ScriptableObject
    {
        // HMS-INSTANCE
        public static readonly string InstanceIDKey = "HMS_INSTANCE_ID";
        public static readonly string APIInteropsHostKey = "HMS_API_INTEROPS_HOST";
        public static readonly string APIInteropsPortKey = "HMS_API_INTEROPS_PORT";
        public static readonly string InstanceTCPPortKey = "TCP_PORT";
        public static readonly string InstanceUDPPortKey = "UDP_PORT";

        // HMS-LAUNCHER
        public static readonly string LauncherClientIDKey = "CLIENT_ID";
        public static readonly string LauncherHostKey = "LAUNCHER_HOST";
        public static readonly string LauncherPortKey = "LAUNCHER_PORT";

        [SerializeField]
        private HMSRuntimeMode _runtimeMode;

        [SerializeField]
        private HMSApiInfo _apiInfo;

        private HMSGameInstanceApiInfo _instanceApiInfo;
        private HMSLauncherInfo _launcherInfo;

        public HMSRuntimeMode RuntimeMode => _runtimeMode;

        public HMSApiInfo GetApiInfo()
        {
            HMSValidations.ValidateRoleOperation(HMSRuntimeRole.Client, HMSRuntimeRole.LaunchedClient);
            return _apiInfo;

        }

        public void ClearCache()
        {
            _instanceApiInfo = null;
            _launcherInfo = null;
        }

        #region Instance API Info

        public HMSGameInstanceApiInfo GetGameInstanceApiInfo()
        {
            HMSValidations.ValidateRoleOperation(HMSRuntimeRole.Server);

            if (!Application.isPlaying && _runtimeMode != HMSRuntimeMode.Editor)
                throw new ApplicationNotPlayingException();

            return _instanceApiInfo ??= _runtimeMode switch
            {
                HMSRuntimeMode.Editor => GenerateEditorInstanceAPIInfo(),
                HMSRuntimeMode.Build => GenerateBuildInstanceAPIInfo(),
                _ => GenerateBuildInstanceAPIInfo()
            };
        }


        private HMSGameInstanceApiInfo GenerateEditorInstanceAPIInfo()
        {
            return new HMSGameInstanceApiInfo()
            {
                InstanceID = "EDITOR_INSTANCE",
                ApiHost = "127.0.0.1",
                ApiPort = 3000,
            };
        }

        /// <summary>
        /// Generates the API information for a game server build environment.
        /// </summary>
        /// <remarks>
        /// This method retrieves the necessary API details from environment variables.
        /// Throws an <see cref="System.InvalidOperationException"/> if any required environment variable is not set.
        /// </remarks>
        /// <returns>An instance of <see cref="HMSGameInstanceApiInfo"/> containing the API information.</returns>
        private HMSGameInstanceApiInfo GenerateBuildInstanceAPIInfo()
        {
            string instanceID = System.Environment.GetEnvironmentVariable(InstanceIDKey)
                ?? throw new System.InvalidOperationException($"The environment variable {InstanceIDKey} is not set.");
            string apiHost = System.Environment.GetEnvironmentVariable(APIInteropsHostKey)
                ?? throw new System.InvalidOperationException($"The environment variable {APIInteropsHostKey} is not set.");
            string portString = System.Environment.GetEnvironmentVariable(APIInteropsPortKey)
                ?? throw new System.InvalidOperationException($"The environment variable {APIInteropsPortKey} is not set.");

            return new HMSGameInstanceApiInfo()
            {
                InstanceID = instanceID,
                ApiHost = apiHost,
                ApiPort = int.Parse(portString),
            };
        }

        #endregion

        #region Client Launcher Info

        public HMSLauncherInfo GetGameClientLauncherInfo()
        {
            HMSValidations.ValidateRoleOperation(HMSRuntimeRole.LaunchedClient);

            if (!Application.isPlaying && _runtimeMode != HMSRuntimeMode.Editor)
                throw new ApplicationNotPlayingException();

            return _launcherInfo ??= _runtimeMode switch
            {
                HMSRuntimeMode.Editor => GenerateEditorLauncherInfo(),
                HMSRuntimeMode.Build => GenerateBuildLauncherInfo(),
                _ => GenerateBuildLauncherInfo()
            };
        }

        private HMSLauncherInfo GenerateEditorLauncherInfo()
        {
            return new HMSLauncherInfo()
            {
                ClientID = "editor",
                Host = "127.0.0.1",
                Port = 8000
            };
        }

        private HMSLauncherInfo GenerateBuildLauncherInfo()
        {
            var clientID = System.Environment.GetEnvironmentVariable(LauncherClientIDKey)
                ?? throw new System.InvalidOperationException($"The environment variable {LauncherClientIDKey} is not set.");
            var host = System.Environment.GetEnvironmentVariable(LauncherHostKey)
                ?? throw new System.InvalidOperationException($"The environment variable {LauncherHostKey} is not set.");
            var portString = System.Environment.GetEnvironmentVariable(LauncherPortKey)
                ?? throw new System.InvalidOperationException($"The environment variable {LauncherPortKey} is not set.");

            return new HMSLauncherInfo()
            {
                ClientID = clientID,
                Host = host,
                Port = int.Parse(portString)
            };
        }

        #endregion


#if UNITY_EDITOR
        /// <summary>
        /// EDITOR ONLY <br /><br />
        /// Finds all HMSRuntimeProfile assets in the project.
        /// </summary>
        /// <returns>A list of all HMSRuntimeProfile assets in the project, sorted by profile name.</returns>
        public static List<HMSRuntimeProfile> FindAllInProject()
        {
            List<HMSRuntimeProfile> profiles = new();
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(HMSRuntimeProfile)}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                HMSRuntimeProfile profile = AssetDatabase.LoadAssetAtPath<HMSRuntimeProfile>(path);
                if (profile != null)
                {
                    profiles.Add(profile);
                }
            }

            return profiles.OrderBy(p => p.name).ToList();
        }
#endif
    }
}
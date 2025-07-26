using com.onlineobject.objectnet;
using HMSUnitySDK.Http;
using HMSUnitySDK.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace HMSUnitySDK.ObjectNet
{
    public class HMSNetworkManager : MonoBehaviour, IHMSService
    {
        #region IHMSService 

        public string ServiceObjectName => "HMS Network Manager";
        public void InitializeService() { }
        public bool ValidateService() => true;

        #endregion

        private readonly HMSRuntimeRole _hmsRuntimeRole;
        private NetworkManager _networkManager;
        private HMSRuntimeInfo _hmsRuntimeInfo;
        private string _bridgeToken;

        public bool IsConnected => _networkManager.IsConnected();
        public bool IsBridgeTokenSet => !string.IsNullOrEmpty(_bridgeToken);
        public string BridgeToken => _bridgeToken;

        private void OnDestroy()
        {
            if (_networkManager != null)
                _networkManager.StopNetwork();
        }

        public void Init(NetworkManager networkManager)
        {
            _networkManager = networkManager;
            _networkManager.StopNetwork();
            _hmsRuntimeInfo = HMSRuntimeInfo.Get();

            switch (_hmsRuntimeInfo.Role)
            {
                case HMSRuntimeRole.Server:
                    var gameServerApiInfo = _hmsRuntimeInfo.Profile.GetGameInstanceApiInfo();
                    InitializeServer(gameServerApiInfo);
                    break;
                case HMSRuntimeRole.Client:
                    var apiInfo = _hmsRuntimeInfo.Profile.GetApiInfo();
                    InitializeClient(apiInfo);
                    break;
            }
        }

        /// <summary>
        /// Sets the TCP and UDP ports used by the network connection. This must be done before the client network is started. Information
        /// about ports must be requested to the HMS-API.
        /// </summary>
        /// <param name="tcpPort">The TCP port to set.</param>
        /// <param name="udpPort">The UDP port to set.</param>
        /// <remarks>
        /// This method can only be called when the current instance is running as a client.
        /// </remarks>
        private void SetClientPorts(int tcpPort, int udpPort)
        {
            ValidateRole(HMSRuntimeRole.Client);

            _networkManager.SetTcpPort(tcpPort);
            _networkManager.SetUdpPort(udpPort);
        }

        public void StartClientNetwork(
            string instanceId,
            HMSApiInfo apiInfo,
            UnityAction onSuccess = null,
            UnityAction onFailure = null
        )
        {
            ValidateRole(HMSRuntimeRole.Client);
            _ = StartClientNetworkAsync(instanceId, apiInfo, onSuccess, onFailure);
        }

        public async Awaitable StartClientNetworkAsync(
            string instanceId,
            HMSApiInfo apiInfo,
            UnityAction onSuccess = null,
            UnityAction onFailure = null
        )
        {
            ValidateRole(HMSRuntimeRole.Client);
            switch (_hmsRuntimeInfo.Profile.RuntimeMode)
            {
                case HMSRuntimeMode.Editor:
                    await StartEditorClient(onSuccess, onFailure);
                    break;
                case HMSRuntimeMode.Build:
                    await StartBuildClient(instanceId, apiInfo, onSuccess, onFailure);
                    break;
            }
        }

        public void StopNetwork()
        {
            _bridgeToken = string.Empty;
            _networkManager.StopNetwork();
        }

        private void InitializeServer(HMSGameInstanceApiInfo apiInfo)
        {
            _networkManager.ConfigureMode(NetworkConnectionType.Server);

            // Server instances will always operate on port 4550 as the 
            // HMS-API is conteinerized to expose the 4550 through a dynamic port pooling system.
            _networkManager.SetTcpPort(4550);
            _networkManager.SetUdpPort(4550);

            // TODO: Manage spawn position possibilities
            ResolveSpawnPosition();

            // No need to manual initialization for the server
            _networkManager.StartNetwork();
        }

        private void InitializeClient(HMSApiInfo apiInfo)
        {
            _networkManager.ConfigureMode(NetworkConnectionType.Client);

            // Client instances will connect to the HMS-API host
            _networkManager.SetServerAddress(apiInfo.ApiHost);
            Debug.Log($"-HMSUnitySDK | HMSNetworkManager | Server address: {apiInfo.ApiHost}");
        }

        private void ValidateRole(HMSRuntimeRole role)
        {
            if (_hmsRuntimeRole != role)
            {
                throw new OperationNotAllowedForRoleException();
            }
        }

        private void ResolveSpawnPosition()
        {
            var spawnPoint = FindAnyObjectByType<HMSSpawnPoint>();
            if (spawnPoint == null)
                throw new System.InvalidOperationException("No spawn point found.");

            _networkManager.SetSpawnPosition(spawnPoint.transform);
        }

        private async Awaitable<JoinInstanceData> JoinInstance(string instanceId, HMSApiInfo apiInfo, string accessToken)
        {
            var request = HMSHttpRequest.To($"{apiInfo.HttpPrefix}/game-instances/{instanceId}/join", HttpMethod.Post);
            request.SetBearerAuth(accessToken);
            var response = await request.SendAsync();
            if (!response.Success)
            {
                throw new System.Exception("Failed to join instance: " + response.HttpErrorMessage);
            }

            return response.ReadBodyData<JoinInstanceData>();
        }

        private async Awaitable StartEditorClient(UnityAction onSuccess = null, UnityAction onFailure = null)
        {
            _bridgeToken = System.Guid.NewGuid().ToString();
            SetClientPorts(4550, 4550);

            // TODO: Manage spawn position possibilities
            ResolveSpawnPosition();
            _networkManager.StartNetwork();
            onSuccess?.Invoke();

            await Awaitable.NextFrameAsync();
        }

        private async Awaitable StartBuildClient(string instanceId, HMSApiInfo apiInfo, UnityAction onSuccess = null, UnityAction onFailure = null)
        {
            var auth = HMSLocator.Get<HMSAuth>();
            if (!auth.IsAuthenticated)
            {
                throw new System.Exception("You must be authenticated before starting a client network.");
            }

            try
            {
                var joinData = await JoinInstance(instanceId, apiInfo, auth.AuthData.access_token);
                _bridgeToken = joinData.bridge_token;

                SetClientPorts(joinData.connectivity.tcp_port, joinData.connectivity.udp_port);

                // TODO: Manage spawn position possibilities
                ResolveSpawnPosition();
                _networkManager.StartNetwork();
                onSuccess?.Invoke();
            }
            catch (System.Exception ex)
            {
                onFailure?.Invoke();
                Debug.LogException(ex);
            }
        }
    }

    [System.Serializable]
    public struct JoinInstanceData
    {
        public string bridge_token;
        public InstanceConnectivityData connectivity;
    }

    [System.Serializable]
    public struct InstanceConnectivityData
    {
        public string instance_id;
        public InstanceStatus status;
        public int tcp_port;
        public int udp_port;
    }

    public enum InstanceStatus
    {
        Idle = 0,
        Initializing = 1,
        InitializationFailed = 2,
        Running = 3,
        Stopped = 4,
        Destroying = 5,
    }
}
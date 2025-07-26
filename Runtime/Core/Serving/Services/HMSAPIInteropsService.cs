using System;
using HMSUnitySDK.APIInterops;
using UnityEngine;

namespace HMSUnitySDK
{
    [HMSBuildRoles(HMSRuntimeRole.Server)]
    public class HMSAPIInteropsService : MonoBehaviour, IHMSService
    {
        #region Static


        #endregion

        #region IHMSService 

        public string ServiceObjectName => "[Interops] API";
        public void InitializeService() { }
        public bool ValidateService() => true;

        #endregion

        public event Action<HMSAPIInterops> ConnectionStablished;
        public HMSAPIInterops Connection { get; private set; }
        public bool IsConnected => Connection != null && Connection.IsConnected;

        private HMSGameInstanceApiInfo _gameServerApiInfo;

        private void Awake()
        {
            _gameServerApiInfo = ResolveApiInfo();

            Debug.Log(string.Format(
                "-HMSSocketService | Instance ID: {0}",
                _gameServerApiInfo.InstanceID
            ));

            Debug.Log(string.Format(
                "-HMSSocketService | Socket Host: {0} | Socket Port: {1}",
                _gameServerApiInfo.ApiHost,
                _gameServerApiInfo.ApiPort
            ));

            Debug.Log(string.Format(
                "-HMSSocketService | HTTP Host: {0} | HTTP Port: {1}",
                _gameServerApiInfo.ApiHost,
                _gameServerApiInfo.ApiPort
            ));

            Connection = ResolveConnection();
            Connection.ConnectionStatusChanged += OnConnectionStatusChanged;
            Connection.Connect();
        }

        private void OnDestroy()
        {
            Connection.ConnectionStatusChanged -= OnConnectionStatusChanged;
            Connection = null;
        }

        private void OnConnectionStatusChanged(HMSAPIInterops.ConnectionStatus status)
        {
            if (status == HMSAPIInterops.ConnectionStatus.Connected)
            {
                ConnectionStablished?.Invoke(Connection);
            }
        }

        /// <summary>
        /// Resolves the connection object based on whether the application is running in the
        /// editor or not. If the application is running in the editor, a dummy socket handler
        /// is used. Otherwise, a SocketIOUnity handler is used.
        /// </summary>
        /// <returns>A new instance of <see cref="HMSAPIInterops"/>.</returns>
        private HMSAPIInterops ResolveConnection()
        {
            if (!Application.isEditor)
            {
                var socketHandler = new HMSAPISocketIOUnity(_gameServerApiInfo);
                var httpHandler = new HMSAPIHttpHandler(_gameServerApiInfo);
                return new HMSAPIInterops(socketHandler, httpHandler, _gameServerApiInfo);
            }
            else
            {
                var socketHandler = new HMSDummyAPIInteropsSocketHandler();
                var httpHandler = new HMSAPIHttpHandler(_gameServerApiInfo);
                return new HMSAPIInterops(socketHandler, httpHandler, _gameServerApiInfo);
            }

        }

        /// <summary>
        /// Resolves the server API information based on the current runtime information.
        /// </summary>
        /// <returns>The server API information.</returns> 
        private HMSGameInstanceApiInfo ResolveApiInfo()
        {
            var hmsRuntimeInfo = HMSRuntimeInfo.Get();
            HMSGameInstanceApiInfo serverApiInfo = hmsRuntimeInfo.Profile.GetGameInstanceApiInfo();
            return serverApiInfo;
        }
    }
}

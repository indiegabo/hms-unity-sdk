
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace HMSUnitySDK.APIInterops
{
    /// <summary>
    /// Represents a connection to the HMS-API so interoperations can happen 
    /// between the HMS-GAME-SERVER and the HMS-API.
    /// </summary>
    public class HMSAPIInterops
    {
        public event Action<ConnectionStatus> ConnectionStatusChanged;

        private readonly HMSAPISocketHandler _socket;
        private readonly HMSAPIHttpHandler _http;
        private HMSGameInstanceApiInfo _gameServerApiInfo;
        private ConnectionStatus _status;

        public HMSGameInstanceApiInfo GameServerAPIInfo => _gameServerApiInfo;
        public HMSAPISocketHandler Socket => _socket;
        public HMSAPIHttpHandler Http => _http;

        public ConnectionStatus Status => _status;
        public bool IsConnected => _status == ConnectionStatus.Connected;

        /// <summary>
        /// Initializes a new instance of the <see cref="HMSAPIInterops"/> class with the given socket handler and metadata.
        /// </summary>
        /// <param name="socketHandler">The socket handler to be used for the connection.</param>
        /// <param name="metaData">The metadata for the connection.</param>
        public HMSAPIInterops(HMSAPISocketHandler socketHandler, HMSAPIHttpHandler httpHandler, HMSGameInstanceApiInfo apiInfo)
        {
            _socket = socketHandler;
            _http = httpHandler;
            _gameServerApiInfo = apiInfo;
            _status = ConnectionStatus.Disconnected;
        }

        /// <summary>
        /// Initiates the connection process to the HMS Cloud by changing the status to Connecting
        /// and starting the asynchronous connection task.
        /// </summary>
        public void Connect()
        {
            ChangeStatus(ConnectionStatus.Connecting);
            _ = ConnectionTask();
        }

        /// <summary>
        /// Asynchronously connects to the HMS Cloud using the provided socket handler.
        /// If the connection is successful, the connection status is changed to Connected.
        /// Otherwise, the connection status is changed to Failed.
        /// </summary>
        private async Task ConnectionTask()
        {
            bool connected = await _socket.Connect();
            if (connected)
            {
                ChangeStatus(ConnectionStatus.Connected);
            }
            else
            {
                ChangeStatus(ConnectionStatus.Failed);
            }
        }

        /// <summary>
        /// Changes the connection status and notifies any listeners of the change.
        /// </summary>
        /// <param name="newStatus">The new connection status.</param>
        private void ChangeStatus(ConnectionStatus @new)
        {
            _status = @new;
            ConnectionStatusChanged?.Invoke(_status);
        }

        /// <summary>
        /// The status regarding the connection to the HMS-API.
        /// </summary>
        public enum ConnectionStatus
        {
            /// <summary>
            /// The instance is not connected to the HMS-API.
            /// </summary>
            Disconnected,
            /// <summary>
            /// The instance is connected to the HMS-API.
            /// </summary>
            Connected,
            /// <summary>
            /// An attempt is being made to connect to the HMS-API.
            /// </summary>
            Connecting,
            /// <summary>
            /// The connection to the HMS-API has failed.
            /// </summary>
            Failed,
        }
    }
}
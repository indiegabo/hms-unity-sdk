
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace HMSUnitySDK.LauncherInteroperations
{
    /// <summary>
    /// Represents a connection to the HMS-LAUNCHER so interoperations can happen 
    /// between the HMS-GAME-CLIENT and the HMS-LAUNCHER.
    /// </summary>
    public class HMSLauncherInterops
    {
        public event Action<ConnectionStatus> ConnectionStatusChanged;

        private readonly HMSLauncherSocket _socket;
        private readonly HMSLauncherInfo _launcherInfo;
        private ConnectionStatus _status;

        public HMSLauncherInfo LauncherInfo => _launcherInfo;
        public HMSLauncherSocket Socket => _socket;

        public ConnectionStatus Status => _status;
        public bool IsConnected => _status == ConnectionStatus.Connected;

        /// <summary>
        /// Initializes a new instance of the <see cref="HMSLauncherInterops"/> class with the given socket handler and metadata.
        /// </summary>
        /// <param name="socketHandler">The socket handler to be used for the connection.</param>
        /// <param name="metaData">The metadata for the connection.</param>
        public HMSLauncherInterops(HMSLauncherSocket socketHandler, HMSLauncherInfo launcherInfo)
        {
            _socket = socketHandler;
            _launcherInfo = launcherInfo;
            _status = ConnectionStatus.Disconnected;
        }

        /// <summary>
        /// Initiates the connection process to the HMS Cloud by changing the status to Connecting
        /// and starting the asynchronous connection task.
        /// </summary>
        public void Connect()
        {
            ChangeStatus(ConnectionStatus.Connecting);
            _ = ConnectAsync();
        }

        /// <summary>
        /// Asynchronously connects to the HMS-LAUNCHER using the provided socket handler.
        /// If the connection is successful, the connection status is changed to Connected.
        /// Otherwise, the connection status is changed to Failed.
        /// </summary>
        public async Awaitable<bool> ConnectAsync()
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
            return connected;
        }

        public void Disconnect()
        {
            _socket.Disconnect();
            ChangeStatus(ConnectionStatus.Disconnected);
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
        /// The status regarding the connection to the HMS-LAUNCHER.
        /// </summary>
        public enum ConnectionStatus
        {
            /// <summary>
            /// The client is not connected to the HMS-LAUNCHER.
            /// </summary>
            Disconnected,
            /// <summary>
            /// The client is connected to the HMS-LAUNCHER.
            /// </summary>
            Connected,
            /// <summary>
            /// An attempt is being made to connect to the HMS-LAUNCHER.
            /// </summary>
            Connecting,
            /// <summary>
            /// The connection to the HMS-LAUNCHER has failed.
            /// </summary>
            Failed,
        }
    }
}
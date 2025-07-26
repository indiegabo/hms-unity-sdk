
using UnityEngine;

namespace HMSUnitySDK
{
    [System.Serializable]
    /// <summary>
    /// Represents the API information for a game server instance.
    /// </summary>
    public class HMSGameInstanceApiInfo
    {
        private string _instanceID;
        private string _interopsHost;
        private int _interopsPort;

        /// <summary>
        /// The instance ID on the HMS-API. This is set by the HMS-API when creating the HMS-GAME-SERVER
        /// instance.
        /// </summary>
        public string InstanceID { get => _instanceID; set => _instanceID = value; }

        /// <summary>
        /// The internal HMS-API host.
        /// This is set the by the HMS-API when creating the HMS-GAME-SERVER instance.
        /// </summary>
        public string ApiHost { get => _interopsHost; set => _interopsHost = value; }

        /// <summary>
        /// The internal HMS-API port.
        /// This is set the by the HMS-API when creating the HMS-GAME-SERVER instance.
        /// </summary>
        public int ApiPort { get => _interopsPort; set => _interopsPort = value; }

        /// <summary>
        /// Used to connect to the HMS-API over HTTP and SocketIO.
        /// </summary>
        public string HttpInteropsPrefix => $"http://{_interopsHost}:{_interopsPort}";
    }
}
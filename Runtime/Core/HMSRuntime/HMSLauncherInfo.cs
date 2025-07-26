
using UnityEngine;

namespace HMSUnitySDK
{
    [System.Serializable]
    public class HMSLauncherInfo
    {
        private string _clientID;
        private string _host;
        private int _port;

        /// <summary>
        /// The client ID on the HMS-LAUNCHER. This is set by the HMS-LAUNCHER when starting the HMS-GAME-CLIENT.
        /// </summary>
        public string ClientID { get => _clientID; set => _clientID = value; }

        /// <summary>
        /// The internal HMS-LAUNCHER host.
        /// This is set the by the HMS-LAUNCHER when starting the HMS-GAME-CLIENT.
        /// </summary>
        public string Host { get => _host; set => _host = value; }

        /// <summary>
        /// The internal HMS-LAUNCHER port.
        /// This is set the by the HMS-LAUNCHER when starting the HMS-GAME-CLIENT.
        /// </summary>
        public int Port { get => _port; set => _port = value; }

        /// <summary>
        /// Used to create a URL to the HMS-LAUNCHER.
        /// </summary>
        public string HttpPrefix => $"http://{_host}:{_port}";
    }
}
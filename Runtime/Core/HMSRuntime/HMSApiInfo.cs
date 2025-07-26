using UnityEngine;

namespace HMSUnitySDK
{

    /// <summary>
    /// Contains connectivity information about the server API.
    /// </summary>
    [System.Serializable]
    public class HMSApiInfo
    {

        [SerializeField] private bool _isSecure;
        [SerializeField] private string _apiHost;
        [SerializeField] private int _apiPort;

        /// <summary>
        /// If the connection between the HMS-CLIENT and the HMS-API should be secured.
        /// </summary>
        public bool IsSecure { get => _isSecure; set => _isSecure = value; }

        /// <summary>
        /// The HMS-API host. The address that the HMS-CLIENT will connect to.
        /// </summary>
        public string ApiHost { get => _apiHost; set => _apiHost = value; }

        /// <summary>
        /// The HMS-API port. The port in wich the HMS-CLIENT will stablish a connection to the HMS-API.
        /// </summary>
        public int ApiPort { get => _apiPort; set => _apiPort = value; }

        public string HttpPrefix => _isSecure
            ? $"https://{_apiHost}:{_apiPort}/v1"
            : $"http://{_apiHost}:{_apiPort}/v1";

    }
}
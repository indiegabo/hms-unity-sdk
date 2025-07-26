using UnityEngine;
using System.Collections.Generic;

namespace HMSUnitySDK
{
    /// <summary>
    /// The role the Unity app's runtime will have.
    /// </summary>
    public enum HMSRuntimeRole
    {
        /// <summary>
        /// The app will run as an HMS-GAME-CLIENT.
        /// This means the app will connect to a server and send/receive information.
        /// </summary>
        Client,

        /// <summary>
        /// The app will run as a HMS-GAME-CLIENT that was launched by a HMS-LAUNCHER.
        /// </summary>
        LaunchedClient,

        /// <summary>
        /// The app will run as a HMS-GAME-INSTANCE (server).
        /// This means the app will listen for incoming connections and send/receive information.
        /// </summary>
        Server,
    }
}
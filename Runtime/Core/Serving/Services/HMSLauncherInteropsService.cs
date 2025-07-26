using System;
using System.Collections.Generic;
using HMSUnitySDK.LauncherInteroperations;
using UnityEngine;

namespace HMSUnitySDK
{
    [HMSBuildRoles(HMSRuntimeRole.LaunchedClient)]
    public class HMSLauncherInteropsService : HMSService
    {
        #region Static


        #endregion

        #region IHMSService 

        public override string ServiceObjectName => "[Interops] Launcher";

        #endregion

        #region Fields

        private HMSLauncherInfo _launcherInfo;
        private HashSet<HMSLauncherInteropsHandler> _handlers = new();

        #endregion

        #region Getters

        public event Action<HMSLauncherInterops> ConnectionStablished;
        public HMSLauncherInterops Interops { get; private set; }
        public bool IsConnected => Interops != null && Interops.IsConnected;

        #endregion

        #region Behaviour
        private void Awake()
        {
            _launcherInfo = ResolveLauncherInfo();

            Debug.Log(string.Format(
                "-HMSLauncherInteropsService | Instance ID: {0}",
                _launcherInfo.ClientID
            ));

            Debug.Log(string.Format(
                "-HMSLauncherInteropsService | Socket Host: {0} | Socket Port: {1}",
                _launcherInfo.Host,
                _launcherInfo.Port
            ));

        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the HMS-LAUNCHER connection by registering handlers for each service
        /// and connecting to the HMS-LAUNCHER using the socket information provided in the
        /// launcher information.
        /// </summary>
        /// <returns>An awaitable object representing the asynchronous connection.</returns>
        /// <remarks>
        /// This method should be called inside a try-catch block to handle any potential exceptions.
        /// </remarks>
        public async Awaitable Init()
        {
            Interops = ResolveInterops();

            foreach (var handler in _handlers)
            {
                handler.Deploy(Interops.Socket);
            }

            var connected = await Interops.ConnectAsync();
            if (!connected)
            {
                throw new FailedConnectingToLauncher();
            }
            ConnectionStablished?.Invoke(Interops);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif

        }

        /// <summary>
        /// Deregisters all listeners from the handlers and disconnects from the HMS-LAUNCHER.
        /// This should be called when the client disconnects from the game instance.
        /// </summary>
        public void Dismiss()
        {
            foreach (var handler in _handlers)
            {
                handler.Dispose(Interops.Socket);
            }
            Interops.Disconnect();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
        }

        #endregion

        #region Handlers

        public void RegisterHandler(HMSLauncherInteropsHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (_handlers.Contains(handler))
            {
                throw new ArgumentException("Handler already registered", nameof(handler));
            }

            _handlers.Add(handler);
            Debug.Log($"-HMSUnitySDK | HMSLauncherInteropsService | Registered handler: {handler.GetType().Name}");
        }

        public void DeregisterHandler(HMSLauncherInteropsHandler handler)
        {
            if (_handlers.Contains(handler))
            {
                _handlers.Remove(handler);
                Debug.Log($"-HMSUnitySDK | HMSLauncherInteropsService | Deregistered handler: {handler.GetType().Name}");
            }
        }

        #endregion

        /// <summary>
        /// Resolves the connection object based on whether the application is running in the
        /// editor or not. If the application is running in the editor, a dummy socket handler
        /// is used. Otherwise, a SocketIOUnity handler is used.
        /// </summary>
        /// <returns>A new instance of <see cref="HMSLauncherInterops"/>.</returns>
        private HMSLauncherInterops ResolveInterops()
        {
            HMSRuntimeInfo hmsRuntimeInfo = HMSRuntimeInfo.Get();
            if (hmsRuntimeInfo.Profile.RuntimeMode != HMSRuntimeMode.Editor)
            {
                var socketHandler = new HMSLauncherSocketIOUnity(_launcherInfo);
                return new HMSLauncherInterops(socketHandler, _launcherInfo);
            }
            else
            {
                var socketHandler = new HMSDummyLauncherInteropsSocket();
                return new HMSLauncherInterops(socketHandler, _launcherInfo);
            }
        }


        /// <summary>
        /// Resolves the launcher information required for connecting to the HMS-LAUNCHER.
        /// </summary>
        /// <returns>An instance of <see cref="HMSLauncherInfo"/> containing the launcher details.</returns>
        private HMSLauncherInfo ResolveLauncherInfo()
        {
            var hmsRuntimeInfo = HMSRuntimeInfo.Get();
            HMSLauncherInfo launcherInfo = hmsRuntimeInfo.Profile.GetGameClientLauncherInfo();
            return launcherInfo;
        }


#if UNITY_EDITOR
        private void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange change)
        {
            if (Interops == null) return;

            if (change == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                Interops.Socket.Emit("sg-client.quit");
                Dismiss();
            }
        }
#endif
    }
}

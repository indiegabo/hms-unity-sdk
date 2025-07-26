using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;

namespace HMSUnitySDK.APIInterops
{
    public class HMSAPISocketIOUnity : HMSAPISocketHandler
    {
        private readonly static string NamespacePrefix = "game-instance";
        protected SocketIOUnity _socketIO;
        protected HMSGameInstanceApiInfo _apiInfo;

        int _maxAttempts = 20;
        int _attempts = 0;
        int _attemptsDelay = 1000 * 2; // 2 seconds

        public HMSAPISocketIOUnity(HMSGameInstanceApiInfo apiInfo)
        {
            _apiInfo = apiInfo;
            var uri = new Uri(
                $"{_apiInfo.HttpInteropsPrefix}/{NamespacePrefix}-{apiInfo.InstanceID}"
            );

            Debug.Log($"-HMSUnitySDK | HMSSocketIOUnity | URI {uri}");

            var options = new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    {"token", "UNITY"}
                },
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
            };

            _socketIO = new SocketIOUnity(uri, options)
            {
                JsonSerializer = new NewtonsoftJsonSerializer()
            };
        }

        /// <summary>
        /// Emits an event with the given name and arguments over the websocket
        /// connection.
        /// </summary>
        /// <param name="eventName">The name of the event to emit.</param>
        /// <param name="args">The arguments to be passed with the event.</param>
        public override void Emit(string eventName, params object[] args)
        {
            ValidateSocketConnection();
            _socketIO.Emit(eventName, args);
        }

        /// <summary>
        /// Registers a callback for the specified event.
        /// </summary>
        /// <param name="eventName">The name of the event to listen for.</param>
        /// <param name="callback">The action to execute when the event is triggered.</param>
        public override void On(string eventName, Action<SocketIOResponse> callback)
        {
            ValidateSocketConnection();
            _socketIO.On(eventName, callback);
        }

        /// <summary>
        /// Registers a callback for the specified event to be executed on the Unity main thread.
        /// </summary>
        /// <param name="eventName">The name of the event to listen for.</param>
        /// <param name="callback">The action to execute on the Unity main thread when the event is triggered.</param>
        public override void OnUnityThread(string eventName, Action<SocketIOResponse> callback)
        {
            ValidateSocketConnection();
            _socketIO.OnUnityThread(eventName, callback);

        }

        /// <summary>
        /// Removes the listener for the specified <paramref name="eventName"/>.
        /// </summary>
        /// <param name="eventName">The event name to remove the listener for.</param>
        public override void Off(string eventName)
        {
            ValidateSocketConnection();
            _socketIO.Off(eventName);
        }

        /// <summary>
        /// Attempts to establish a connection to the HMS Cloud server using SocketIOUnity.
        /// Retries the connection until the maximum number of attempts is reached.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains 
        /// true if the connection is successful, otherwise false after max attempts.</returns>
        public override async Awaitable<bool> Connect()
        {
            if (_socketIO.Connected) return true;

            while (_attempts < _maxAttempts)
            {
                Debug.Log($"-HMSUnitySDK | HMSSocketIOUnity | Attempting connection at {Time.time}");
                _socketIO.Connect();

                await Task.Delay(_attemptsDelay);

                if (_socketIO.Connected)
                {
                    Debug.Log($"-HMSUnitySDK | HMSSocketIOUnity | Connected to {_socketIO.Namespace} at {Time.time}");
                    return true;
                }

                _socketIO.Disconnect();

                _attempts++;
                Debug.Log($"-HMSUnitySDK | HMSSocketIOUnity | Attempt {_attempts} failed. Trying again in {_attemptsDelay / 1000} seconds...");
            }

            Debug.Log($"-HMSUnitySDK | HMSSocketIOUnity | Failed to connect after {_maxAttempts} attempts.");
            return false;
        }

        public override void Disconnect()
        {
            if (_socketIO == null) return;
            _socketIO.Disconnect();
        }

        /// <summary>
        /// Validates the connection of the SocketIOUnity socket.
        /// Throws a <see cref="SocketNotConnectedException"/> if the socket is not connected.
        /// </summary>
        private void ValidateSocketConnection()
        {
            if (_socketIO.Connected) return;

            throw new SocketNotConnectedException();
        }

        /// <summary>
        /// Exception thrown when the socket is not connected to the HMS Cloud server
        /// yet it is trying to be used.
        /// </summary>
        public class SocketNotConnectedException : Exception
        {
            private static readonly string ErrorMessage
                = $"Trying to operate {nameof(HMSAPISocketIOUnity)} when the socket is not connected.";

            public SocketNotConnectedException() : base(ErrorMessage) { }
        }
    }
}
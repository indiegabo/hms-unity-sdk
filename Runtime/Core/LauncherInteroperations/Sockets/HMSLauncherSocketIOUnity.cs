using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;

namespace HMSUnitySDK.LauncherInteroperations
{
    public class HMSLauncherSocketIOUnity : HMSLauncherSocket
    {
        private readonly static string NamespacePrefix = "hms-game-client";
        protected SocketIOUnity _socketIO;
        protected HMSLauncherInfo _launcherInfo;

        int _maxAttempts = 10;
        int _attempts = 0;
        int _attemptsDelay = 1000 * 1; // 5 seconds

        public HMSLauncherSocketIOUnity(HMSLauncherInfo launcherInfo)
        {
            _launcherInfo = launcherInfo;
            var uri = new Uri(
                $"{_launcherInfo.HttpPrefix}/{NamespacePrefix}-{launcherInfo.ClientID}"
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

        public override async Awaitable<T> EmitWithAck<T>(string eventName, int timeout = 5000, params object[] args)
        {
            ValidateSocketConnection();

            var tcs = new TaskCompletionSource<T>();
            var cts = new CancellationTokenSource(timeout);

            // Configura timeout
            cts.Token.Register(() =>
            {
                if (!tcs.Task.IsCompleted)
                {
                    Debug.LogError($"[SocketIO] Timeout for event: {eventName}");
                    tcs.TrySetException(new TimeoutException($"Event {eventName} timed out after {timeout}ms"));
                }
            });

            try
            {
                // Adiciona um parâmetro vazio para forçar o modo acknowledgement
                var emitArgs = new List<object>(args);
                emitArgs.Insert(0, null); // Socket.IO precisa disso para reconhecer o callback

                await _socketIO.EmitAsync(eventName, cts.Token, socketResponse =>
                {
                    try
                    {
                        var hmsResponse = socketResponse.GetValue<InteropResponse<T>>();
                        tcs.TrySetResult(hmsResponse.data);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SocketIO] Error processing response: {ex}");
                        tcs.TrySetException(ex);
                    }
                }, emitArgs.ToArray());

                return await tcs.Task;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SocketIO] Error emitting event {eventName}: {ex}");
                tcs.TrySetException(ex);
                throw;
            }
        }

        /// <summary>
        /// Registers a callback for the specified event.
        /// </summary>
        /// <param name="eventName">The name of the event to listen for.</param>
        /// <param name="callback">The action to execute when the event is triggered.</param>
        public override void On(string eventName, Action<SocketIOResponse> callback)
        {
            _socketIO.On(eventName, callback);
        }

        /// <summary>
        /// Registers a callback for the specified event to be executed on the Unity main thread.
        /// </summary>
        /// <param name="eventName">The name of the event to listen for.</param>
        /// <param name="callback">The action to execute on the Unity main thread when the event is triggered.</param>
        public override void OnUnityThread(string eventName, Action<SocketIOResponse> callback)
        {
            _socketIO.OnUnityThread(eventName, callback);

        }

        /// <summary>
        /// Removes the listener for the specified <paramref name="eventName"/>.
        /// </summary>
        /// <param name="eventName">The event name to remove the listener for.</param>
        public override void Off(string eventName)
        {
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
                = $"Trying to operate {nameof(HMSLauncherSocketIOUnity)} when the socket is not connected.";

            public SocketNotConnectedException() : base(ErrorMessage) { }
        }
    }


    // Classe para deserializar a resposta padrão do servidor
    [Serializable]
    public class InteropResponse<T>
    {
        public bool success;
        public T data;
        public string error;
        public string code;
        public string stack;
    }

    public static class SocketIOExtensions
    {
        public static T GetValue<T>(this SocketIOResponse response)
        {
            if (response.Count == 0)
                throw new Exception("Empty response");

            var json = response.GetValue<JsonElement>(0);
            return System.Text.Json.JsonSerializer.Deserialize<T>(json.GetRawText());
        }
    }
}
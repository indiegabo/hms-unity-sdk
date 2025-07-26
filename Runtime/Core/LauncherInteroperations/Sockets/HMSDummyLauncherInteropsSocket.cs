using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;

namespace HMSUnitySDK.LauncherInteroperations
{
    public class HMSDummyLauncherInteropsSocket : HMSLauncherSocket
    {
        private readonly Dictionary<string, Action> _emissionHandlers = new();
        private readonly Dictionary<string, Func<object>> _ackEmissionHandlers = new();
        private readonly Dictionary<string, Action<SocketIOResponse>> _eventHandlers = new();

        protected SocketIOUnity _socketIO;

        public void Init()
        {
            var uri = new Uri("http://dummy-url");

            Debug.Log($"-HMSUnitySDK | HMSDummyLauncherInteropsSocket | URI {uri}");

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

        public override async Awaitable<bool> Connect()
        {
            await Task.Delay(2000);
            return true;
        }

        public override void Emit(string eventName, params object[] args)
        {
            if (!_emissionHandlers.ContainsKey(eventName))
            {
                Debug.LogWarning("-HMSDummyLauncherInteropsSocketHandler | Event not found: " + eventName);
                return;
            }

            _emissionHandlers[eventName].Invoke();
            Debug.Log($"-HMSUnitySDK | HMSDummyLauncherInteropsSocketHandler | {eventName} handled");
        }

        public override async Awaitable<T> EmitWithAck<T>(string eventName, int timeout = 5000, params object[] args)
        {
            if (!_ackEmissionHandlers.TryGetValue(eventName, out var handler))
            {
                throw new ArgumentException($"Ack Event handler not found for \"{eventName}\"");
            }
            await Task.Delay(150);

            return (T)handler();
        }

        public override void On(string eventName, Action<SocketIOResponse> callback)
        {
            if (_eventHandlers.ContainsKey(eventName))
            {
                throw new ArgumentException($"Event handler already registered for \"{eventName}\"");
            }

            _eventHandlers.Add(eventName, callback);
        }

        public override void OnUnityThread(string eventName, Action<SocketIOResponse> callback)
        {
            On(eventName, callback);
        }

        public override void Off(string eventName)
        {
            _eventHandlers.Remove(eventName);
        }

        public void RegisterEmissionHandler(string eventName, Action action)
        {
            if (_emissionHandlers.ContainsKey(eventName))
            {
                throw new ArgumentException($"Event handler already registered for \"{eventName}\"");
            }
            _emissionHandlers.Add(eventName, action);
        }

        public void RegisterAckEmissionHandler(string eventName, Func<object> function)
        {
            if (_ackEmissionHandlers.ContainsKey(eventName))
            {
                throw new ArgumentException($"Ack Event handler already registered for \"{eventName}\"");
            }
            _ackEmissionHandlers.Add(eventName, function);
        }

        public void TriggerEvent(string eventName, params object[] values)
        {
            if (!_eventHandlers.TryGetValue(eventName, out var handler))
            {
                throw new ArgumentException($"Event handler not found for \"{eventName}\"");
            }

            SocketIOResponse response = CreateDummyResponse(values, _socketIO);
            _eventHandlers[eventName].Invoke(response);
        }

        private SocketIOResponse CreateDummyResponse(object[] data, SocketIO socket = null)
        {
            // Check if data is provided
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Data cannot be null or empty.");
            }

            // Take the first object (assuming single-object events)
            var firstObject = data[0];

            // Serialize just that object (not the entire array)
            var serializedData = Newtonsoft.Json.JsonConvert.SerializeObject(firstObject);
            var jsonDocument = JsonDocument.Parse(serializedData);
            var jsonElements = new List<JsonElement> { jsonDocument.RootElement };

            return new SocketIOResponse(jsonElements, socket)
            {
                PacketId = -1
            };
        }
    }
}

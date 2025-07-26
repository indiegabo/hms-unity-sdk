using System;
using System.Threading.Tasks;
using SocketIOClient;
using UnityEngine;

namespace HMSUnitySDK.APIInterops
{
    public abstract class HMSAPISocketHandler
    {
        public virtual void Emit(string eventName, params object[] args) { }
        public virtual void On(string eventName, Action<SocketIOResponse> callback) { }
        public virtual void OnUnityThread(string eventName, Action<SocketIOResponse> callback) { }
        public virtual void Off(string eventName) { }

        public abstract Awaitable<bool> Connect();
        public virtual void Disconnect() { }

        public HMSAPISocketHandler() { }
    }
}

using System;
using SocketIOClient;
using UnityEngine;

namespace HMSUnitySDK.LauncherInteroperations
{
    public abstract class HMSLauncherSocket
    {
        public virtual void Emit(string eventName, params object[] args) { }
        public virtual Awaitable<T> EmitWithAck<T>(string eventName, int timeout = 5000, params object[] args)
        {
            return default;
        }
        public virtual void On(string eventName, Action<SocketIOResponse> callback) { }
        public virtual void OnUnityThread(string eventName, Action<SocketIOResponse> callback) { }
        public virtual void Off(string eventName) { }

        public abstract Awaitable<bool> Connect();
        public virtual void Disconnect() { }

        public HMSLauncherSocket() { }
    }
}

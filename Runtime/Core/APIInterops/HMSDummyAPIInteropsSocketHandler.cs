using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace HMSUnitySDK.APIInterops
{
    public class HMSDummyAPIInteropsSocketHandler : HMSAPISocketHandler
    {
        private Dictionary<string, Action> _eventHandlers = new();

        public override async Awaitable<bool> Connect()
        {
            await Task.Delay(2000);
            return true;
        }

        public override void Emit(string eventName, params object[] args)
        {
            if (!_eventHandlers.ContainsKey(eventName))
            {
                Debug.LogWarning("-HMSDummySocketHandler | Event not found: " + eventName);
                return;
            }

            _eventHandlers[eventName].Invoke();
            Debug.Log($"-HMSUnitySDK | HMSDummySocketHandler | {eventName} handled");
        }

        public HMSDummyAPIInteropsSocketHandler()
        {
            RegisterEventHandlers();
        }

        private void RegisterEventHandlers()
        {
            // Life Cycle
            _eventHandlers.Add(HMSGameInstanceLifeCycle.EventsNames.Alive, () => { });
            _eventHandlers.Add(HMSGameInstanceLifeCycle.EventsNames.Heartbeat, () => { });
            _eventHandlers.Add(HMSGameInstanceLifeCycle.EventsNames.Ready, () => { });

            // Player Manager
            _eventHandlers.Add(HMSPlayerManager.EventsNames.PlayerJoined, () => { });
            _eventHandlers.Add(HMSPlayerManager.EventsNames.PlayerDisconnected, () => { });
            _eventHandlers.Add(HMSPlayerManager.EventsNames.PlayerReconnected, () => { });
        }
    }
}

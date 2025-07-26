
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace HMSUnitySDK.LauncherInteroperations
{
    public class HMSLauncherInteropsBehaviour : MonoBehaviour
    {
        protected HMSLauncherInteropsService _interopsService;

        public virtual void InitializeService()
        {
            _interopsService = HMSLocator.Get<HMSLauncherInteropsService>();

            if (_interopsService.IsConnected)
            {
                HandleConnection(_interopsService.Interops);
            }

            _interopsService.ConnectionStablished += OnConnectionStablished;

        }


        protected virtual void OnDestroy()
        {
            _interopsService.ConnectionStablished -= OnConnectionStablished;
        }


        private void OnConnectionStablished(HMSLauncherInterops interops) => HandleConnection(interops);

        protected virtual void HandleConnection(HMSLauncherInterops interops)
        {
        }
    }
}
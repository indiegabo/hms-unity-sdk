
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace HMSUnitySDK.APIInterops
{
    public class HMSAPIInteropsBehaviour : MonoBehaviour
    {
        protected HMSAPIInteropsService _interopsService;

        public virtual void InitializeService()
        {
            _interopsService = HMSLocator.Get<HMSAPIInteropsService>();

            if (_interopsService.IsConnected)
            {
                HandleConnection(_interopsService.Connection);
            }

            _interopsService.ConnectionStablished += OnConnectionStablished;

        }

        protected virtual void OnDestroy()
        {
            _interopsService.ConnectionStablished -= OnConnectionStablished;
        }


        private void OnConnectionStablished(HMSAPIInterops interops) => HandleConnection(interops);

        protected virtual void HandleConnection(HMSAPIInterops interops) { }
    }
}
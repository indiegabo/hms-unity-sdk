using UnityEngine;
using com.onlineobject.objectnet;
using System;

namespace HMSUnitySDK.ObjectNet
{
    public class HMSLoginValidationProvider : MonoBehaviour, IInformationProvider
    {

        public bool IsLoginValid(params object[] arguments)
        {
            string identifier = arguments[0] as string; // First argument is the identifier   
            string bridgeToken = arguments[1] as string; // Second argument is the bridge token  

            var playerManager = HMSLocator.Get<HMSPlayerManager>();
            var runtimeInfo = HMSRuntimeInfo.Get();

            if (runtimeInfo.Profile.RuntimeMode == HMSRuntimeMode.Editor)
            {
                playerManager.GenerateDummyTokenEntry(identifier, bridgeToken);
            }

            Debug.Log($"-HMSUnitySDK | HMSLoginValidationProvider |  identifier: {identifier} | bridgeToken: {bridgeToken}");

            bool isValid = playerManager.Join(bridgeToken);
            Debug.Log($"-HMSUnitySDK | HMSLoginValidationProvider | IsLoginValid: {isValid}");
            return isValid;
        }
    }
}

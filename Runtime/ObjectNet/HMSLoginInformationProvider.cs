using UnityEngine;
using com.onlineobject.objectnet;
using System;

namespace HMSUnitySDK.ObjectNet
{
    public class HMSLoginInformationProvider : MonoBehaviour, IInformationProvider
    {
        /// <summary>
        /// /// Retrieves login parameters as an array of objects.
        /// </summary>
        /// <returns>An array of objects containing login parameters.</returns>
        /// <remarks>
        /// Note that this method is used to provide login parameters to the login provider method in same order
        /// </remarks>
        public object[] GetLoginParams()
        {
            var hmsRuntimeInfo = HMSRuntimeInfo.Get();
            var authService = HMSLocator.Get<HMSAuth>();

            var networkManager = HMSLocator.Get<HMSNetworkManager>();
            if (!networkManager.IsBridgeTokenSet)
            {
                Debug.LogError("-HMSLoginInformationProvider | Bridge token not set.");
                throw new Exception($"You must connect to the network using the {nameof(HMSNetworkManager)}.");
            }

            if (hmsRuntimeInfo.Profile.RuntimeMode != HMSRuntimeMode.Editor)
            {
                if (!authService.IsAuthenticated)
                {
                    Debug.LogError("-HMSLoginInformationProvider | User is not logged in.");
                    throw new Exception("You must be authenticated before starting a client network.");
                }
            }

            var loginParams = new object[] {
                authService.GetAuthenticatedUserIdentifier(),
                networkManager.BridgeToken,
            };

            return loginParams;
        }

        /// <summary>
        /// Retrieves the types of the login parameters.
        /// </summary>
        /// <returns>An array of Type objects representing the types of the login parameters.</returns>
        /// <remarks>
        /// Note that this method is used to provide the types of the parameters returned by the GetLoginParams method in same order
        /// </remarks>
        public Type[] GetLoginParamsTypes()
        {
            return new Type[] {
                typeof(string),
                typeof(string)
            };
        }
    }
}

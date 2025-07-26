using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using com.onlineobject.objectnet;

namespace HMSUnitySDK.ObjectNet
{
    public class HMSPlayerIDProvider : MonoBehaviour, IInformationProvider
    {
        public int GetPlayerID(params object[] arguments)
        {
            string identifier = arguments[0] as string; // First argument is identifier   
            Debug.Log($"-HMSUnitySDK | HMSPlayerIDProvider | GetPlayerID identifier: {identifier}");

            var playerManager = HMSLocator.Get<HMSPlayerManager>();
            int playerID = playerManager.GetHMSPlayerID(identifier);

            if (playerID < 0)
            {
                throw new Exception("Invalid Player ID");
            }

            return playerID;
        }
    }
}

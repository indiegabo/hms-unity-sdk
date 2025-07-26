using System;
using SocketIOClient;
using UnityEngine;

namespace HMSUnitySDK.LauncherInteroperations
{
    public class HMSLauncherAppHandler : HMSLauncherInteropsHandler
    {
        private static class InteropsNames
        {
            public static readonly string AppTerminate = "sg-client.terminate";
        }

        public override void Deploy(HMSLauncherSocket socketHandler)
        {
            socketHandler.OnUnityThread(InteropsNames.AppTerminate, OnAppTerminate);
        }

        public override void Dispose(HMSLauncherSocket socketHandler)
        {
            socketHandler.Off(InteropsNames.AppTerminate);
        }

        private void OnAppTerminate(SocketIOResponse response)
        {
#if UNITY_EDITOR
            if (Application.isEditor)
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
#endif
            Application.Quit();
        }

    }
}
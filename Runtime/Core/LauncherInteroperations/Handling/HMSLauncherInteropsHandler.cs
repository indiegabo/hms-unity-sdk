using UnityEngine;

namespace HMSUnitySDK.LauncherInteroperations
{
    public abstract class HMSLauncherInteropsHandler : MonoBehaviour
    {
        public abstract void Deploy(HMSLauncherSocket socketHandler);
        public abstract void Dispose(HMSLauncherSocket socketHandler);
    }
}
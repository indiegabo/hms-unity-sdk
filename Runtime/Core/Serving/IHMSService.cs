using UnityEngine;

namespace HMSUnitySDK
{
    public interface IHMSService
    {
        bool ValidateService();
        string ServiceObjectName { get; }
        void InitializeService();
    }
}
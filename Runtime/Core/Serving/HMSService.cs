
using UnityEngine;

namespace HMSUnitySDK
{
    public abstract class HMSService : MonoBehaviour, IHMSService
    {
        public virtual string ServiceObjectName { get => this.GetType().Name; }
        public virtual void InitializeService() { }
        public virtual bool ValidateService() => true;
    }
}
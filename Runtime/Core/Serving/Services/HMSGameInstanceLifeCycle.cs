using System.Collections;
using UnityEngine;
using HMSUnitySDK.APIInterops;

namespace HMSUnitySDK
{
    [HMSBuildRoles(HMSRuntimeRole.Server)]
    public class HMSGameInstanceLifeCycle : HMSAPIInteropsBehaviour, IHMSService
    {
        #region Static

        public static class EventsNames
        {
            // Emit
            public static readonly string Alive = "game-instance.lfc.alive";
            public static readonly string Heartbeat = "game-instance.lfc.heartbeat";
            public static readonly string Ready = "game-instance.lfc.ready";
        }

        #endregion

        #region Fields

        #endregion

        #region IHMSService 

        public string ServiceObjectName => "[Life Cycle] Instance";
        public override void InitializeService() { base.InitializeService(); }
        public bool ValidateService() => true;

        #endregion

        #region Behaviour
        #endregion

        #region Life Cycle

        public void AnnounceReady()
        {
            if (!_interopsService.IsConnected)
            {
                throw new InteropsNotStabilished(InteropsNotStabilished.Subject.HMSApi);
            }

            var interops = _interopsService.Connection;
            var instanceID = interops.GameServerAPIInfo.InstanceID;
            interops.Socket.Emit(EventsNames.Ready, instanceID);
        }

        protected override void HandleConnection(HMSAPIInterops interops)
        {
            interops.Socket.Emit(EventsNames.Alive, interops.GameServerAPIInfo.InstanceID);
            interops.Socket.Emit(EventsNames.Ready, interops.GameServerAPIInfo.InstanceID);
            StartCoroutine(HeartbeatRoutine(interops));
        }

        private IEnumerator HeartbeatRoutine(HMSAPIInterops interops)
        {
            var heartbeatInterval = new WaitForSeconds(10f);
            while (true)
            {
                yield return heartbeatInterval;
                interops.Socket.Emit(EventsNames.Heartbeat, interops.GameServerAPIInfo.InstanceID);
            }
        }

        #endregion
    }
}
using System.Collections;
using UnityEngine;
using HMSUnitySDK.LauncherInteroperations;

namespace HMSUnitySDK
{
    // [HMSBuildRoles(HMSRuntimeRole.LaunchedClient)]
    // public class HMSLaunchedGameClientLifeCycle : HMSLauncherInteropsBehaviour, IHMSService
    // {
    //     #region Static

    //     public static class EventsNames
    //     {
    //         // Emit
    //         public static readonly string Alive = "hms-game-client.lfc.alive";
    //         public static readonly string Heartbeat = "hms-game-client.lfc.heartbeat";
    //         public static readonly string Ready = "hms-game-client.lfc.ready";
    //     }

    //     #endregion

    //     #region Fields

    //     #endregion

    //     #region IHMSService 

    //     public string ServiceObjectName => "[Life Cycle] Launched Game Client";
    //     public override void InitializeService() { base.InitializeService(); }
    //     public bool ValidateService()
    //     {
    //         var runtime = HMSRuntimeInfo.Get();
    //         Debug.Log($"-HMSUnitySDK | {nameof(HMSLaunchedGameClientLifeCycle)} | Runtime role: {runtime.Role}");
    //         return runtime.Role == HMSRuntimeRole.LaunchedClient;
    //     }

    //     #endregion

    //     #region Behaviour
    //     #endregion

    //     #region Life Cycle

    //     protected override void HandleConnection(HMSLauncherInterops interops)
    //     {
    //         interops.Socket.Emit(EventsNames.Alive, interops.LauncherInfo.ClientID);
    //         StartCoroutine(HeartbeatRoutine(interops));
    //     }

    //     private IEnumerator HeartbeatRoutine(HMSLauncherInterops interops)
    //     {
    //         var heartbeatInterval = new WaitForSeconds(10f);
    //         while (true)
    //         {
    //             yield return heartbeatInterval;
    //             interops.Socket.Emit(EventsNames.Heartbeat, interops.LauncherInfo.ClientID);
    //         }
    //     }

    //     #endregion
    // }
}
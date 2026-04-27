using System.Linq;
using UnityEngine;

namespace HMSUnitySDK.Utils
{
    public static class HMSValidations
    {
        public static void ValidateRoleOperation(params HMSRuntimeRole[] rolesForMethod)
        {
            var hmsRuntimeInfo = HMSRuntimeInfo.Get();
            if (hmsRuntimeInfo == null)
            {
                const string runtimeInfoMessage =
                    "HMSRuntimeInfo could not be loaded from Resources/HMSResources.";
                Debug.LogError(runtimeInfoMessage);
                throw new System.InvalidOperationException(runtimeInfoMessage);
            }

            if (rolesForMethod.Contains(hmsRuntimeInfo.Role)) return;

            var roles = string.Join(", ", rolesForMethod.Select((r, i) => $"{i}: {r}"));
            var operationMessage = $"Operation is marked with roles {roles} " +
                $"but the current runtime is {hmsRuntimeInfo.Role}";
            Debug.LogError(operationMessage);
            throw new OperationNotAllowedForRoleException(operationMessage);
        }

        public static void ValidateRuntimeMode(HMSRuntimeMode mode)
        {
            var hmsRuntimeInfo = HMSRuntimeInfo.Get();
            if (hmsRuntimeInfo == null)
            {
                const string runtimeInfoMessage =
                    "HMSRuntimeInfo could not be loaded from Resources/HMSResources.";
                Debug.LogError(runtimeInfoMessage);
                throw new System.InvalidOperationException(runtimeInfoMessage);
            }

            if (hmsRuntimeInfo.Profile == null)
            {
                const string profileMessage =
                    "HMSRuntimeInfo.Profile is not assigned. Runtime mode cannot be validated.";
                Debug.LogError(profileMessage);
                throw new System.InvalidOperationException(profileMessage);
            }

            if (mode == hmsRuntimeInfo.Profile.RuntimeMode) return;
            var operationMessage = $"Operation is marked with mode {mode} " +
                $"but the current runtime mode is {hmsRuntimeInfo.Profile.RuntimeMode}";
            Debug.LogError(operationMessage);
            throw new OperationNotAllowedForModeException(operationMessage);
        }
    }
}
using UnityEngine;
using System.Collections.Generic;
using HMSUnitySDK.Utils;

namespace HMSUnitySDK
{
    // [CreateAssetMenu(fileName = "HMSExecutionInfo", menuName = "HMSUnitySDK/Runtime/Info", order = 1)]
    public class HMSRuntimeInfo : ScriptableObject
    {
        #region Static

        private static readonly string SOName = "HMSRuntimeInfo";
        private static HMSRuntimeInfo _hmsRuntimeInfo;

        public static HMSRuntimeInfo Get()
        {
            return _hmsRuntimeInfo != null ? _hmsRuntimeInfo : (
                _hmsRuntimeInfo = Resources.Load<HMSRuntimeInfo>($"{HMSResources.Path}/{SOName}")
            );
        }

        public static void ClearCache()
        {
            _hmsRuntimeInfo = null;
        }

#if UNITY_EDITOR
        public static HMSRuntimeInfo GetFromResources()
        {
            return Resources.Load<HMSRuntimeInfo>($"{HMSResources.Path}/{SOName}");
        }
#endif

        #endregion

        [SerializeField] private HMSRuntimeRole _role;
        [SerializeField] private HMSRuntimeProfile _profile;
        [SerializeField] private bool _instantiateObjectNetManager;

        public HMSRuntimeRole Role => _role;
        public HMSRuntimeProfile Profile => _profile;
        public bool InstantiateObjectNetManager => _instantiateObjectNetManager;

        public bool IsEditorRuntime => _profile == null || _profile.RuntimeMode == HMSRuntimeMode.Editor;

#if UNITY_EDITOR
        public void SetRole(HMSRuntimeRole role) => _role = role;
        public void SetProfile(HMSRuntimeProfile profile) => _profile = profile;
#endif
    }
}
// Atributo para tornar o elemento visível no UI Builder
using System;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace HMSUnitySDK.Editor
{
    [UxmlElement]
    public partial class HMSBuildProfileElement : VisualElement
    {
        private static readonly string TemplateName = "HMSBuildProfileElement";

        private TemplateContainer _containerMain;
        private ObjectField _fieldUnityBuildProfile;

        private bool CanOperate => _settings != null;

        private HMSRuntimeRole _role;
        private HMSPanelSettings _settings;

        public HMSBuildProfileElement()
        {
            _containerMain = Resources.Load<VisualTreeAsset>($"UXML/{TemplateName}").CloneTree();

            _fieldUnityBuildProfile = _containerMain.Q<ObjectField>("field-unity-build-profile");
            _fieldUnityBuildProfile.objectType = typeof(BuildProfile);
            _fieldUnityBuildProfile.RegisterValueChangedCallback(OnProfileValueChanged);

            Add(_containerMain);
        }

        public void Setup(HMSRuntimeRole role, HMSPanelSettings settings)
        {
            _role = role;
            _settings = settings;

            var buildProfile = _settings.GetBuildProfile(_role);
            _fieldUnityBuildProfile.SetValueWithoutNotify(buildProfile.UnityBuildProfile);
        }

        private void OnProfileValueChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            if (!CanOperate) return;
            var unityBuildProfile = evt.newValue as BuildProfile;
            if (unityBuildProfile == null)
            {
                Debug.LogError("Build profile is null!");
                return;
            }

            var buildProfile = _settings.GetBuildProfile(_role);
            buildProfile.UnityBuildProfile = unityBuildProfile;
            _settings.Save();
        }
    }
}
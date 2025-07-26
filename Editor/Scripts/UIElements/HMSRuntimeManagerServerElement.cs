using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace HMSUnitySDK.Editor
{
    [UxmlElement]
    public partial class HMSRuntimeManagerServerElement : VisualElement
    {
        private static readonly string TemplateName = "HMSRuntimeManagerServerElement";

        private TemplateContainer _containerMain;

        private HMSRuntimeInfo _runtimeInfo;
        private HMSPanelSettings _settings;

        public HMSRuntimeManagerServerElement()
        {
            _containerMain = Resources.Load<VisualTreeAsset>($"UXML/{TemplateName}").CloneTree();
            _containerMain.style.flexGrow = 1;

            Add(_containerMain);
        }

        public void DefineSettings(HMSPanelSettings settings)
        {
            _settings = settings;
        }

        public void Setup(HMSRuntimeInfo runtimeInfo)
        {
            _runtimeInfo = runtimeInfo;
        }

        public void Dismiss()
        {
        }
    }
}
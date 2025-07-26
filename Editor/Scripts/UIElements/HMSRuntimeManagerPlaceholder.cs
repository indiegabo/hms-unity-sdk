using UnityEngine;
using UnityEngine.UIElements;

namespace HMSUnitySDK.Editor
{
    [UxmlElement]
    public partial class HMSRuntimeManagerPlaceholderElement : VisualElement
    {
        private static readonly string TemplateName = "HMSRuntimeManagerPlaceholderElement";

        private TemplateContainer _containerMain;

        public HMSRuntimeManagerPlaceholderElement()
        {
            _containerMain = Resources.Load<VisualTreeAsset>($"UXML/{TemplateName}").CloneTree();
            _containerMain.style.flexGrow = 1;

            Add(_containerMain);
        }
    }
}
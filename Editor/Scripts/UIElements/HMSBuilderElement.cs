using System.IO;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace HMSUnitySDK.Editor
{
    [UxmlElement]
    public partial class HMSBuilderElement : VisualElement
    {
        private static readonly string TemplateName = "HMSBuilderElement";

        private TemplateContainer _containerMain;

        private ToolbarButton _buttonBuild;
        private ToolbarButton _buttonBuildClean;

        private HMSRuntimeInfo _hmsRuntimeInfo;
        private HMSPanelSettings _panelSettings;

        public HMSBuilderElement()
        {
            _panelSettings = HMSPanelSettings.instance;
            _hmsRuntimeInfo = HMSRuntimeInfo.GetFromResources();

            _containerMain = Resources.Load<VisualTreeAsset>($"UXML/{TemplateName}").CloneTree();
            _containerMain.style.flexGrow = 1;

            _buttonBuild = _containerMain.Q<ToolbarButton>("button-build");
            _buttonBuild.clicked += OnButtonBuildClicked;

            _buttonBuildClean = _containerMain.Q<ToolbarButton>("button-build-clean");
            _buttonBuildClean.clicked += OnButtonBuildCleanClicked;

            Add(_containerMain);
        }

        private void OnButtonBuildClicked()
        {
            var buildProfile = _panelSettings.GetBuildProfile(_hmsRuntimeInfo.Role);
            if (!ValidateBuildProfile(_hmsRuntimeInfo.Role, buildProfile)) return;
            var userPath = RequestPathToUser();
            // User canceled
            if (string.IsNullOrEmpty(userPath)) return;
            _ = HMSPlayerBuilder.BuildUsingProfile(buildProfile.UnityBuildProfile, userPath);
        }

        private void OnButtonBuildCleanClicked()
        {
            var buildProfile = _panelSettings.GetBuildProfile(_hmsRuntimeInfo.Role);
            if (!ValidateBuildProfile(_hmsRuntimeInfo.Role, buildProfile)) return;
            var userPath = RequestPathToUser();
            // User canceled
            if (string.IsNullOrEmpty(userPath)) return;
            var hmsOptions = new HMSPlayerBuilder.HMSBuildOptions()
            {
                shoulUpdateVersion = false,
                versionUpdateDefinition = HMSPlayerBuilder.VersionUpdateDefinition.Patch
            };
            _ = HMSPlayerBuilder.BuildCleanUsingProfile(
                buildProfile.UnityBuildProfile,
                userPath, hmsOptions
            );
        }

        private bool ValidateBuildProfile(HMSRuntimeRole role, HMSBuildProfile buildProfile)
        {
            if (buildProfile.UnityBuildProfile == null)
            {
                Debug.LogError("Cannot build without a Unity build profile.");
                return false;
            }

            return true;
        }

        private string RequestPathToUser()
        {
            var buildProfile = _panelSettings.GetBuildProfile(_hmsRuntimeInfo.Role);

            var startingPath = Application.dataPath;
            startingPath.Replace("/Assets", "");

            if (!string.IsNullOrEmpty(buildProfile.LastBuildPath) && Directory.Exists(buildProfile.LastBuildPath))
            {
                startingPath = buildProfile.LastBuildPath;
            }

            string path = EditorUtility.SaveFolderPanel("Select Build Location", startingPath, "");

            // User canceled
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            buildProfile.LastBuildPath = path;
            _panelSettings.Save();
            return path;
        }
    }
}
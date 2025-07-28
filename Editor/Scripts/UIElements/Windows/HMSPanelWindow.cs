using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using UnityEditor.UIElements;
using System.IO;
using UnityEditor.Build.Profile;

namespace HMSUnitySDK.Editor
{
    public class HMSPanelWindow : EditorWindow
    {
        #region Static

        private static readonly string TemplateName = "HMSPanelWindow";
        private static HMSPanelWindow _window;

        private static readonly string RuntimeModeEditorColorHex = "034103";
        private static readonly string RuntimeModeEditorText = "Editor";

        private static readonly string RuntimeModeBuildColorHex = "400239";
        private static readonly string RuntimeModeBuildText = "Production";

        [MenuItem("Tools/HMSUnitySDK/Main Panel", false, 0)]
        public static void ShowWindow()
        {
            if (_window == null)
            {
                _window = GetWindow<HMSPanelWindow>();
                _window.titleContent = new GUIContent("Handy Multiplayer Server Panel");
                _window.minSize = new Vector2(400, 300);
            }
            else
            {
                _window.Show();
            }
        }

        #endregion

        #region Fields

        // Runtime setup Elements
        private TemplateContainer _containerMain;
        private EnumField _fieldRuntimeRole;
        private HMSBuildProfileElement _buildProfileElement;
        private ObjectField _fieldRuntimeProfileSO;

        // Runtime manager elements
        private Label _labelRuntimeMode;

        private VisualElement _runtimeManagerPlaceholder;
        private HMSRuntimeManagerClientElement _runtimeManagerClient;
        private HMSRuntimeManagerServerElement _runtimeManagerServer;

        private HMSRuntimeInfo _hmsRuntimeInfo;
        private HMSPanelSettings _panelSettings;

        #endregion

        #region Editor window Cycle

        public void CreateGUI()
        {
            _panelSettings = HMSPanelSettings.instance;
            _hmsRuntimeInfo = HMSRuntimeInfo.GetFromResources();

            _containerMain = Resources.Load<VisualTreeAsset>($"UXML/{TemplateName}").CloneTree();
            _containerMain.style.flexGrow = 1;

            _fieldRuntimeRole = _containerMain.Q<EnumField>("field-runtime-role");
            _fieldRuntimeRole.value = _hmsRuntimeInfo.Role;
            _fieldRuntimeRole.RegisterValueChangedCallback(OnFieldRuntimeRoleValueChanged);

            _buildProfileElement = _containerMain.Q<HMSBuildProfileElement>("role-build-profile");
            _buildProfileElement.Setup(_hmsRuntimeInfo.Role, _panelSettings);

            _fieldRuntimeProfileSO = _containerMain.Q<ObjectField>("field-runtime-profile-so");
            _fieldRuntimeProfileSO.SetValueWithoutNotify(_hmsRuntimeInfo.Profile);
            _fieldRuntimeProfileSO.RegisterValueChangedCallback(OnFieldProfileSOValueChanged);

            _labelRuntimeMode = _containerMain.Q<Label>("label-runtime-mode");

            _runtimeManagerPlaceholder = _containerMain.Q<VisualElement>("runtime-manager-placeholder");
            _runtimeManagerClient = _containerMain.Q<HMSRuntimeManagerClientElement>("runtime-manager-client");
            _runtimeManagerClient.DefineSettings(_panelSettings);

            _runtimeManagerServer = _containerMain.Q<HMSRuntimeManagerServerElement>("runtime-manager-server");
            _runtimeManagerServer.DefineSettings(_panelSettings);

            bool isPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
            EvaluateRuntimeManagerVisibility(isPlaying, _hmsRuntimeInfo);
            EvaluateRuntimeManagerSetup(isPlaying, _hmsRuntimeInfo);
            EvaluateRuntimeManagerLabel(isPlaying, _hmsRuntimeInfo);

            rootVisualElement.Add(_containerMain);
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        #endregion

        #region Callbacks

        private void OnFieldRuntimeRoleValueChanged(ChangeEvent<Enum> evt)
        {
            var role = (HMSRuntimeRole)evt.newValue;
            _hmsRuntimeInfo.SetRole(role);
            _buildProfileElement.Setup(role, _panelSettings);
            SaveRuntimeInfo();
        }

        private void OnFieldProfileSOValueChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            var profile = evt.newValue as HMSRuntimeProfile;
            _hmsRuntimeInfo.SetProfile(profile);
            SaveRuntimeInfo();
        }

        private void SaveRuntimeInfo()
        {
            EditorUtility.SetDirty(_hmsRuntimeInfo);
            AssetDatabase.SaveAssetIfDirty(_hmsRuntimeInfo);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            bool isPlaying = state switch
            {
                PlayModeStateChange.EnteredPlayMode => true,
                PlayModeStateChange.EnteredEditMode => false,
                _ => false,
            };

            EvaluateRuntimeManagerVisibility(isPlaying, _hmsRuntimeInfo);
            EvaluateRuntimeManagerSetup(isPlaying, _hmsRuntimeInfo);
            EvaluateRuntimeManagerLabel(isPlaying, _hmsRuntimeInfo);
        }

        private void EvaluateRuntimeManagerLabel(bool isPlaying, HMSRuntimeInfo hmsRuntimeInfo)
        {
            if (!isPlaying)
            {
                _labelRuntimeMode.style.display = DisplayStyle.None;
                return;
            }

            _labelRuntimeMode.style.display = DisplayStyle.Flex;

            string colorHex = hmsRuntimeInfo.Profile.RuntimeMode switch
            {
                HMSRuntimeMode.Editor => RuntimeModeEditorColorHex,
                HMSRuntimeMode.Build => RuntimeModeBuildColorHex,
                _ => throw new ArgumentOutOfRangeException(),
            };

            if (ColorUtility.TryParseHtmlString("#" + colorHex, out Color color))
            {
                _labelRuntimeMode.style.backgroundColor = color;
            }
            else
            {
                Debug.LogError("Failed to parse color: " + colorHex);
            }

            string text = hmsRuntimeInfo.Profile.RuntimeMode switch
            {
                HMSRuntimeMode.Editor => RuntimeModeEditorText,
                HMSRuntimeMode.Build => RuntimeModeBuildText,
                _ => throw new ArgumentOutOfRangeException(),
            };

            _labelRuntimeMode.text = text;
        }

        #endregion

        #region Handling methods

        private void EvaluateRuntimeManagerVisibility(bool isPlaying, HMSRuntimeInfo runtimeInfo)
        {
            if (isPlaying)
            {
                _runtimeManagerPlaceholder.style.display = DisplayStyle.None;
                Debug.Log($"Evaluating runtime manager for role: {runtimeInfo.Role}");
                switch (runtimeInfo.Role)
                {
                    case HMSRuntimeRole.Client:
                    case HMSRuntimeRole.LaunchedClient:
                        _runtimeManagerClient.style.display = DisplayStyle.Flex;
                        _runtimeManagerServer.style.display = DisplayStyle.None;
                        break;
                    case HMSRuntimeRole.Server:
                        _runtimeManagerClient.style.display = DisplayStyle.None;
                        _runtimeManagerServer.style.display = DisplayStyle.Flex;
                        break;
                }
            }
            else
            {
                _runtimeManagerPlaceholder.style.display = DisplayStyle.Flex;
                _runtimeManagerClient.style.display = DisplayStyle.None;
                _runtimeManagerServer.style.display = DisplayStyle.None;
            }
        }

        private void EvaluateRuntimeManagerSetup(bool isPlaying, HMSRuntimeInfo runtimeInfo)
        {
            if (isPlaying)
            {
                switch (runtimeInfo.Role)
                {
                    case HMSRuntimeRole.Client:
                        _runtimeManagerClient.Setup(_hmsRuntimeInfo);
                        break;
                    case HMSRuntimeRole.Server:
                        _runtimeManagerServer.Setup(_hmsRuntimeInfo);
                        break;
                }
            }
            else
            {
                _runtimeManagerClient.Dismiss();
                _runtimeManagerServer.Dismiss();
            }
        }

        #endregion

    }
}
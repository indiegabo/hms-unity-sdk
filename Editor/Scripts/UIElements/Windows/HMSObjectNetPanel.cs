using HMSUnitySDK.ObjectNet;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace HMSUnitySDK.Editor
{
    public class HMSObjectNetPanelWindow : EditorWindow
    {
        #region Static

        private static readonly string TemplatePath = "ObjectNet/HMSObjectNetPanelWindow";
        private static HMSObjectNetPanelWindow _window;

        [MenuItem("HMSUnitySDK/ObjectNet", false, 0)]
        public static void ShowWindow()
        {
            if (_window == null)
            {
                _window = GetWindow<HMSObjectNetPanelWindow>();
                _window.titleContent = new GUIContent("HMS ObjectNet Panel");
                _window.minSize = new Vector2(400, 300);
            }
            else
            {
                _window.Show();
            }
        }

        #endregion        

        #region Fields

        private HMSObjectNetConfig _config;
        private SerializedObject _serializedConfig;

        private TemplateContainer _containerMain;

        private UnityEditor.UIElements.ObjectField _fieldNetworkManagerPrefab;
        private Toggle _toggleShouldInstantiateManager;


        #endregion

        #region  Winddow Cycle

        public void CreateGUI()
        {
            _config = HMSObjectNetConfig.Get();
            _serializedConfig = new SerializedObject(_config);

            _containerMain = Resources.Load<VisualTreeAsset>($"UXML/{TemplatePath}").CloneTree();
            _containerMain.style.flexGrow = 1;

            // Get references to UI elements
            _toggleShouldInstantiateManager
                = _containerMain.Q<Toggle>("toggle-should-instantiate-manager");
            _fieldNetworkManagerPrefab
                = _containerMain.Q<UnityEditor.UIElements.ObjectField>("field-network-manager-prefab");

            // Bind properties
            _toggleShouldInstantiateManager
                .BindProperty(_serializedConfig.FindProperty("_shouldInstantiateManager"));
            _fieldNetworkManagerPrefab
                .BindProperty(_serializedConfig.FindProperty("_networkManagerPrefab"));

            // Setup visibility callback
            _toggleShouldInstantiateManager.RegisterValueChangedCallback(evt =>
            {
                UpdatePrefabFieldVisibility(evt.newValue);
            });

            // Set initial visibility
            UpdatePrefabFieldVisibility(_toggleShouldInstantiateManager.value);

            rootVisualElement.Add(_containerMain);
        }

        #endregion

        #region Visibility of fields

        private void UpdatePrefabFieldVisibility(bool shouldShow)
        {
            _fieldNetworkManagerPrefab.SetEnabled(shouldShow);
            _fieldNetworkManagerPrefab.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
        }

        #endregion
    }
}
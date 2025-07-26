using System;
using HMSUnitySDK.Http;
using HMSUnitySDK.ObjectNet;
using Newtonsoft.Json.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace HMSUnitySDK.Editor
{
    [UxmlElement]
    public partial class HMSRuntimeManagerClientElement : VisualElement
    {
        private static readonly string TemplateName = "HMSRuntimeManagerClientElement";

        private TemplateContainer _containerMain;

        // HMS Auth
        private VisualElement _containerAuthForm;
        private TextField _fieldUsernameOrEmail;
        private TextField _fieldPassword;
        private Button _buttonLogin;
        private Label _labelIdentifier;
        private TextField _fieldAccessToken;
        private TextField _fieldRefreshToken;

        // Object Net
        private TextField _fieldInstanceID;
        private Button _buttonCreateInstance;
        private Button _buttonDestroyInstance;

        // Object Net
        private Button _buttonConnect;

        private HMSRuntimeInfo _runtimeInfo;
        private HMSApiInfo _apiInfo;
        private HMSPanelSettings _settings;
        private HMSAuth _auth;
        private HMSNetworkManager _hmsNetworkManager;
        private HMSAuthenticatedUser _authenticatedUser;

        public HMSRuntimeManagerClientElement()
        {
            _containerMain = Resources.Load<VisualTreeAsset>($"UXML/{TemplateName}").CloneTree();
            _containerMain.style.flexGrow = 1;

            // Auth Form
            _containerAuthForm = _containerMain.Q<VisualElement>("container-auth-form");

            _fieldUsernameOrEmail = _containerMain.Q<TextField>("field-username-or-email");
            _fieldUsernameOrEmail.RegisterValueChangedCallback(OnFieldUsernameOrEmailValueChanged);

            _fieldPassword = _containerMain.Q<TextField>("field-password");
            _fieldPassword.RegisterValueChangedCallback(OnPasswordValueChanged);

            _buttonLogin = _containerMain.Q<Button>("button-login");
            _buttonLogin.clicked += OnButtonLoginClicked;

            _labelIdentifier = _containerMain.Q<Label>("label-identifier");
            _fieldAccessToken = _containerMain.Q<TextField>("field-access-token");
            _fieldRefreshToken = _containerMain.Q<TextField>("field-refresh-token");

            _fieldInstanceID = _containerMain.Q<TextField>("field-instance-id");
            _fieldInstanceID.RegisterValueChangedCallback(OnInstanceIDValueChanged);

            _buttonCreateInstance = _containerMain.Q<Button>("button-create-instance");
            _buttonCreateInstance.clicked += OnButtonCreateInstanceClicked;
            _buttonDestroyInstance = _containerMain.Q<Button>("button-destroy-instance");
            _buttonDestroyInstance.clicked += OnButtonDestroyInstanceClicked;

            _buttonConnect = _containerMain.Q<Button>("button-connect");
            _buttonConnect.clicked += OnButtonConnectClicked;

            Add(_containerMain);
        }

        public void DefineSettings(HMSPanelSettings settings)
        {
            _settings = settings;

            _fieldUsernameOrEmail.SetValueWithoutNotify(_settings.UsernameOrEmail);
            _fieldPassword.SetValueWithoutNotify(_settings.Password);
        }

        public void Setup(HMSRuntimeInfo runtimeInfo)
        {
            _runtimeInfo = runtimeInfo;
            _apiInfo = _runtimeInfo.Profile.GetApiInfo();
            _auth = HMSLocator.Get<HMSAuth>();
            _hmsNetworkManager = HMSLocator.Get<HMSNetworkManager>();

            SetButtonConnectEnabled(_hmsNetworkManager.IsConnected);

            _buttonCreateInstance.style.display = DisplayStyle.None;
            _buttonDestroyInstance.style.display = DisplayStyle.None;

            if (_runtimeInfo.Profile.RuntimeMode == HMSRuntimeMode.Editor)
            {
                _containerAuthForm.style.display = DisplayStyle.None;
                _fieldInstanceID.SetValueWithoutNotify("EDITOR_INSTANCE");
                _fieldInstanceID.SetEnabled(false);
            }
            else
            {
                _containerAuthForm.style.display = DisplayStyle.Flex;
                _fieldInstanceID.SetValueWithoutNotify(_settings.LastInstanceID);
                _fieldInstanceID.SetEnabled(true);
            }
        }

        public void Dismiss()
        {
            _runtimeInfo = null;
            _auth = null;

            _fieldAccessToken?.SetValueWithoutNotify(string.Empty);
            _fieldRefreshToken?.SetValueWithoutNotify(string.Empty);
            _fieldInstanceID.SetValueWithoutNotify(string.Empty);
        }

        private void OnPasswordValueChanged(ChangeEvent<string> evt)
        {
            _settings.Password = evt.newValue;
        }

        private void OnFieldUsernameOrEmailValueChanged(ChangeEvent<string> evt)
        {
            _settings.UsernameOrEmail = evt.newValue;
        }

        private void OnInstanceIDValueChanged(ChangeEvent<string> evt)
        {
            _settings.LastInstanceID = evt.newValue;
        }

        private void OnButtonLoginClicked()
        {
            _ = PerformLogin();
        }

        private async Awaitable PerformLogin()
        {
            var payload = new LoginUserPayload()
            {
                emailOrUsername = _fieldUsernameOrEmail.value,
                password = _fieldPassword.value,
            };

            _authenticatedUser = await _auth.Login(payload);

            _labelIdentifier.text = _authenticatedUser.username + " | " + _authenticatedUser.email;
            _fieldAccessToken.value = _auth.AuthData.access_token;
            _fieldRefreshToken.value = _auth.AuthData.refresh_token;

            await EvaluateInstanceStatus(_apiInfo);
        }

        private void OnButtonConnectClicked()
        {
            if (_runtimeInfo == null) return;
            if (_hmsNetworkManager.IsConnected) return;
            if (string.IsNullOrEmpty(_fieldInstanceID.value))
            {
                Debug.LogError("you need to set the instance id.");
                return;
            }

            _hmsNetworkManager.StartClientNetwork(_fieldInstanceID.value, _apiInfo);
        }

        private void SetButtonConnectEnabled(bool networkManagerConnected)
        {
            _buttonConnect.SetEnabled(!networkManagerConnected);
        }

        private void OnButtonCreateInstanceClicked()
        {
            if (string.IsNullOrEmpty(_fieldAccessToken.value))
            {
                Debug.LogError("Not authenticated! Login first.");
                return;
            }
            _ = CreateInstance(_apiInfo, _fieldAccessToken.value);
        }

        private void OnButtonDestroyInstanceClicked()
        {
            if (string.IsNullOrEmpty(_fieldAccessToken.value))
            {
                Debug.LogError("Not authenticated! Login first.");
                return;
            }

            _ = DestroyInstance(_apiInfo, _fieldAccessToken.value);
        }

        private async Awaitable<bool> EvaluateInstanceStatus(HMSApiInfo apiInfo)
        {
            if (string.IsNullOrEmpty(_fieldInstanceID.value))
            {
                _buttonCreateInstance.style.display = DisplayStyle.Flex;
                _buttonDestroyInstance.style.display = DisplayStyle.None;
                return false;
            }

            bool instanceUp = await IsInstanceUp(apiInfo, _fieldAccessToken.value);
            if (instanceUp)
            {
                _buttonCreateInstance.style.display = DisplayStyle.None;
                _buttonDestroyInstance.style.display = DisplayStyle.Flex;
            }
            else
            {
                _buttonCreateInstance.style.display = DisplayStyle.Flex;
                _buttonDestroyInstance.style.display = DisplayStyle.None;
            }

            return instanceUp;
        }

        private async Awaitable<bool> IsInstanceUp(HMSApiInfo apiInfo, string accessToken)
        {
            var instanceID = _fieldInstanceID.value;

            var request = HMSHttpRequest.To($"{apiInfo.HttpPrefix}/game-instances/{instanceID}/exists");
            request.SetBearerAuth(accessToken);
            var response = await request.SendAsync();

            if (!response.Success)
            {
                Debug.LogError("Failed to check instance status: " + response.HttpErrorMessage);
                return false;
            }

            return response.ReadBodyData<bool>();
        }

        private async Awaitable CreateInstance(HMSApiInfo apiInfo, string accessToken)
        {
            bool instanceUp = await IsInstanceUp(apiInfo, accessToken);
            if (instanceUp)
            {
                return;
            }

            var request = HMSHttpRequest.To($"{apiInfo.HttpPrefix}/game-instances", HttpMethod.Post);
            var instanceID = _fieldInstanceID.value;
            request.SetBody(new InstanceCreationPayload()
            {
                identifier = "server-1",
                custom_id = instanceID,
            });

            request.SetBearerAuth(accessToken);

            var response = await request.SendAsync();

            if (!response.Success)
            {
                Debug.LogError("Failed to create instance: " + response.HttpErrorMessage);
            }

            await EvaluateInstanceStatus(apiInfo);
        }

        private async Awaitable DestroyInstance(HMSApiInfo apiInfo, string accessToken)
        {
            bool instanceUp = await IsInstanceUp(apiInfo, accessToken);
            if (!instanceUp)
            {
                return;
            }
            var instanceID = _fieldInstanceID.value;
            var request = HMSHttpRequest.To($"{apiInfo.HttpPrefix}/game-instances/{instanceID}", HttpMethod.Delete);
            request.SetBearerAuth(accessToken);
            var response = await request.SendAsync();

            if (!response.Success)
            {
                Debug.LogError("Failed to destroy instance: " + response.HttpErrorMessage);
            }

            await EvaluateInstanceStatus(apiInfo);

        }

        private struct InstanceCreationPayload
        {
            public string identifier;
            public string custom_id;
        }
    }
}
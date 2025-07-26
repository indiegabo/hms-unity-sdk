using System;
using HMSUnitySDK.Http;
using UnityEngine;
using UnityEngine.Events;

namespace HMSUnitySDK
{
    [HMSBuildRoles(HMSRuntimeRole.Client, HMSRuntimeRole.LaunchedClient)]
    public class HMSAuth : MonoBehaviour, IHMSService
    {
        #region Fields

        private HMSRuntimeInfo _hmsRuntimeInfo;
        private HMSApiInfo _apiInfo;
        private HMSAuthData _authData;

        #endregion

        #region IHMSService 

        public string ServiceObjectName => "Auth";
        public void InitializeService() { }
        public bool ValidateService() => true;

        #endregion

        #region Getters

        public bool IsAuthenticated => _authData != null;

        public HMSAuthData AuthData
        {
            get => _authData;
            private set
            {
                _authData = value;
                OnAuthDataChanged?.Invoke(value);
            }
        }

        public event UnityAction<HMSAuthData> OnAuthDataChanged;

        #endregion

        #region Behaviour

        protected void Awake()
        {
            _hmsRuntimeInfo = HMSRuntimeInfo.Get();
            var runtimeProfile = _hmsRuntimeInfo.Profile;
            _apiInfo = runtimeProfile.GetApiInfo();
        }

        #endregion

        #region Login 

        public async Awaitable<HMSAuthenticatedUser> Login(LoginUserPayload payload)
        {
            var url = $"{_apiInfo.HttpPrefix}/auth/login";
            try
            {

                var request = HMSHttpRequest.To(url, HttpMethod.Post);
                request.SetBody(payload);
                var response = await request.SendAsync();
                if (!response.Success)
                {
                    throw new LoginFailedException(response);
                }

                var data = response.ReadBodyData<HMSAuthData>();

                if (string.IsNullOrEmpty(data.access_token))
                {
                    throw new LoginFailedException("Access token is missing from the response.");
                }

                RegisterAuthData(data);

                return data.user;
            }
            catch (System.Exception e)
            {
                if (e.GetType() == typeof(LoginFailedException)) throw e;
                throw new LoginFailedException(e);
            }
        }

        /// <summary>
        /// Registers and updates the authentication data.
        /// </summary>
        /// <param name="data">The authentication data to be registered.</param>
        /// <remarks>
        /// This method should only be used from outside this class if you 
        /// are defining your own authentication flow. Like when using data 
        /// from a launcher. 
        /// <br />
        /// <br />
        /// This method updates the current authentication data and triggers the
        /// OnAuthDataChanged event to notify any subscribers of the change.
        /// </remarks>
        public void RegisterAuthData(HMSAuthData data)
        {
            AuthData = data;
        }

        #endregion

        #region Bridge Token

        public string GetAuthenticatedUserIdentifier()
        {
            if (_hmsRuntimeInfo.Profile.RuntimeMode == HMSRuntimeMode.Editor)
            {
                return "user_" + Guid.NewGuid().ToString();
            }

            if (!IsAuthenticated)
            {
                var errorMessage = "User is not logged in.";
                Debug.LogError(errorMessage, this);
                throw new Exception(errorMessage);
            }

            return _authData.user.username;
        }

        #endregion

        #region Exceptions

        private class LoginFailedException : System.Exception
        {
            private const string MessageTemplate = "Login failed with status code {0} and message: {1}";

            public LoginFailedException(HMSHttpResponse httpResponse)
            : base(string.Format(MessageTemplate, httpResponse.ResponseCode, httpResponse.HttpErrorMessage))
            {
            }

            public LoginFailedException(System.Exception e)
            : base("Login failed: " + e.Message)
            { }

            public LoginFailedException(string message)
            : base("Login failed: " + message)
            { }
        }

        #endregion
    }

    [System.Serializable]
    public struct LoginUserPayload
    {
        public string emailOrUsername;
        public string password;
        public bool emailOnly;
    }

    [System.Serializable]
    public class HMSAuthData
    {
        public string access_token;
        public string refresh_token;
        public HMSAuthenticatedUser user;
    }

    [System.Serializable]
    public struct HMSAuthenticatedUser
    {
        public int id;
        public string email;
        public string username;
    }
}
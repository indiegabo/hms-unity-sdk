using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace HMSUnitySDK.Http
{
    public class HMSHttpRequest
    {
        #region Static

        private static readonly string ContentTypeString = "Content-Type";

        #endregion

        #region Fields

        private string _url;
        private TimeSpan _timeout;
        private HttpMethod _httpMethod;

        private object _body;
        private byte[] _bodyRaw;
        private string _contentType;

        private UnityWebRequest _unityWebRequest;

        private Dictionary<string, string> _httpHeaders;
        private Dictionary<string, string> _queryEntries;

        private UnityAction<HMSHttpRequest> _onFinalize;

        #endregion

        #region Properties

        protected UnityAction<HMSHttpRequest> OnFinalize => _onFinalize;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new WebRequest
        /// </summary>
        public HMSHttpRequest()
        {
            _timeout = TimeSpan.FromSeconds(120);
            _httpHeaders = new Dictionary<string, string>();
            _queryEntries = new Dictionary<string, string>();
        }

        #endregion

        #region Static Creation

        /// <summary>
        /// Creates and returns a new request setting its base url and HttpMethod
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static HMSHttpRequest To(string url, HttpMethod method = HttpMethod.Get)
        {
            HMSHttpRequest request = new();
            request.SetUrl(url);
            request.SetMethod(method);
            request.AddHeader("Content-Type", "application/json");
            return request;
        }

        #endregion

        #region Preparation

        /// <summary>
        /// Sets the base url to be requested.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public HMSHttpRequest SetUrl(string url)
        {
            _url = url;
            return this;
        }

        /// <summary>
        /// Sets the maximum amount of time before the request is cancelled.
        /// </summary>
        /// <param name="timeoutInSeconds"></param>
        /// <returns></returns>
        public HMSHttpRequest SetTimeout(float timeoutInSeconds)
        {
            _timeout = TimeSpan.FromSeconds(timeoutInSeconds);
            return this;
        }

        /// <summary>
        /// Sets the HttpMethod used by the request
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public HMSHttpRequest SetMethod(HttpMethod method)
        {
            _httpMethod = method;
            return this;
        }

        /// <summary>
        /// Sets an "Authorization" header for authentication.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public HMSHttpRequest SetBearerAuth(string token)
        {
            return AddHeader("Authorization", $"Bearer {token}");
        }

        /// <summary>
        /// Adds a body to the request. This must be a System.Serializable object or a byte array.
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public HMSHttpRequest SetBody(byte[] body)
        {
            _body = null;
            _bodyRaw = body;
            return this;
        }

        /// <summary>
        /// Adds a body to the request. This must be a System.Serializable object or a byte array.
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public HMSHttpRequest SetBody(object body)
        {
            _body = body;
            _bodyRaw = null;
            return this;
        }

        /// <summary>
        /// Defines the content type value
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public HMSHttpRequest SetContentType(string contentType)
        {
            AddHeader(ContentTypeString, contentType);
            return this;
        }

        /// <summary>
        /// Adds a HttpHeader to the request
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public HMSHttpRequest AddHeader(string key, string value)
        {
            if (_httpHeaders.ContainsKey(key))
            {
                _httpHeaders[key] = value;
                return this;
            }
            ;

            _httpHeaders.Add(key, value);
            return this;
        }

        /// <summary>
        /// Removes a HttpHeader to the request
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public HMSHttpRequest RemoveHeader(string key)
        {
            _httpHeaders.Remove(key);
            return this;
        }

        /// <summary>
        /// Adds a query entry
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public HMSHttpRequest AddQueryEntry(string key, int value)
        {
            KeyValuePair<string, string> entry = new(key, value.ToString());
            return AddQueryEntry(entry);
        }

        /// <summary>
        /// Adds a query entry
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public HMSHttpRequest AddQueryEntry(string key, string value)
        {
            KeyValuePair<string, string> entry = new(key, value);
            return AddQueryEntry(entry);
        }

        /// <summary>
        /// Adds a query entry
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public HMSHttpRequest AddQueryEntry(KeyValuePair<string, string> entry)
        {
            if (_queryEntries.ContainsKey(entry.Key)) return this;
            _queryEntries.Add(entry.Key, entry.Value);
            return this;
        }

        /// <summary>
        /// A callback to be invoked when the response arrives meaning the request is complete.
        /// </summary>
        /// <param name="onFinalize"></param>
        /// <returns></returns>
        public HMSHttpRequest SetOnFinalize(UnityAction<HMSHttpRequest> onFinalize)
        {
            _onFinalize = onFinalize;
            return this;
        }

        #endregion

        #region Sending

        /// <summary>
        /// Performs the request. This should be used as a Coroutine.
        /// </summary>
        /// <param name="onDone"></param>
        /// <returns></returns>
        public virtual IEnumerator Send(UnityAction<HMSHttpResponse> onDone)
        {
            _unityWebRequest = GenerateRequest();
            yield return _unityWebRequest.SendWebRequest();

            HMSHttpResponse response = GenerateResponse(_unityWebRequest);

            _onFinalize?.Invoke(this);
            onDone.Invoke(response);
            _unityWebRequest.Dispose();
        }

        /// <summary>
        /// Performs the request asynchronously.
        /// </summary>
        /// <returns>The response</returns>
        public virtual async Awaitable<HMSHttpResponse> SendAsync()
        {
            _unityWebRequest = GenerateRequest();
            UnityWebRequestAsyncOperation operation = _unityWebRequest.SendWebRequest();

            await operation;

            HMSHttpResponse response = GenerateResponse(_unityWebRequest);
            if (!response.Success)
            {
                var errorBody = response.ReadErrorBody();
                var message = "Request to {0} failed with status: {1}";
                Debug.LogError(string.Format(message, _url, response.HttpErrorMessage));
                foreach (var error in errorBody.messages)
                {
                    Debug.LogError(error);
                }
            }

            _onFinalize?.Invoke(this);

            _unityWebRequest.Dispose();

            return response;
        }

        /// <summary>
        /// Generates a UnityWebRequest based on the current request state.
        /// </summary>
        /// <remarks>
        /// This method is intended to be overridden.
        /// The request is generated by combining the <see cref="_url"/> and the query string generated by <see cref="GenerateQuery"/>.
        /// The request headers are set by iterating over <see cref="_httpHeaders"/>.
        /// The request body is set by calling <see cref="SetRequestBody(ref UnityWebRequest)"/>.
        /// The request timeout is set from <see cref="_timeout"/>.
        /// The request's download handler is set to a <see cref="DownloadHandlerBuffer"/>.
        /// </remarks>
        protected virtual UnityWebRequest GenerateRequest()
        {
            string finalUrl = _url + GenerateQuery();

            UnityWebRequest request = new(finalUrl, _httpMethod.ToString());

            // Setting Http Headers
            foreach (KeyValuePair<string, string> dictionaryItem in _httpHeaders)
            {
                request.SetRequestHeader(dictionaryItem.Key, dictionaryItem.Value);
            }

            // Preparing the request body.
            SetRequestBody(ref request);

            request.timeout = (int)_timeout.TotalSeconds;

            request.downloadHandler = new DownloadHandlerBuffer();
            return request;
        }

        /// <summary>
        /// Generates a query string from the current query entries.
        /// </summary>
        /// <returns>
        /// A query string prefixed with '?' for the first entry and '&amp;' for subsequent entries.
        /// </returns>
        protected string GenerateQuery()
        {
            string query = string.Empty;

            for (int i = 0; i < _queryEntries.Count; i++)
            {
                var queryEntry = _queryEntries.ElementAt(i);

                if (i > 0)
                {
                    query += $"&{queryEntry.Key}={queryEntry.Value}";
                }
                else
                {
                    query += $"?{queryEntry.Key}={queryEntry.Value}";
                }
            }

            return query;
        }

        /// <summary>
        /// Sets the request body based on the state of the object.
        /// </summary>
        /// <param name="request">The UnityWebRequest to set the body on.</param>
        /// <remarks>
        /// If <see cref="_body"/> is not null, it will be converted to a JSON string and set as the request body.
        /// If <see cref="_bodyRaw"/> is not null, it will be set as the request body directly.
        /// If <see cref="_contentType"/> is not null or empty, it will be set as the Content-Type header of the request.
        /// </remarks>
        protected virtual void SetRequestBody(ref UnityWebRequest request)
        {
            if (_body == null && _bodyRaw == null) return;

            if (_body != null)
            {
                byte[] boryRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(_body));
                request.uploadHandler = new UploadHandlerRaw(boryRaw);
            }
            else if (_bodyRaw != null) // Bytes array body
            {
                request.uploadHandler = new UploadHandlerRaw(_bodyRaw);
            }

            if (!string.IsNullOrEmpty(_contentType))
            {
                request.uploadHandler.contentType = _contentType;
            }
        }

        /// <summary>
        /// Generates a WebResponse from a UnityWebRequest.
        /// </summary>
        /// <param name="request">The UnityWebRequest to generate the response from.</param>
        /// <returns>A WebResponse representing the result of the request.</returns>
        protected virtual HMSHttpResponse GenerateResponse(UnityWebRequest request)
        {
            bool success = request.result == UnityWebRequest.Result.Success;
            string contents = request.downloadHandler.text;
            string httpErrorMessage = request.error;
            long responseCode = request.responseCode;

            return new HMSHttpResponse(
                this,
                success,
                responseCode,
                contents,
                httpErrorMessage
            );
        }

        #endregion

        #region Preview

        public string GetFinalURL()
        {
            return _url + GenerateQuery();
        }

        #endregion
    }

    public enum HttpMethod
    {
        Get,
        Post,
        Put,
        Delete,
        Options,
        Patch,
    }
}
using System;
using System.Collections.Generic;
using HMSUnitySDK.Http;
using UnityEngine;

namespace HMSUnitySDK.APIInterops
{
    /// <summary>
    /// Represents the metada data for a HTTP request from the 
    /// HMS-GAMES-INSTANCE to the HMS-API.
    /// </summary>
    public class HMSInternalRequestMetaData
    {
        private readonly List<KeyValuePair<string, string>> _queryEntries = new();
        private readonly List<KeyValuePair<string, string>> _headers = new();

        /// <summary>
        /// Well... the HttpMethod of the request
        /// </summary>
        public HttpMethod HttpMethod { get; set; } = HttpMethod.Get;

        /// <summary>
        /// The endpoint of the for the HMS-API
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Data to be sent to the HMS-API
        /// </summary>
        public object Body { get; set; }

        /// <summary>
        /// Raw data to be sent to the HMS-API. 
        /// Raw data means byte stuff.
        /// </summary>
        public byte[] BodyRaw { get; set; }

        /// <summary>
        /// The Bearer token to be sent to the HMS-API in 
        /// case of authentication needed.
        /// </summary>
        public string BearerToken { get; set; }

        public void AddQueryEntry(string key, string value) => _queryEntries.Add(new KeyValuePair<string, string>(key, value));
        public List<KeyValuePair<string, string>> GetQueryEntries() => _queryEntries;

        public void AddHeader(string key, string value) => _headers.Add(new KeyValuePair<string, string>(key, value));
        public List<KeyValuePair<string, string>> GetHeaders() => _headers;
    }


    public class HMSAPIHttpHandler
    {
        protected HMSGameInstanceApiInfo APIInfo { get; private set; }

        public async virtual Awaitable<HMSHttpResponse> Perform(HMSInternalRequestMetaData metaData)
        {
            var request = HMSHttpRequest.To($"{APIInfo.HttpInteropsPrefix}/{metaData.Endpoint}", metaData.HttpMethod);

            var headers = metaData.GetHeaders();

            foreach (var header in headers)
            {
                request.AddHeader(header.Key, header.Value);
            }

            var queryEntries = metaData.GetQueryEntries();

            foreach (var queryEntry in queryEntries)
            {
                request.AddQueryEntry(queryEntry.Key, queryEntry.Value);
            }

            if (!string.IsNullOrEmpty(metaData.BearerToken))
            {
                request.SetBearerAuth(metaData.BearerToken);
            }

            if (metaData.Body != null)
            {
                request.SetBody(metaData.Body);
            }
            else if (metaData.BodyRaw != null)
            {
                request.SetBody(metaData.BodyRaw);
            }

            return await request.SendAsync();
        }

        public HMSAPIHttpHandler(HMSGameInstanceApiInfo apiInfo)
        {
            APIInfo = apiInfo;
        }
    }
}
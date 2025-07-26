
namespace HMSUnitySDK.Http
{
    public class HMSApiRequest
    {
        #region Static Creation

        /// <summary>
        /// Creates and returns a new request setting its base url and HttpMethod
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static HMSHttpRequest To(string endpoint, HttpMethod method = HttpMethod.Get)
        {
            HMSRuntimeInfo hmsRuntimeInfo = HMSRuntimeInfo.Get();

            if (endpoint.StartsWith("/"))
            {
                endpoint = endpoint.Remove(0, 1);
            }

            string url = $"{hmsRuntimeInfo.Profile.GetApiInfo().HttpPrefix}/{endpoint}";
            HMSHttpRequest request = new();

            request.SetUrl(url);
            request.SetMethod(method);
            request.AddHeader("Content-Type", "application/json");
            return request;
        }

        #endregion
    }
}
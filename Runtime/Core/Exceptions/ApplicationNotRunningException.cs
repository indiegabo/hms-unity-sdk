namespace HMSUnitySDK
{
    public class ApplicationNotPlayingException : System.Exception
    {
        private static readonly string _message = $"Operating HMS while not in play mode";
        public ApplicationNotPlayingException() : base(_message) { }
    }
}
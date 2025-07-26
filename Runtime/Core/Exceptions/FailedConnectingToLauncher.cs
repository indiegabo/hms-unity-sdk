namespace HMSUnitySDK
{
    public class FailedConnectingToLauncher : System.Exception
    {
        private static readonly string _message = $"Failed connecting to HMS launcher";
        public FailedConnectingToLauncher() : base(_message) { }
    }
}
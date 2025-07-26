
using System;

namespace HMSUnitySDK
{
    /// <summary>
    /// Exception that is thrown when interops are not stablished.
    /// This means either a HMS-GAME-CLIENT is not connected to its HMS-LAUNCHER or 
    /// a HMS-GAME-INSTANCE is not connected to the HMS-API.
    /// </summary>
    public class InteropsNotStabilished : Exception
    {
        public InteropsNotStabilished()
            : base("Interops not stablished.")
        {
        }

        public InteropsNotStabilished(Subject subject)
            : base($"Interops not stablished for subject {subject}.")
        {
        }

        public enum Subject
        {
            HMSLauncher,
            HMSApi,
        }
    }
}
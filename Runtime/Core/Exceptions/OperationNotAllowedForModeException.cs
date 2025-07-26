
using System;

namespace HMSUnitySDK
{
    /// <summary>
    /// Exception that is thrown when a method is called in an incorrect runtime mode.
    /// </summary>
    public class OperationNotAllowedForModeException : Exception
    {
        public OperationNotAllowedForModeException()
            : base("Operation not allowed for the current runtime mode.")
        {
        }

        public OperationNotAllowedForModeException(string message)
            : base(message)
        {
        }

        public OperationNotAllowedForModeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
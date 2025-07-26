
using System;

namespace HMSUnitySDK
{
    /// <summary>
    /// Exception that is thrown when a method is called in an incorrect runtime role.
    /// </summary>
    public class OperationNotAllowedForRoleException : Exception
    {
        public OperationNotAllowedForRoleException()
            : base("Operation not allowed for the current runtime role.")
        {
        }

        public OperationNotAllowedForRoleException(string message)
            : base(message)
        {
        }

        public OperationNotAllowedForRoleException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
using UnityEngine;
using System.Collections.Generic;

namespace HMSUnitySDK
{
    /// <summary>
    /// Determines the mode of the Unity app's runtime in the HMS cycle
    /// </summary>
    public enum HMSRuntimeMode
    {
        /// <summary>
        /// Means all the instances are running localy in their respective
        /// editors. 
        /// </summary>
        Editor,
        /// <summary>
        /// Means this instance will run in a build app and resolve the <see cref="HMSApiInfo"/>
        /// based on the environment variables set by the HMS-API.
        /// </summary>
        Build,
    }
}
using System;
using UnityEngine;

namespace HMSUnitySDK
{
    /// <summary>
    /// Marks a static method to be executed by the HMS initialization pipeline
    /// after HMS bootstrap has completed successfully.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class HMSInitAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new attribute instance that defaults to
        /// <see cref="RuntimeInitializeLoadType.AfterSceneLoad"/>.
        /// </summary>
        public HMSInitAttribute()
            : this(RuntimeInitializeLoadType.AfterSceneLoad, 0)
        {
        }

        /// <summary>
        /// Initializes a new attribute instance with a custom execution stage and
        /// ordering value.
        /// </summary>
        /// <param name="loadType">
        /// Runtime stage when the method should be invoked.
        /// </param>
        /// <param name="order">
        /// Ordering hint where lower values execute first.
        /// </param>
        public HMSInitAttribute(RuntimeInitializeLoadType loadType, int order = 0)
        {
            LoadType = loadType;
            Order = order;
        }

        /// <summary>
        /// Runtime stage when the method is eligible for execution.
        /// </summary>
        public RuntimeInitializeLoadType LoadType { get; }

        /// <summary>
        /// Ordering value used to sort methods in the same runtime stage.
        /// </summary>
        public int Order { get; }
    }
}
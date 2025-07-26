using System;
using System.Reflection;
using com.onlineobject.objectnet;
using UnityEngine;

namespace HMSUnitySDK.Utils
{
    public static class NetworkManagerExtensions
    {
        /// <summary>
        /// Sets the spawn position for the NetworkManager
        /// </summary>
        /// <param name="networkManager">The NetworkManager to set the spawn position for</param>
        /// <param name="newTransform">The new Transform to use as the spawn position</param>
        /// <remarks>
        /// This method uses Reflection to access the private field "fixedPositionToSpawn" of the NetworkManager
        /// and change its value to the given Transform. If the field is not found, an error message is logged.
        /// </remarks>
        public static void SetSpawnPosition(this NetworkManager networkManager, Transform newTransform)
        {
            Type objectType = networkManager.GetType();

            // Get the field fixedPositionToSpawn using Reflection
            FieldInfo fixedPositionField = objectType.GetField("fixedPositionToSpawn", BindingFlags.Instance | BindingFlags.NonPublic);

            if (fixedPositionField != null)
            {
                fixedPositionField.SetValue(networkManager, newTransform);
            }
            else
            {
                Debug.LogError("Field fixedPositionToSpawn not found!");
            }
        }

        /// <summary>
        /// Sets the player spawn mode for the NetworkManager.
        /// </summary>
        /// <param name="networkManager">The NetworkManager to set the spawn mode for.</param>
        /// <param name="mode">The new player spawn mode to set.</param>
        /// <remarks>
        /// This method uses Reflection to access the private field "playerSpawnMode" of the NetworkManager
        /// and updates its value to the specified mode. If the field is not found, an error message is logged.
        /// </remarks>
        public static void SetSpawnMode(this NetworkManager networkManager, NetworkPlayerSpawnMode mode)
        {
            Type objectType = networkManager.GetType();

            FieldInfo spawnModeField = objectType.GetField("playerSpawnMode", BindingFlags.Instance | BindingFlags.NonPublic);

            if (spawnModeField != null)
            {
                spawnModeField.SetValue(networkManager, mode);
            }
            else
            {
                Debug.LogError("Field playerSpawnMode not found!");
            }
        }
    }
}
using System;
using System.Collections.Generic;
using HMSUnitySDK.Utils;
using SocketIOClient;
using UnityEngine;
using HMSUnitySDK.APIInterops;

namespace HMSUnitySDK
{
    [HMSBuildRoles(HMSRuntimeRole.Server)]
    public class HMSPlayerManager : HMSAPIInteropsBehaviour, IHMSService
    {
        #region Static

        public static class EventsNames
        {
            // Litens to:
            public static readonly string PlayerJoinStart = "player-join-start";
            public static readonly string PlayerJoinTimedOut = "player-join-timed-out";
            public static readonly string PlayerLeft = "player-left";
            public static readonly string PlayerRemoved = "player-removed";

            // Emits:
            public static readonly string PlayerJoined = "player-joined";
            public static readonly string PlayerDisconnected = "player-disconnected";
            public static readonly string PlayerReconnected = "player-reconnected";
        }

        #endregion

        #region Fields

        private HMSAPIInterops _hms;
        private readonly Dictionary<string, HMSPlayerEntry> _bridgeTokenRegistry = new();
        private readonly Dictionary<string, HMSPlayerEntry> _joinedPlayers = new();
        private readonly Dictionary<int, string> _joinedPlayersIdentifiers = new();

        public event Action<HMSPlayerEntry> PlayerJoined;
        public event Action<HMSPlayerEntry> PlayerLeft;
        public event Action<HMSPlayerEntry> PlayerRemoved;

        #endregion

        #region IHMSService 

        public string ServiceObjectName => "Player Manager";
        public override void InitializeService() { base.InitializeService(); }
        public bool ValidateService() => true;

        #endregion


        #region Behaviour

        #endregion

        #region Players

        /// <summary>
        /// Joins a player to the HMS-GAME-SERVER using the provided bridge token.
        /// </summary>
        /// <param name="bridgeToken">The bridge token to use for joining the player.</param>
        /// <returns>True if the player was joined successfully, otherwise false.</returns>
        /// <remarks>
        /// The bridge token is removed from the registry after a successful join.
        /// </remarks>
        public bool Join(string bridgeToken)
        {
            if (!_bridgeTokenRegistry.TryGetValue(bridgeToken, out var entry))
            {
                Debug.Log($"-HMSUnitySDK | HMSPlayerManager | Invalid bridge token: {bridgeToken}");
                return false;
            }

            _joinedPlayers.Remove(entry.hmsUser.identifier);
            _joinedPlayers.Add(entry.hmsUser.identifier, entry);
            _joinedPlayersIdentifiers.Remove(entry.hmsUser.hmsPlayerID);
            _joinedPlayersIdentifiers.Add(entry.hmsUser.hmsPlayerID, entry.hmsUser.identifier);
            _bridgeTokenRegistry.Remove(bridgeToken);

            _hms.Socket.Emit(EventsNames.PlayerJoined, new PlayerJoinedPayload()
            {
                bridgeToken = bridgeToken,
            });

            PlayerJoined?.Invoke(entry);

            Debug.Log($"-HMSUnitySDK | HMSPlayerManager | Player {entry.hmsUser.identifier} logged in successfully.");

            return true;
        }

        /// <summary>
        /// Generates a dummy token entry for a player in editor mode.
        /// </summary>
        /// <param name="identifier">The unique identifier for the player.</param>
        /// <param name="bridgeToken">The bridge token associated with the player.</param>
        /// <returns>The bridge token that was added to the registry.</returns>
        /// <remarks>
        /// This method is only valid in editor mode and adds a new player entry to the bridge token registry.
        /// </remarks>
        public string GenerateDummyTokenEntry(string identifier, string bridgeToken)
        {
            HMSValidations.ValidateRuntimeMode(HMSRuntimeMode.Editor);
            int hmsPlayerID = _joinedPlayers.Count + 1;
            _bridgeTokenRegistry.Add(bridgeToken, new HMSPlayerEntry()
            {
                hmsUser = new HMSUserID()
                {
                    identifier = identifier,
                    hmsPlayerID = hmsPlayerID
                },
                player = default,
            });
            return bridgeToken;
        }

        /// <summary>
        /// Retrieves the player associated with a given HMS Player ID.
        /// </summary>
        /// <param name="hmsPlayerID">The HMS Player ID of the player to retrieve.</param>
        /// <param name="player">The player associated with the specified HMS Player ID, or null if not found.</param>
        /// <returns>True if the player was found, false otherwise.</returns>
        public bool TryGetPlayer(int hmsPlayerID, out HMSPlayerEntry player)
        {
            if (!_joinedPlayersIdentifiers.TryGetValue(hmsPlayerID, out var identifier))
            {
                player = null;
                return false;
            }

            return TryGetPlayer(identifier, out player);
        }

        /// <summary>
        /// Retrieves the player associated with a given identifier.
        /// </summary>
        /// <param name="identifier">The unique identifier of the player.</param>
        /// <param name="player">The player associated with the specified identifier, or null if not found.</param>
        /// <returns>True if the player was found, false otherwise.</returns>
        public bool TryGetPlayer(string identifier, out HMSPlayerEntry player)
        {
            return _joinedPlayers.TryGetValue(identifier, out player);
        }

        /// <summary>
        /// Retrieves the HMS Player ID associated with a given identifier.
        /// </summary>
        /// <param name="identifier">The unique identifier of the player.</param>
        /// <returns>The HMS Player ID of the player.</returns>
        /// <exception cref="ArgumentException">Thrown when the player with the specified identifier is not found.</exception>
        public int GetHMSPlayerID(string identifier)
        {
            if (!_joinedPlayers.TryGetValue(identifier, out var entry))
            {
                throw new ArgumentException($"Player with identifier {identifier} not found.");
            }

            return entry.hmsUser.hmsPlayerID;
        }

        /// <summary>
        /// Removes the player with the specified identifier from the joined players registry.
        /// </summary>
        /// <param name="identifier">The unique identifier of the player to remove.</param>
        /// <remarks>
        /// This method does not remove the bridge token from the bridge token registry.
        /// </remarks>
        public void RemovePlayer(string identifier)
        {
            if (!_joinedPlayers.TryGetValue(identifier, out var entry)) return;
            _joinedPlayers.Remove(identifier);
            _joinedPlayersIdentifiers.Remove(entry.hmsUser.hmsPlayerID);
        }

        /// <summary>
        /// Removes the player with the specified HMS Player ID from the joined players registry.
        /// </summary>
        /// <param name="hmsPlayerID">The HMS Player ID of the player to remove.</param>
        public void RemovePlayer(int hmsPlayerID)
        {
            if (!_joinedPlayersIdentifiers.TryGetValue(hmsPlayerID, out var identifier))
            {
                return;
            }
            _joinedPlayersIdentifiers.Remove(hmsPlayerID);
            _joinedPlayers.Remove(identifier);
        }

        #endregion

        #region HMS Connection        

        protected override void HandleConnection(HMSAPIInterops hms)
        {
            _hms = hms;

            // Register on HMS API events.
            _hms.Socket.OnUnityThread(EventsNames.PlayerJoinStart, OnHMSPlayerJoinStart);
            _hms.Socket.OnUnityThread(EventsNames.PlayerJoinTimedOut, OnHMSPlayerJoinTimedOut);
            _hms.Socket.OnUnityThread(EventsNames.PlayerLeft, OnHMSPlayerLeft);
            _hms.Socket.OnUnityThread(EventsNames.PlayerRemoved, OnHMSPlayerRemoved);
        }

        private void OnHMSAPIDisconnection()
        {

        }

        #endregion

        #region Callbacks

        // Events

        private void OnHMSPlayerJoinStart(SocketIOResponse response)
        {
            Debug.Log($"+HMSPlayerManager | Received PlayerJoinStart event");
            var data = response.GetValue<PlayerJoinStartData>();
            if (!_bridgeTokenRegistry.TryAdd(data.bridgeToken, data.entry))
            {
                _bridgeTokenRegistry[data.bridgeToken] = data.entry;
            }

            _ = response.CallbackAsync();

            Debug.Log($"-HMSUnitySDK | HMSPlayerManager | Registered bridge token {data.bridgeToken} for player {data.entry.hmsUser.hmsPlayerID}");
        }

        private void OnHMSPlayerJoinTimedOut(SocketIOResponse response)
        {
            var data = response.GetValue<PlayerJoinTimedOutData>();
            if (!_bridgeTokenRegistry.ContainsKey(data.bridgeToken)) return;
            _bridgeTokenRegistry.Remove(data.bridgeToken);
        }

        private void OnHMSPlayerLeft(SocketIOResponse response)
        {
            var data = response.GetValue<PlayerLeftData>();
            if (!_joinedPlayers.TryGetValue(data.hmsUser.identifier, out var playerEntry))
            {
                return;
            }
            _joinedPlayers.Remove(data.hmsUser.identifier);
            PlayerLeft?.Invoke(playerEntry);
        }

        private void OnHMSPlayerRemoved(SocketIOResponse response)
        {
            var data = response.GetValue<PlayerRemovedData>();
            if (!_joinedPlayers.TryGetValue(data.hmsUser.identifier, out var playerEntry))
            {
                return;
            }

            _joinedPlayers.Remove(data.hmsUser.identifier);
            PlayerRemoved?.Invoke(playerEntry);
        }

        #endregion

        #region Classes and Structs

        // Data that comes from HMS-API:

        [Serializable]
        private struct PlayerJoinStartData
        {
            public string bridgeToken;
            public HMSPlayerEntry entry;
        }

        [Serializable]
        private struct PlayerJoinTimedOutData
        {
            public string bridgeToken;
        }

        [Serializable]
        private struct PlayerLeftData
        {
            public HMSUserID hmsUser;
        }

        [Serializable]
        private struct PlayerRemovedData
        {
            public HMSUserID hmsUser;
        }

        // Payloads that are sent to HMS-API:

        [Serializable]
        private struct PlayerJoinedPayload
        {
            public string bridgeToken;
        }

        // TODO: Find a better place for these

        [Serializable]
        public class HMSPlayerEntry
        {
            public HMSUserID hmsUser;
            public object player;
        }

        [Serializable]
        public struct HMSUserID
        {
            public string identifier;
            public int hmsPlayerID;
        }

        #endregion
    }
}
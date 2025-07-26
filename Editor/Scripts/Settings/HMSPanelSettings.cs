using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;
using HMSUnitySDK.Utils;

namespace HMSUnitySDK.Editor
{
    [FilePath("HMSUnitySDK/HMSPanelSettings", FilePathAttribute.Location.PreferencesFolder)]
    public class HMSPanelSettings : ScriptableSingleton<HMSPanelSettings>
    {
        [SerializeField] private BuildProfileRegistry _buildProfiles = new();
        [SerializeField] private string _usernameOrEmail;
        [SerializeField] private string _password;
        [SerializeField] private string _lastInstanceID;

        public HMSBuildProfile GetBuildProfile(HMSRuntimeRole role)
        {
            if (_buildProfiles.TryGetValue(role, out var profile))
            {
                return profile;
            }

            profile = new HMSBuildProfile();
            _buildProfiles.Add(role, profile);
            return profile;
        }

        public string UsernameOrEmail
        {
            get => _usernameOrEmail;
            set
            {
                _usernameOrEmail = value;
                Save();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                Save();
            }
        }

        public string LastInstanceID
        {
            get => _lastInstanceID;
            set
            {
                _lastInstanceID = value;
                Save();
            }
        }

        public void Save()
        {
            Save(true);
        }

        [System.Serializable]
        private class BuildProfileRegistry : SerializableDictionary<HMSRuntimeRole, HMSBuildProfile> { }
    }

    [System.Serializable]
    public class HMSBuildProfile
    {
        [SerializeField] private BuildProfile _unityBuildProfile;
        [SerializeField] private string _lastBuildPath;

        public BuildProfile UnityBuildProfile
        {
            get => _unityBuildProfile;
            set => _unityBuildProfile = value;
        }

        public string LastBuildPath
        {
            get => _lastBuildPath;
            set => _lastBuildPath = value;
        }
    }
}
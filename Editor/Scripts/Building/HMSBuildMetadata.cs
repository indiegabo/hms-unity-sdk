using UnityEditor;

namespace HMSUnitySDK.Editor
{
    [System.Serializable]
    public struct HMSBuildMetadata
    {
        public string ProductName { get; set; }
        public string Version { get; set; }
        public string Path { get; set; }
        public string ExecutableName { get; set; }
        public ulong Size { get; set; }
        public BuildTarget Platform { get; set; }
        public System.DateTime BuiltAt { get; set; }
    }
}
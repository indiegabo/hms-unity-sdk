using System.Threading.Tasks;
using UnityEngine;

namespace HMSUnitySDK.Editor
{
    public interface IHMSBuildOperation
    {
        string Name { get; }
        Task Prebuild();
        Task Postbuild(HMSBuildMetadata metadata);
    }
}
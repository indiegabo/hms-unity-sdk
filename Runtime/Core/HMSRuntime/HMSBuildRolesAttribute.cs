namespace HMSUnitySDK
{
    public class HMSBuildRolesAttribute : System.Attribute
    {
        public HMSRuntimeRole[] Roles { get; }

        public HMSBuildRolesAttribute(params HMSRuntimeRole[] roles)
        {
            Roles = roles;
        }
    }
}
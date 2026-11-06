namespace Application.Common.Permissions;

/// <summary>
/// Central permission key catalog. Extend per module.
/// </summary>
public static class SystemPermissions
{
    public static class Users
    {
        public const string View = "Users.View";
        public const string Manage = "Users.Manage";
    }

    public static class Devices
    {
        public const string Manage = "Devices.Manage";
    }
}

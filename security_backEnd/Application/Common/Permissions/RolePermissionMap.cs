using Domain.Constants;

namespace Application.Common.Permissions;

/// <summary>
/// Source of effective permissions for T-02: role codes mapped to permission keys.
/// ADMIN receives Users.View/Users.Manage and Devices.Manage (registration/admin device ops).
/// OWNER does not. Device operational control (ARM/DISARM/...) is never granted by ADMIN role alone.
/// </summary>
public static class RolePermissionMap
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Map =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [DbStatusCodes.RoleCodes.Admin] = new HashSet<string>(StringComparer.Ordinal)
            {
                SystemPermissions.Users.View,
                SystemPermissions.Users.Manage,
                SystemPermissions.Devices.Manage
            },
            [DbStatusCodes.RoleCodes.Owner] = new HashSet<string>(StringComparer.Ordinal)
        };

    public static IReadOnlySet<string> GetPermissionsForRoles(IEnumerable<string> roleCodes)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var role in roleCodes)
        {
            if (Map.TryGetValue(role, out var perms))
                set.UnionWith(perms);
        }

        return set;
    }

    public static bool HasAll(IEnumerable<string> roleCodes, params string[] required)
    {
        if (required.Length == 0)
            return false;

        var effective = GetPermissionsForRoles(roleCodes);
        return required.All(effective.Contains);
    }
}

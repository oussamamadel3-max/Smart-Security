using Application.Common.Permissions;
using Domain.Constants;

namespace Application.Tests;

public sealed class AdminPermissionMapTests
{
    [Fact]
    public void Admin_Has_Overview_Audit_And_Alert_Permissions_Without_Owner_Grants()
    {
        var admin = RolePermissionMap.GetPermissionsForRoles(new[] { DbStatusCodes.RoleCodes.Admin });
        Assert.Contains(SystemPermissions.Users.View, admin);
        Assert.Contains(SystemPermissions.Devices.View, admin);
        Assert.Contains(SystemPermissions.Devices.Manage, admin);
        Assert.Contains(SystemPermissions.AuditLogs.View, admin);
        Assert.Contains(SystemPermissions.AdminAlerts.View, admin);
        Assert.Contains(SystemPermissions.AdminAlerts.Manage, admin);

        var owner = RolePermissionMap.GetPermissionsForRoles(new[] { DbStatusCodes.RoleCodes.Owner });
        Assert.Empty(owner);
        Assert.False(RolePermissionMap.HasAll(new[] { DbStatusCodes.RoleCodes.Owner }, SystemPermissions.AuditLogs.View));
        Assert.False(RolePermissionMap.HasAll(new[] { DbStatusCodes.RoleCodes.Owner }, SystemPermissions.Devices.View));
        Assert.False(RolePermissionMap.HasAll(new[] { DbStatusCodes.RoleCodes.Owner }, SystemPermissions.Devices.Manage));
    }

    [Fact]
    public void Device_View_And_Manage_Are_Distinct_Permission_Keys()
    {
        Assert.NotEqual(SystemPermissions.Devices.View, SystemPermissions.Devices.Manage);
        Assert.NotEqual(SystemPermissions.AdminAlerts.View, SystemPermissions.AdminAlerts.Manage);
        Assert.NotEqual(SystemPermissions.Users.View, SystemPermissions.Users.Manage);
    }

    [Fact]
    public void Admin_Role_Does_Not_Imply_Owner_Device_Command_Surface()
    {
        // Operational ARM/DISARM/MUTE/TEST are owner HTTP routes + ownership checks —
        // not granted as admin permission keys.
        var admin = RolePermissionMap.GetPermissionsForRoles(new[] { DbStatusCodes.RoleCodes.Admin });
        Assert.DoesNotContain("Devices.Command", admin);
        Assert.DoesNotContain("Devices.Arm", admin);
        Assert.DoesNotContain("Devices.Disarm", admin);
        Assert.True(admin.Contains(SystemPermissions.Devices.View));
        Assert.True(admin.Contains(SystemPermissions.Devices.Manage));
    }
}

import 'app_permissions.dart';
import 'app_role.dart';

/// Route/role access policy wired to [AppCoreConfig.roleAllows].
///
/// ```dart
/// enum StaffRole implements AppRoleContract { ... }
/// enum AppSection implements AppPermissionContract { ... }
///
/// final rolePolicy = RoleAccessPolicy<StaffRole, AppSection>(
///   routeToPermission: kRouteSectionMap,
///   rolePermissions: kRoleMenuAccess,
///   parseRole: StaffRole.values.fromCode,
/// );
/// ```
class RoleAccessPolicy<
  TRole extends AppRoleContract,
  TPermission extends AppPermissionContract
> {
  RoleAccessPolicy({
    required AppRoutePermissionMap<TPermission> routeToPermission,
    required AppPermissionMatrix<TRole, TPermission> rolePermissions,
    required AppRoleFromCode<TRole> parseRole,
  }) : _routeToPermission = routeToPermission,
       _rolePermissions = rolePermissions,
       _parseRole = parseRole;

  /// Builds a policy from a fixed role list (uses [AppRoleLookup.fromCode]).
  factory RoleAccessPolicy.fromRoles({
    required Iterable<TRole> roles,
    required AppRoutePermissionMap<TPermission> routeToPermission,
    required AppPermissionMatrix<TRole, TPermission> rolePermissions,
  }) {
    return RoleAccessPolicy<TRole, TPermission>(
      routeToPermission: routeToPermission,
      rolePermissions: rolePermissions,
      parseRole: roles.fromCode,
    );
  }

  final AppRoutePermissionMap<TPermission> _routeToPermission;
  final AppPermissionMatrix<TRole, TPermission> _rolePermissions;
  final AppRoleFromCode<TRole> _parseRole;

  /// Used by [AppCoreConfig.roleAllows] and GoRouter redirect.
  bool allows(String? roleCode, String path) {
    final permission = _permissionForPath(path);
    if (permission == null) return true;

    final role = _parseRole(roleCode);
    if (role == null) return false;

    return canAccess(role, permission);
  }

  bool canAccess(TRole role, TPermission permission) =>
      role.canAccess(permission, _rolePermissions);

  TPermission? _permissionForPath(String path) {
    MapEntry<String, TPermission>? matched;

    for (final entry in _routeToPermission.entries) {
      if (path.startsWith(entry.key)) {
        matched = entry;
        break;
      }
    }

    return matched?.value;
  }
}

import 'app_role.dart';

/// Contract for a coarse navigation/feature permission (menu section, module).
///
/// Fine-grained actions (approve, delete) stay inside each feature.
///
/// ```dart
/// enum AppSection implements AppPermissionContract {
///   settings('settings');
///   const AppSection(this.key);
///   @override
///   final String key;
/// }
/// ```
abstract interface class AppPermissionContract {
  String get key;
}

/// Role → allowed permissions matrix used by routing and home menus.
typedef AppPermissionMatrix<
  TRole extends AppRoleContract,
  TPermission extends AppPermissionContract
> = Map<TRole, Set<TPermission>>;

/// Route prefix → permission (e.g. `/home/settings` → settings section).
typedef AppRoutePermissionMap<TPermission extends AppPermissionContract> =
    Map<String, TPermission>;

extension AppRolePermissionX<
  TRole extends AppRoleContract,
  TPermission extends AppPermissionContract
>
    on TRole {
  bool canAccess(
    TPermission permission,
    AppPermissionMatrix<TRole, TPermission> matrix,
  ) =>
      matrix[this]?.contains(permission) ?? false;
}

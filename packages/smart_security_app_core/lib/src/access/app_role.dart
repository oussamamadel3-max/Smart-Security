/// Contract for an application role.
///
/// Host apps implement this on an `enum`, e.g. OWNER / ADMIN for Smart Security.
abstract interface class AppRoleContract {
  String get code;
}

/// Resolves a session/API role code to [AppRoleContract].
typedef AppRoleFromCode<T extends AppRoleContract> = T? Function(String? roleCode);

/// Finds a role by [code] in a fixed role list.
extension AppRoleLookup<T extends AppRoleContract> on Iterable<T> {
  T? fromCode(String? code) {
    if (code == null) return null;
    for (final role in this) {
      if (role.code == code) return role;
    }
    return null;
  }
}

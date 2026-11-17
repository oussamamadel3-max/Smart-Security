/// Verified Backend Auth routes (`AuthController` → `api/auth`).
///
/// Host apps may wrap these in their own endpoint classes for T-02.
/// Do not invent device/command endpoints here.
abstract final class DefaultAuthPaths {
  static const login = '/api/auth/login';
  static const refresh = '/api/auth/refresh';
  static const logout = '/api/auth/logout';
  static const logoutAll = '/api/auth/logout-all';
  static const me = '/api/auth/me';
}

import 'package:smart_security_app_core/smart_security_app_core.dart';

/// 401/403/404 must not leave a previous owner's row on screen.
bool historyAccessDropped(Failure failure) =>
    failure.requiresReauthentication ||
    failure is ForbiddenFailure ||
    failure is NotFoundFailure;

/// Logout, auth loss, and account switch drop retained history.
bool historyCacheMustClear({
  required bool wasAuthenticated,
  required bool isAuthenticated,
  required String? previousUserId,
  required String? nextUserId,
}) {
  if (wasAuthenticated && !isAuthenticated) return true;
  if (previousUserId != null &&
      nextUserId != null &&
      previousUserId != nextUserId) {
    return true;
  }
  return false;
}

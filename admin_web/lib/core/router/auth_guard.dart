import '../../features/auth/domain/entities/auth_user_entity.dart';
import '../session/auth_session_state.dart';
import 'app_routes.dart';

/// Sole auth-driven navigation decision for GoRouter.redirect.
///
/// Role split is a UX hint from the session user. It is not authorization:
/// the server still enforces OWNER vs ADMIN, and a 403 must not log the user out.
String? authGuardRedirect({
  required AuthSessionState session,
  required String matchedLocation,
}) {
  final onLogin = matchedLocation == AppRoutes.login;
  final onSplash = matchedLocation == AppRoutes.splash;

  if (session.status == AuthStatus.unknown || onSplash) {
    return null;
  }

  if (session.shouldForceLogin) {
    return onLogin ? null : AppRoutes.login;
  }

  final user = session.user;
  if (session.isAuthenticated && user != null) {
    if (onLogin) {
      return homeRouteFor(user);
    }

    if (matchedLocation.startsWith(AppRoutes.adminHome) && !user.isAdmin) {
      return AppRoutes.ownerHome;
    }
    if (matchedLocation.startsWith(AppRoutes.ownerHome) && !user.isOwner) {
      return AppRoutes.adminHome;
    }
  }

  return null;
}

/// Default post-login home. Admin is never sent to owner ops by default.
String homeRouteFor(AuthUserEntity user) {
  if (user.isAdmin) return AppRoutes.adminHome;
  return AppRoutes.ownerHome;
}

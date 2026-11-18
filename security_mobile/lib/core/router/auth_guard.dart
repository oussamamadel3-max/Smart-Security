import '../session/auth_session_state.dart';
import 'app_routes.dart';

/// Sole auth-driven navigation decision for GoRouter.redirect.
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
    // Drop any deep-link into owner device screens after auth loss.
    return onLogin ? null : AppRoutes.login;
  }

  if (session.isAuthenticated && onLogin) {
    return AppRoutes.home;
  }

  return null;
}

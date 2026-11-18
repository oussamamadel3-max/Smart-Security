import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/core/router/app_routes.dart';
import 'package:security_mobile/core/router/auth_guard.dart';
import 'package:security_mobile/core/session/auth_session_state.dart';
import 'package:security_mobile/features/auth/domain/entities/auth_user_entity.dart';

void main() {
  test('unauthenticated users are sent to login', () {
    final redirect = authGuardRedirect(
      session: const AuthSessionState(status: AuthStatus.unauthenticated),
      matchedLocation: AppRoutes.home,
    );
    expect(redirect, AppRoutes.login);
  });

  test('authenticated users leave login for home', () {
    final redirect = authGuardRedirect(
      session: const AuthSessionState(
        status: AuthStatus.authenticated,
        user: AuthUserEntity(id: '1', roles: ['OWNER']),
      ),
      matchedLocation: AppRoutes.login,
    );
    expect(redirect, AppRoutes.home);
  });

  test('sessionExpired forces login without treating 403 specially here', () {
    final redirect = authGuardRedirect(
      session: const AuthSessionState(status: AuthStatus.sessionExpired),
      matchedLocation: AppRoutes.home,
    );
    expect(redirect, AppRoutes.login);
  });
}

import 'package:admin_web/core/router/app_routes.dart';
import 'package:admin_web/core/router/auth_guard.dart';
import 'package:admin_web/core/session/auth_session_state.dart';
import 'package:admin_web/features/auth/domain/entities/auth_user_entity.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  const owner = AuthUserEntity(id: '1', roles: ['OWNER']);
  const admin = AuthUserEntity(id: '2', roles: ['ADMIN']);

  test('sessionExpired forces login', () {
    final redirect = authGuardRedirect(
      session: const AuthSessionState(status: AuthStatus.sessionExpired),
      matchedLocation: AppRoutes.adminAccounts,
    );
    expect(redirect, AppRoutes.login);
  });

  test('unauthenticated forces login', () {
    final redirect = authGuardRedirect(
      session: const AuthSessionState(status: AuthStatus.unauthenticated),
      matchedLocation: AppRoutes.ownerHome,
    );
    expect(redirect, AppRoutes.login);
  });

  test('owner on login goes to owner home', () {
    final redirect = authGuardRedirect(
      session: const AuthSessionState(
        status: AuthStatus.authenticated,
        user: owner,
      ),
      matchedLocation: AppRoutes.login,
    );
    expect(redirect, AppRoutes.ownerHome);
  });

  test('admin on login goes to admin home by default', () {
    final redirect = authGuardRedirect(
      session: const AuthSessionState(
        status: AuthStatus.authenticated,
        user: admin,
      ),
      matchedLocation: AppRoutes.login,
    );
    expect(redirect, AppRoutes.adminHome);
  });

  test('owner cannot open admin path', () {
    final redirect = authGuardRedirect(
      session: const AuthSessionState(
        status: AuthStatus.authenticated,
        user: owner,
      ),
      matchedLocation: AppRoutes.adminHome,
    );
    expect(redirect, AppRoutes.ownerHome);
  });

  test('admin cannot open owner path', () {
    final redirect = authGuardRedirect(
      session: const AuthSessionState(
        status: AuthStatus.authenticated,
        user: admin,
      ),
      matchedLocation: AppRoutes.ownerHome,
    );
    expect(redirect, AppRoutes.adminHome);
  });

  test('homeRouteFor prefers admin', () {
    expect(homeRouteFor(admin), AppRoutes.adminHome);
    expect(homeRouteFor(owner), AppRoutes.ownerHome);
  });

  test('admin cannot open owner device command route', () {
    final redirect = authGuardRedirect(
      session: const AuthSessionState(
        status: AuthStatus.authenticated,
        user: admin,
      ),
      matchedLocation: AppRoutes.ownerDeviceDetailPath('device-1'),
    );
    expect(redirect, AppRoutes.adminHome);
  });

  test('owner stays on owner device detail', () {
    final redirect = authGuardRedirect(
      session: const AuthSessionState(
        status: AuthStatus.authenticated,
        user: owner,
      ),
      matchedLocation: AppRoutes.ownerDeviceDetailPath('device-1'),
    );
    expect(redirect, isNull);
  });

  test('admin cannot open owner history route', () {
    final redirect = authGuardRedirect(
      session: const AuthSessionState(
        status: AuthStatus.authenticated,
        user: admin,
      ),
      matchedLocation: AppRoutes.ownerDeviceHistoryPath('device-1'),
    );
    expect(redirect, AppRoutes.adminHome);
  });

  test('owner cannot open nested admin account or device routes', () {
    const session = AuthSessionState(
      status: AuthStatus.authenticated,
      user: owner,
    );
    expect(
      authGuardRedirect(
        session: session,
        matchedLocation: AppRoutes.adminAccounts,
      ),
      AppRoutes.ownerHome,
    );
    expect(
      authGuardRedirect(
        session: session,
        matchedLocation: AppRoutes.adminDevicePath('device-1'),
      ),
      AppRoutes.ownerHome,
    );
    expect(
      authGuardRedirect(
        session: session,
        matchedLocation: AppRoutes.adminDeviceLinkPath('device-1'),
      ),
      AppRoutes.ownerHome,
    );
    expect(
      authGuardRedirect(
        session: session,
        matchedLocation: AppRoutes.adminAuditLogPath('112'),
      ),
      AppRoutes.ownerHome,
    );
    expect(
      authGuardRedirect(
        session: session,
        matchedLocation: AppRoutes.adminAlertPath('alert-1'),
      ),
      AppRoutes.ownerHome,
    );
  });

  test('admin stays on nested admin routes', () {
    const session = AuthSessionState(
      status: AuthStatus.authenticated,
      user: admin,
    );
    expect(
      authGuardRedirect(
        session: session,
        matchedLocation: AppRoutes.adminAccountNew,
      ),
      isNull,
    );
    expect(
      authGuardRedirect(
        session: session,
        matchedLocation: AppRoutes.adminDeviceTransferPath('device-1'),
      ),
      isNull,
    );
    expect(
      authGuardRedirect(
        session: session,
        matchedLocation: AppRoutes.adminAuditLogs,
      ),
      isNull,
    );
    expect(
      authGuardRedirect(
        session: session,
        matchedLocation: AppRoutes.adminAlertNew,
      ),
      isNull,
    );
  });

  test('owner stays on history and event detail routes', () {
    const session = AuthSessionState(
      status: AuthStatus.authenticated,
      user: owner,
    );
    expect(
      authGuardRedirect(
        session: session,
        matchedLocation: AppRoutes.ownerDeviceHistoryPath('device-1'),
      ),
      isNull,
    );
    expect(
      authGuardRedirect(
        session: session,
        matchedLocation: AppRoutes.ownerDeviceAlarmEventPath(
          'device-1',
          'ev-1',
        ),
      ),
      isNull,
    );
  });
}

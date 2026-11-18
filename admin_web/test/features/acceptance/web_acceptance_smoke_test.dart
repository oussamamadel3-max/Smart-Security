import 'package:admin_web/core/network/web_auth_constants.dart';
import 'package:admin_web/core/router/app_routes.dart';
import 'package:admin_web/core/router/auth_guard.dart';
import 'package:admin_web/core/session/access_token_holder.dart';
import 'package:admin_web/core/session/auth_session_state.dart';
import 'package:admin_web/features/auth/domain/entities/auth_user_entity.dart';
import 'package:admin_web/features/devices/domain/entities/device_command_entity.dart';
import 'package:admin_web/features/devices/domain/history_session_policy.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

/// Software-only acceptance smoke for WEB-T-08.
///
/// Live CSRF/Origin/CORS, account disable, G1/ACK, ownership transfer, FCM,
/// and an interactive browser against a real Backend are out of this file.
void main() {
  const owner = AuthUserEntity(id: 'owner-1', roles: ['OWNER']);
  const admin = AuthUserEntity(id: 'admin-1', roles: ['ADMIN']);

  AuthSessionState sessionFor(AuthUserEntity user) {
    return AuthSessionState(status: AuthStatus.authenticated, user: user);
  }

  test('owner journey stays on device, history, and command routes', () {
    expect(
      authGuardRedirect(
        session: const AuthSessionState(status: AuthStatus.unauthenticated),
        matchedLocation: AppRoutes.ownerHome,
      ),
      AppRoutes.login,
    );
    expect(
      authGuardRedirect(
        session: const AuthSessionState(status: AuthStatus.sessionExpired),
        matchedLocation: AppRoutes.ownerDeviceDetailPath('device-1'),
      ),
      AppRoutes.login,
    );

    final ownerSession = sessionFor(owner);
    expect(
      authGuardRedirect(
        session: ownerSession,
        matchedLocation: AppRoutes.login,
      ),
      AppRoutes.ownerHome,
    );
    expect(
      authGuardRedirect(
        session: ownerSession,
        matchedLocation: AppRoutes.ownerDeviceDetailPath('device-1'),
      ),
      isNull,
    );
    expect(
      authGuardRedirect(
        session: ownerSession,
        matchedLocation: AppRoutes.ownerDeviceHistoryPath('device-1'),
      ),
      isNull,
    );
    expect(
      authGuardRedirect(
        session: ownerSession,
        matchedLocation: AppRoutes.ownerDeviceCommandPath('device-1', 'c1'),
      ),
      isNull,
    );
  });

  test('admin journey keeps admin routes and blocks owner commands', () {
    final adminSession = sessionFor(admin);
    for (final location in [
      AppRoutes.adminAccounts,
      AppRoutes.adminDevices,
      AppRoutes.adminDevicePath('device-1'),
      AppRoutes.adminAuditLogs,
      AppRoutes.adminAlerts,
    ]) {
      expect(
        authGuardRedirect(session: adminSession, matchedLocation: location),
        isNull,
        reason: location,
      );
    }

    expect(
      authGuardRedirect(
        session: adminSession,
        matchedLocation: AppRoutes.ownerDeviceDetailPath('device-1'),
      ),
      AppRoutes.adminHome,
    );
    expect(
      authGuardRedirect(
        session: adminSession,
        matchedLocation: AppRoutes.ownerDeviceCommandPath('device-1', 'c1'),
      ),
      AppRoutes.adminHome,
    );

    final ownerSession = sessionFor(owner);
    for (final location in [
      AppRoutes.adminAccounts,
      AppRoutes.adminDevices,
      AppRoutes.adminAuditLogs,
      AppRoutes.adminAlerts,
    ]) {
      expect(
        authGuardRedirect(session: ownerSession, matchedLocation: location),
        AppRoutes.ownerHome,
        reason: location,
      );
    }
  });

  test('command statuses do not treat HTTP accept as device execution', () {
    final pending = _command(CommandStatus.pending);
    expect(pending.isPendingRequest, isTrue);
    expect(pending.status.isConfirmedExecution, isFalse);
    expect(pending.actualStateChanged, isFalse);

    final executed = _command(
      CommandStatus.executed,
      actualStateChanged: true,
      resultStateSequence: 4,
    );
    expect(executed.status.isConfirmedExecution, isTrue);
    expect(executed.isTerminal, isTrue);
    expect(executed.resultStateSequence, 4);

    final rejected = _command(CommandStatus.rejected, rejectCode: 'STALE');
    expect(rejected.status.isConfirmedExecution, isFalse);
    expect(rejected.isTerminal, isTrue);
    expect(rejected.rejectCode, 'STALE');

    final timedOut = _command(CommandStatus.timedOut);
    expect(timedOut.status.isUnresolvedTimeout, isTrue);
    expect(timedOut.blocksConflictingUntilReconciled, isTrue);
    expect(timedOut.status.isConfirmedExecution, isFalse);

    final reconciled = _command(
      CommandStatus.timedOut,
      reconciledAt: DateTime.utc(2026, 11, 18),
    );
    expect(reconciled.blocksConflictingUntilReconciled, isFalse);
    expect(reconciled.status.isConfirmedExecution, isFalse);

    expect(CommandStatus.parse('NOT_A_STATUS').isConfirmedExecution, isFalse);
  });

  test('logout drops in-memory access and history cache', () {
    final access = AccessTokenHolder()..setToken('access-jwt');
    expect(access.hasAccessToken, isTrue);
    access.clear();
    expect(access.hasAccessToken, isFalse);
    expect(access.accessToken, isNull);

    expect(
      historyCacheMustClear(
        wasAuthenticated: true,
        isAuthenticated: false,
        previousUserId: owner.id,
        nextUserId: null,
      ),
      isTrue,
    );
    expect(WebAuthConstants.csrfHeaderName, 'X-CSRF-TOKEN');
    expect(historyAccessDropped(const Failure.forbidden()), isTrue);
  });
}

DeviceCommandEntity _command(
  CommandStatus status, {
  bool actualStateChanged = false,
  int? resultStateSequence,
  String? rejectCode,
  DateTime? reconciledAt,
}) {
  return DeviceCommandEntity(
    commandId: 'c1',
    deviceId: 'device-1',
    type: CommandType.arm,
    status: status,
    commandSequence: 1,
    ownershipVersion: 1,
    observedStateSequence: 1,
    createdAt: DateTime.utc(2026, 11, 18),
    expiresAt: DateTime.utc(2026, 11, 18, 0, 1),
    actualStateChanged: actualStateChanged,
    idempotentReplay: false,
    resultStateSequence: resultStateSequence,
    rejectCode: rejectCode,
    reconciledAt: reconciledAt,
  );
}

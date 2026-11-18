import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/core/router/app_routes.dart';
import 'package:security_mobile/core/router/auth_guard.dart';
import 'package:security_mobile/core/session/auth_session_state.dart';
import 'package:security_mobile/features/devices/domain/entities/device_command_entity.dart';
import 'package:security_mobile/features/devices/domain/entities/owner_device_entity.dart';
import 'package:security_mobile/features/notifications/domain/usecases/open_notification_usecase.dart';
import 'package:security_mobile/features/wifi_provisioning/domain/local_provisioning_client.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

/// Compact acceptance smoke covering critical MOB-T-08 client invariants.
void main() {
  test('auth guard sends unauthenticated users to login', () {
    final redirect = authGuardRedirect(
      session: const AuthSessionState(status: AuthStatus.unauthenticated),
      matchedLocation: AppRoutes.home,
    );
    expect(redirect, AppRoutes.login);
  });

  test('PENDING device stays limited', () {
    const device = OwnerDeviceEntity(
      deviceId: 'd1',
      displayName: 'غرفة',
      ownershipStatus: OwnershipStatus.pending,
      ownershipVersion: 1,
      confirmedOwnershipVersion: 0,
      awaitingOwnershipAck: true,
      canControl: false,
      linkStatusMessage: 'الربط قيد الاكتمال',
    );
    expect(device.showsLimitedPendingView, isTrue);
  });

  test('PENDING command is not confirmed execution', () {
    final command = DeviceCommandEntity(
      commandId: 'c1',
      deviceId: 'd1',
      type: CommandType.arm,
      status: CommandStatus.pending,
      commandSequence: 1,
      ownershipVersion: 1,
      observedStateSequence: 0,
      createdAt: DateTime.now(),
      expiresAt: DateTime.now().add(const Duration(seconds: 30)),
      actualStateChanged: false,
      idempotentReplay: false,
    );
    expect(command.status.isConfirmedExecution, isFalse);
  });

  test('Wi-Fi path never invents connected without contract', () async {
    final client = UnavailableLocalProvisioningClient();
    final outcome = await client.submitWifi(
      const LocalWifiCredentials(ssid: 'x', password: 'y'),
    );
    expect(outcome, isNot(LocalProvisioningOutcome.connected));
  });

  test('open-notification sealed results cover denied path type', () {
    expect(const OpenNotificationUnsupported(), isA<OpenNotificationResult>());
  });
}

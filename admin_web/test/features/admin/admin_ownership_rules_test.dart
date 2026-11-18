import 'package:admin_web/features/admin/data/mappers/admin_mapper.dart';
import 'package:admin_web/features/admin/domain/admin_ownership_view.dart';
import 'package:admin_web/features/admin/domain/create_owner_input.dart';
import 'package:admin_web/features/admin/domain/register_device_input.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

void main() {
  test('pending link is not classified as active transfer', () {
    final pending = AdminDeviceMapper.toEntity(_device(status: 'PENDING'));
    final activeWaiting = AdminDeviceMapper.toEntity(
      _device(status: 'ACTIVE', awaiting: true),
    );
    final active = AdminDeviceMapper.toEntity(
      _device(status: 'ACTIVE', awaiting: false),
    );

    expect(pending.ownershipKind, AdminOwnershipKind.pending);
    expect(activeWaiting.ownershipKind, AdminOwnershipKind.activeAwaitingAck);
    expect(active.ownershipKind, AdminOwnershipKind.active);
    expect(AdminOwnershipView.exposesOperationalCommands, isFalse);
    expect(
      AdminOwnershipView.classify(
        status: OwnershipStatus.pending,
        awaitingOwnershipAck: true,
      ),
      AdminOwnershipKind.pending,
    );
  });

  test('registration rejects guessed-empty capacities and non-object rules', () {
    final badRule = RegisterDeviceInput(
      hardwareUid: 'uid-1',
      displayName: 'مدخل',
      authType: DeviceCredentialTypes.passwordHash,
      credentialValue: 'secret-pass',
      alarmRuleJson: '[]',
      eventBufferCapacity: 10,
      commandTtlSeconds: 10,
    );
    expect(RegisterDeviceRules.validate(badRule), RegisterDeviceRules.alarmRule);

    final zeroTtl = RegisterDeviceInput(
      hardwareUid: 'uid-1',
      displayName: 'مدخل',
      authType: DeviceCredentialTypes.passwordHash,
      credentialValue: 'secret-pass',
      alarmRuleJson: '{"sensor":"vibration"}',
      eventBufferCapacity: 0,
      commandTtlSeconds: 0,
    );
    expect(
      RegisterDeviceRules.validate(zeroTtl),
      RegisterDeviceRules.eventBufferCapacity,
    );
    expect(zeroTtl.toString().contains('secret-pass'), isFalse);
  });

  test('owner create rules reject a short password before any request', () {
    expect(
      OwnerAccountRules.validateCreate(
        userName: 'owner1',
        fullName: 'أحمد',
        password: 'short',
      ),
      OwnerAccountRules.password,
    );
    final input = CreateOwnerInput(
      userName: 'owner1',
      fullName: 'أحمد',
      password: 'secret-pass',
    );
    expect(input.toString().contains('secret-pass'), isFalse);
  });
}

Map<String, dynamic> _device({
  required String status,
  bool awaiting = false,
}) {
  return {
    'deviceId': 'd1',
    'hardwareUid': 'HW-1',
    'displayName': 'المنزل',
    'lifecycleStatus': 'REGISTERED',
    'currentOwnershipVersion': 1,
    'ownershipStatus': status,
    'confirmedOwnershipVersion': awaiting ? 0 : 1,
    'awaitingOwnershipAck': awaiting,
    'indicators': {
      'connectionStatus': 'OFFLINE',
      'armState': 'UNKNOWN',
      'sirenState': 'UNKNOWN',
      'healthState': 'UNKNOWN',
      'clockTrusted': false,
    },
  };
}

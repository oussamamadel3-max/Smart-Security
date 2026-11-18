import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/features/devices/domain/entities/device_indicators_entity.dart';
import 'package:security_mobile/features/devices/domain/entities/owner_device_entity.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

void main() {
  test('PENDING link stays limited without equating offline to disarm', () {
    const device = OwnerDeviceEntity(
      deviceId: 'd1',
      displayName: 'منزل',
      ownershipStatus: OwnershipStatus.pending,
      ownershipVersion: 1,
      confirmedOwnershipVersion: 0,
      awaitingOwnershipAck: true,
      canControl: false,
      linkStatusMessage: 'الربط قيد الاكتمال',
    );

    expect(device.showsLimitedPendingView, isTrue);
    expect(device.showsOperationalIndicators, isFalse);
    expect(device.isInitialLinkPending, isTrue);
  });

  test('ACTIVE awaiting sync exposes indicators but not full control', () {
    const indicators = DeviceIndicatorsEntity(
      connection: ConnectionStatus.online,
      arm: ArmStatus.armed,
      siren: SirenStatus.off,
      health: DiagnosticStatus.unknown,
    );
    const device = OwnerDeviceEntity(
      deviceId: 'd2',
      displayName: 'مستودع',
      ownershipStatus: OwnershipStatus.active,
      ownershipVersion: 2,
      confirmedOwnershipVersion: 1,
      awaitingOwnershipAck: true,
      canControl: false,
      linkStatusMessage: 'بانتظار مزامنة إصدار الملكية',
      indicators: indicators,
    );

    expect(device.isActiveAwaitingOwnershipSync, isTrue);
    expect(device.showsOperationalIndicators, isTrue);
    expect(device.canControl, isFalse);
    expect(indicators.showsStaleWarning, isFalse);
  });

  test('offline warning is independent from arm/health', () {
    const indicators = DeviceIndicatorsEntity(
      connection: ConnectionStatus.offline,
      arm: ArmStatus.armed,
      siren: SirenStatus.unknown,
      health: DiagnosticStatus.unknown,
    );
    expect(indicators.showsStaleWarning, isTrue);
    expect(indicators.arm, ArmStatus.armed);
    expect(indicators.health, DiagnosticStatus.unknown);
  });
}

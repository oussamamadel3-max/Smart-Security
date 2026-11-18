import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/features/devices/data/mappers/owner_device_mapper.dart';
import 'package:security_mobile/features/devices/data/models/owner_device_dto.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

void main() {
  test('maps ACTIVE list item with independent indicators', () {
    final entity = OwnerDeviceMapper.toEntity(
      OwnerDeviceDto(
        deviceId: '11111111-1111-1111-1111-111111111111',
        displayName: 'منزل',
        ownershipStatus: 'ACTIVE',
        ownershipVersion: 2,
        confirmedOwnershipVersion: 2,
        awaitingOwnershipAck: false,
        canControl: true,
        linkStatusMessage: 'الملكية فعالة',
        indicators: const OwnerDeviceIndicatorsDto(
          connectionStatus: 'ONLINE',
          armState: 'ARMED',
          sirenState: 'OFF',
          healthState: 'UNKNOWN',
        ),
      ),
    );

    expect(entity.ownershipStatus, OwnershipStatus.active);
    expect(entity.indicators?.connection, ConnectionStatus.online);
    expect(entity.indicators?.arm, ArmStatus.armed);
    expect(entity.indicators?.health, DiagnosticStatus.unknown);
  });

  test('unknown health code does not become healthy', () {
    final entity = OwnerDeviceMapper.toEntity(
      const OwnerDeviceDto(
        deviceId: '22222222-2222-2222-2222-222222222222',
        displayName: 'مكتب',
        ownershipStatus: 'ACTIVE',
        ownershipVersion: 1,
        confirmedOwnershipVersion: 1,
        awaitingOwnershipAck: false,
        canControl: true,
        linkStatusMessage: 'الملكية فعالة',
        indicators: OwnerDeviceIndicatorsDto(
          connectionStatus: 'OFFLINE',
          armState: 'WEIRD',
          sirenState: 'OFF',
          healthState: 'WEIRD',
        ),
      ),
    );

    expect(entity.indicators?.arm, ArmStatus.unknown);
    expect(entity.indicators?.health, DiagnosticStatus.unknown);
    expect(entity.indicators?.connection, ConnectionStatus.offline);
  });
}

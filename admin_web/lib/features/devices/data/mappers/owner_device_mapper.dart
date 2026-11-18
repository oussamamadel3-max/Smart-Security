import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../domain/entities/device_indicators_entity.dart';
import '../../domain/entities/owner_device_entity.dart';
import '../models/owner_device_dto.dart';

abstract final class OwnerDeviceMapper {
  static OwnerDeviceEntity toEntity(OwnerDeviceDto dto) {
    return OwnerDeviceEntity(
      deviceId: dto.deviceId,
      displayName: dto.displayName,
      ownershipStatus: OwnershipStatus.parse(dto.ownershipStatus),
      ownershipVersion: dto.ownershipVersion,
      confirmedOwnershipVersion: dto.confirmedOwnershipVersion,
      awaitingOwnershipAck: dto.awaitingOwnershipAck,
      canControl: dto.canControl,
      linkStatusMessage: dto.linkStatusMessage,
      deviceLifecycleStatus: dto.deviceLifecycleStatus,
      indicators: dto.indicators == null ? null : _indicators(dto.indicators!),
    );
  }

  static DeviceIndicatorsEntity _indicators(OwnerDeviceIndicatorsDto dto) {
    return DeviceIndicatorsEntity(
      connection: ConnectionStatus.parse(dto.connectionStatus),
      arm: ArmStatus.parse(dto.armState),
      siren: SirenStatus.parse(dto.sirenState),
      health: DiagnosticStatus.parse(dto.healthState),
      healthCode: dto.healthCode,
      lastSeenAt: _parseTime(dto.lastSeenAt),
      lastStateAt: _parseTime(dto.lastStateAt),
      stateSequence: dto.stateSequence,
      clockTrusted: dto.clockTrusted,
      currentAlarmEventUid: dto.currentAlarmEventUid,
      powerSource: dto.powerSource,
      batteryPercent: dto.batteryPercent,
      heartbeatTimeoutSeconds: dto.heartbeatTimeoutSeconds,
    );
  }

  static DateTime? _parseTime(String? raw) {
    if (raw == null || raw.trim().isEmpty) return null;
    return DateTime.tryParse(raw)?.toLocal();
  }
}

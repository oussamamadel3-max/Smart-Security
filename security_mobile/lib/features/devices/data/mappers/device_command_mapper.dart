import '../../../../core/domain/device_status_codes.dart';
import '../../domain/entities/device_command_entity.dart';
import '../models/device_command_dto.dart';

abstract final class DeviceCommandMapper {
  static DeviceCommandEntity toEntity(DeviceCommandDto dto) {
    return DeviceCommandEntity(
      commandId: dto.commandId,
      deviceId: dto.deviceId,
      type: CommandType.parse(dto.type),
      status: CommandStatus.parse(dto.status),
      commandSequence: dto.commandSequence,
      ownershipVersion: dto.ownershipVersion,
      observedStateSequence: dto.observedStateSequence,
      createdAt: DateTime.tryParse(dto.createdAt)?.toLocal() ?? DateTime.now(),
      expiresAt: DateTime.tryParse(dto.expiresAt)?.toLocal() ?? DateTime.now(),
      targetAlarmEventUid: dto.targetAlarmEventUid,
      rejectCode: dto.rejectCode,
      resultStateSequence: dto.resultStateSequence,
      sentAt: DateTime.tryParse(dto.sentAt ?? '')?.toLocal(),
      acknowledgedAt: DateTime.tryParse(dto.acknowledgedAt ?? '')?.toLocal(),
      reconciledAt: DateTime.tryParse(dto.reconciledAt ?? '')?.toLocal(),
      idempotentReplay: dto.idempotentReplay,
      actualStateChanged: dto.actualStateChanged,
      confirmedArmState: dto.confirmedArmState == null
          ? null
          : ArmStatus.parse(dto.confirmedArmState),
      confirmedSirenState: dto.confirmedSirenState == null
          ? null
          : SirenStatus.parse(dto.confirmedSirenState),
    );
  }
}

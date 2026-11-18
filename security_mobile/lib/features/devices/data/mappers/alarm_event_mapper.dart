import 'package:smart_security_app_core/smart_security_app_core.dart';
import '../../domain/entities/alarm_event_entity.dart';
import '../models/alarm_event_dto.dart';

abstract final class AlarmEventMapper {
  static AlarmEventEntity toEntity(AlarmEventDto dto) {
    final occurred = DateTime.tryParse(dto.occurredDeviceAt ?? '')?.toLocal();
    return AlarmEventEntity(
      alarmEventId: dto.alarmEventId,
      deviceId: dto.deviceId,
      eventUid: dto.eventUid,
      ownershipId: dto.ownershipId,
      ownershipVersion: dto.ownershipVersion,
      status: AlarmEventStatus.parse(dto.status),
      receivedAt: DateTime.tryParse(dto.receivedAt)?.toLocal() ?? DateTime.now(),
      // Server nulls occurredDeviceAt when clockTrusted=false; never invent time.
      occurredDeviceAt: dto.clockTrusted ? occurred : null,
      clockTrusted: dto.clockTrusted,
      wasBuffered: dto.wasBuffered,
      mutedAt: DateTime.tryParse(dto.mutedAt ?? '')?.toLocal(),
      sensorKeySnapshot: dto.sensorKeySnapshot,
    );
  }
}

import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../entities/alarm_event_entity.dart';
import '../repositories/device_alarm_events_repository.dart';

class GetDeviceAlarmEventUseCase {
  const GetDeviceAlarmEventUseCase(this._repository);

  final DeviceAlarmEventsRepository _repository;

  Future<Result<AlarmEventEntity>> call({
    required String deviceId,
    required String alarmEventId,
  }) => _repository.getById(deviceId: deviceId, alarmEventId: alarmEventId);
}

import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../entities/alarm_event_entity.dart';
import '../repositories/device_alarm_events_repository.dart';

class ListDeviceAlarmEventsUseCase {
  const ListDeviceAlarmEventsUseCase(this._repository);

  final DeviceAlarmEventsRepository _repository;

  Future<Result<List<AlarmEventEntity>>> call(String deviceId) =>
      _repository.listByDevice(deviceId);
}

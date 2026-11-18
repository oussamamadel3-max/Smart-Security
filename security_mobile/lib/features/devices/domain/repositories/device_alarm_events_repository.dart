import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../entities/alarm_event_entity.dart';

abstract class DeviceAlarmEventsRepository {
  Future<Result<List<AlarmEventEntity>>> listByDevice(String deviceId);

  Future<Result<AlarmEventEntity>> getById({
    required String deviceId,
    required String alarmEventId,
  });

  void clearCache();

  void dropDevice(String deviceId);
}

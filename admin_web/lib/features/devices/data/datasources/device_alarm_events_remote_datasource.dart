import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/network/app_endpoints.dart';
import '../models/alarm_event_dto.dart';

class DeviceAlarmEventsRemoteDataSource extends ApiClient {
  DeviceAlarmEventsRemoteDataSource(super.dio);

  /// GET the ownership-period list. No page or search query is sent.
  Future<Result<List<AlarmEventDto>>> listByDevice(String deviceId) {
    return get(
      AppEndpoints.ownerDeviceAlarmEvents(deviceId),
      parser: (json) {
        if (json is! List) {
          throw const FormatException('Expected alarm-events list');
        }
        return json
            .whereType<Map>()
            .map((e) => AlarmEventDto.fromJson(Map<String, dynamic>.from(e)))
            .toList(growable: false);
      },
    );
  }

  Future<Result<AlarmEventDto>> getById({
    required String deviceId,
    required String alarmEventId,
  }) {
    return get(
      AppEndpoints.ownerDeviceAlarmEvent(deviceId, alarmEventId),
      parser: (json) => AlarmEventDto.fromJson(_asMap(json)),
    );
  }

  Map<String, dynamic> _asMap(dynamic json) {
    if (json is Map<String, dynamic>) return json;
    if (json is Map) return Map<String, dynamic>.from(json);
    throw const FormatException('Expected JSON object');
  }
}

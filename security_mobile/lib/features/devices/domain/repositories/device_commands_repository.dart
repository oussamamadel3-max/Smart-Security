import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../entities/device_command_entity.dart';

abstract class DeviceCommandsRepository {
  Future<Result<DeviceCommandEntity>> create({
    required String deviceId,
    required CommandType type,
    required String idempotencyKey,
    String? targetAlarmEventUid,
  });

  Future<Result<DeviceCommandEntity>> getById({
    required String deviceId,
    required String commandId,
  });

  Future<Result<List<DeviceCommandEntity>>> listByDevice(String deviceId);
}

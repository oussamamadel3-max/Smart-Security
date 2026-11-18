import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../entities/device_command_entity.dart';
import '../repositories/device_commands_repository.dart';

class CreateDeviceCommandUseCase {
  const CreateDeviceCommandUseCase(this._repository);

  final DeviceCommandsRepository _repository;

  Future<Result<DeviceCommandEntity>> call({
    required String deviceId,
    required CommandType type,
    required String idempotencyKey,
    String? targetAlarmEventUid,
  }) {
    return _repository.create(
      deviceId: deviceId,
      type: type,
      idempotencyKey: idempotencyKey,
      targetAlarmEventUid: targetAlarmEventUid,
    );
  }
}

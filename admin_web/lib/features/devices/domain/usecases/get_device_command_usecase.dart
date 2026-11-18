import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../entities/device_command_entity.dart';
import '../repositories/device_commands_repository.dart';

class GetDeviceCommandUseCase {
  const GetDeviceCommandUseCase(this._repository);

  final DeviceCommandsRepository _repository;

  Future<Result<DeviceCommandEntity>> call({
    required String deviceId,
    required String commandId,
  }) => _repository.getById(deviceId: deviceId, commandId: commandId);
}

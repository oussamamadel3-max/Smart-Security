import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../entities/device_command_entity.dart';
import '../repositories/device_commands_repository.dart';

class ListDeviceCommandsUseCase {
  const ListDeviceCommandsUseCase(this._repository);

  final DeviceCommandsRepository _repository;

  Future<Result<List<DeviceCommandEntity>>> call(String deviceId) =>
      _repository.listByDevice(deviceId);
}

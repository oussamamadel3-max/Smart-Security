import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../entities/owner_device_entity.dart';
import '../repositories/devices_repository.dart';

class GetMyDeviceUseCase {
  const GetMyDeviceUseCase(this._repository);

  final DevicesRepository _repository;

  Future<Result<OwnerDeviceEntity>> call(String deviceId) =>
      _repository.getMine(deviceId);
}

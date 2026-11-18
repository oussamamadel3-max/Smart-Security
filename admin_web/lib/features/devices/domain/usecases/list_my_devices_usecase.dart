import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../entities/owner_device_entity.dart';
import '../repositories/devices_repository.dart';

class ListMyDevicesUseCase {
  const ListMyDevicesUseCase(this._repository);

  final DevicesRepository _repository;

  Future<Result<List<OwnerDeviceEntity>>> call() => _repository.listMine();
}

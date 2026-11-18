import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../entities/admin_device_entity.dart';
import '../entities/admin_mutation_entities.dart';
import '../register_device_input.dart';
import '../repositories/admin_devices_repository.dart';

class ListAdminDevicesUseCase {
  const ListAdminDevicesUseCase(this._repository);

  final AdminDevicesRepository _repository;

  Future<Result<List<AdminDeviceEntity>>> call() => _repository.list();
}

class GetAdminDeviceUseCase {
  const GetAdminDeviceUseCase(this._repository);

  final AdminDevicesRepository _repository;

  Future<Result<AdminDeviceEntity>> call(String deviceId) =>
      _repository.getById(deviceId);
}

class RegisterAdminDeviceUseCase {
  const RegisterAdminDeviceUseCase(this._repository);

  final AdminDevicesRepository _repository;

  Future<Result<RegisteredDeviceEntity>> call(RegisterDeviceInput input) =>
      _repository.register(input);
}

class LinkAdminDeviceUseCase {
  const LinkAdminDeviceUseCase(this._repository);

  final AdminDevicesRepository _repository;

  Future<Result<LinkedDeviceEntity>> call({
    required String deviceId,
    required String ownerUserId,
  }) => _repository.link(deviceId: deviceId, ownerUserId: ownerUserId);
}

class TransferAdminDeviceUseCase {
  const TransferAdminDeviceUseCase(this._repository);

  final AdminDevicesRepository _repository;

  Future<Result<TransferredDeviceEntity>> call({
    required String deviceId,
    required String newOwnerUserId,
  }) =>
      _repository.transfer(deviceId: deviceId, newOwnerUserId: newOwnerUserId);
}

class UnlinkAdminDeviceUseCase {
  const UnlinkAdminDeviceUseCase(this._repository);

  final AdminDevicesRepository _repository;

  Future<Result<UnlinkedDeviceEntity>> call(String deviceId) =>
      _repository.unlink(deviceId);
}

class ArchiveAdminDeviceUseCase {
  const ArchiveAdminDeviceUseCase(this._repository);

  final AdminDevicesRepository _repository;

  Future<Result<ArchivedDeviceEntity>> call(String deviceId) =>
      _repository.archive(deviceId);
}

import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../entities/admin_device_entity.dart';
import '../entities/admin_mutation_entities.dart';
import '../register_device_input.dart';

abstract class AdminDevicesRepository {
  Future<Result<List<AdminDeviceEntity>>> list();

  Future<Result<AdminDeviceEntity>> getById(String deviceId);

  Future<Result<RegisteredDeviceEntity>> register(RegisterDeviceInput input);

  Future<Result<LinkedDeviceEntity>> link({
    required String deviceId,
    required String ownerUserId,
  });

  Future<Result<TransferredDeviceEntity>> transfer({
    required String deviceId,
    required String newOwnerUserId,
  });

  Future<Result<UnlinkedDeviceEntity>> unlink(String deviceId);

  Future<Result<ArchivedDeviceEntity>> archive(String deviceId);

  void clearCache();
}

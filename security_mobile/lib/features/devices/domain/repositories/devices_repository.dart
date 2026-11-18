import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../entities/owner_device_entity.dart';

abstract class DevicesRepository {
  Future<Result<List<OwnerDeviceEntity>>> listMine();

  Future<Result<OwnerDeviceEntity>> getMine(String deviceId);

  /// Drops any in-memory owner device cache (logout / 401 / ownership loss).
  void clearCache();
}

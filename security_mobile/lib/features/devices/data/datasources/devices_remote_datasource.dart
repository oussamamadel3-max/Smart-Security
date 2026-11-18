import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/network/app_endpoints.dart';
import '../models/owner_device_dto.dart';

class DevicesRemoteDataSource extends ApiClient {
  DevicesRemoteDataSource(super.dio);

  Future<Result<List<OwnerDeviceDto>>> listMine() {
    return get(
      AppEndpoints.ownerDevices,
      parser: (json) {
        if (json is! List) {
          throw const FormatException('Expected device list');
        }
        return json
            .whereType<Map>()
            .map((e) => OwnerDeviceDto.fromJson(Map<String, dynamic>.from(e)))
            .toList(growable: false);
      },
    );
  }

  Future<Result<OwnerDeviceDto>> getMine(String deviceId) {
    return get(
      AppEndpoints.ownerDevice(deviceId),
      parser: (json) {
        if (json is Map<String, dynamic>) {
          return OwnerDeviceDto.fromJson(json);
        }
        if (json is Map) {
          return OwnerDeviceDto.fromJson(Map<String, dynamic>.from(json));
        }
        throw const FormatException('Expected device object');
      },
    );
  }
}

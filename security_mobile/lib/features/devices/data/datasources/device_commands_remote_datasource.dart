import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/network/app_endpoints.dart';
import '../models/device_command_dto.dart';

class DeviceCommandsRemoteDataSource extends ApiClient {
  DeviceCommandsRemoteDataSource(super.dio);

  Future<Result<DeviceCommandDto>> create({
    required String deviceId,
    required CreateDeviceCommandRequestDto request,
  }) {
    return post(
      AppEndpoints.ownerDeviceCommands(deviceId),
      data: request.toJson(),
      parser: (json) => DeviceCommandDto.fromJson(_asMap(json)),
    );
  }

  Future<Result<DeviceCommandDto>> getById({
    required String deviceId,
    required String commandId,
  }) {
    return get(
      AppEndpoints.ownerDeviceCommand(deviceId, commandId),
      parser: (json) => DeviceCommandDto.fromJson(_asMap(json)),
    );
  }

  Future<Result<List<DeviceCommandDto>>> listByDevice(String deviceId) {
    return get(
      AppEndpoints.ownerDeviceCommands(deviceId),
      parser: (json) {
        if (json is! List) throw const FormatException('Expected command list');
        return json
            .whereType<Map>()
            .map((e) => DeviceCommandDto.fromJson(Map<String, dynamic>.from(e)))
            .toList(growable: false);
      },
    );
  }

  Map<String, dynamic> _asMap(dynamic json) {
    if (json is Map<String, dynamic>) return json;
    if (json is Map) return Map<String, dynamic>.from(json);
    throw const FormatException('Expected JSON object');
  }
}

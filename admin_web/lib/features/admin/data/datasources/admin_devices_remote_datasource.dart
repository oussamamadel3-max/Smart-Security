import 'dart:convert';

import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/network/app_endpoints.dart';
import '../../domain/register_device_input.dart';
import '../mappers/admin_json.dart';

class AdminDevicesRemoteDataSource extends ApiClient {
  AdminDevicesRemoteDataSource(super.dio);

  Future<Result<List<Map<String, dynamic>>>> list() {
    return get(AppEndpoints.adminDevices, parser: readMapList);
  }

  Future<Result<Map<String, dynamic>>> getById(String deviceId) {
    return get(AppEndpoints.adminDevice(deviceId), parser: readMap);
  }

  Future<Result<Map<String, dynamic>>> register(RegisterDeviceInput input) {
    return post(
      AppEndpoints.adminDevices,
      data: _registerBody(input),
      parser: readMap,
    );
  }

  Future<Result<Map<String, dynamic>>> link({
    required String deviceId,
    required String ownerUserId,
  }) {
    return post(
      AppEndpoints.adminDeviceLink(deviceId),
      data: {'ownerUserId': ownerUserId},
      parser: readMap,
    );
  }

  Future<Result<Map<String, dynamic>>> transfer({
    required String deviceId,
    required String newOwnerUserId,
  }) {
    return post(
      AppEndpoints.adminDeviceTransfer(deviceId),
      data: {'newOwnerUserId': newOwnerUserId},
      parser: readMap,
    );
  }

  Future<Result<Map<String, dynamic>>> unlink(String deviceId) {
    return post(AppEndpoints.adminDeviceUnlink(deviceId), parser: readMap);
  }

  Future<Result<Map<String, dynamic>>> archive(String deviceId) {
    return post(AppEndpoints.adminDeviceArchive(deviceId), parser: readMap);
  }
}

Map<String, dynamic> _registerBody(RegisterDeviceInput input) {
  final credential = <String, dynamic>{'authType': input.authType};
  switch (input.authType) {
    case DeviceCredentialTypes.passwordHash:
      credential['password'] = input.credentialValue;
    case DeviceCredentialTypes.certificate:
      credential['certificateFingerprint'] = input.credentialValue.trim();
    case DeviceCredentialTypes.secretRef:
      credential['secretRef'] = input.credentialValue.trim();
  }
  return {
    'hardwareUid': input.hardwareUid.trim(),
    'displayName': input.displayName.trim(),
    'credential': credential,
    'settings': {
      'alarmRule': jsonDecode(input.alarmRuleJson),
      'eventBufferCapacity': input.eventBufferCapacity,
      'commandTtlSeconds': input.commandTtlSeconds,
    },
  };
}

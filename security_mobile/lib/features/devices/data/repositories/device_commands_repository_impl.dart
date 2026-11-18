import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../domain/entities/device_command_entity.dart';
import '../../domain/repositories/device_commands_repository.dart';
import '../datasources/device_commands_remote_datasource.dart';
import '../mappers/device_command_mapper.dart';
import '../models/device_command_dto.dart';

class DeviceCommandsRepositoryImpl implements DeviceCommandsRepository {
  DeviceCommandsRepositoryImpl(this._remote);

  final DeviceCommandsRemoteDataSource _remote;

  @override
  Future<Result<DeviceCommandEntity>> create({
    required String deviceId,
    required CommandType type,
    required String idempotencyKey,
    String? targetAlarmEventUid,
  }) async {
    // POST must not use executeWithRetry — duplicate ARM/DISARM is unsafe.
    final result = await _remote.create(
      deviceId: deviceId,
      request: CreateDeviceCommandRequestDto(
        type: type.code,
        idempotencyKey: idempotencyKey,
        targetAlarmEventUid: targetAlarmEventUid,
      ),
    );
    return switch (result) {
      Success(:final data) => Success(DeviceCommandMapper.toEntity(data)),
      Failure_(:final failure) => Failure_(failure),
    };
  }

  @override
  Future<Result<DeviceCommandEntity>> getById({
    required String deviceId,
    required String commandId,
  }) async {
    final result = await executeWithRetry(
      () => _remote.getById(deviceId: deviceId, commandId: commandId),
    );
    return switch (result) {
      Success(:final data) => Success(DeviceCommandMapper.toEntity(data)),
      Failure_(:final failure) => Failure_(failure),
    };
  }

  @override
  Future<Result<List<DeviceCommandEntity>>> listByDevice(
    String deviceId,
  ) async {
    final result = await executeWithRetry(() => _remote.listByDevice(deviceId));
    return switch (result) {
      Success(:final data) => Success(
        data.map(DeviceCommandMapper.toEntity).toList(growable: false),
      ),
      Failure_(:final failure) => Failure_(failure),
    };
  }
}

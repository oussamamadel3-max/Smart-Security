import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/session/session_epoch.dart';
import '../../domain/entities/device_command_entity.dart';
import '../../domain/repositories/device_commands_repository.dart';
import '../datasources/device_commands_remote_datasource.dart';
import '../mappers/device_command_mapper.dart';
import '../models/device_command_dto.dart';

class DeviceCommandsRepositoryImpl implements DeviceCommandsRepository {
  DeviceCommandsRepositoryImpl(this._remote, {SessionEpoch? epoch})
    : _epoch = epoch ?? SessionEpoch();

  final DeviceCommandsRemoteDataSource _remote;
  final SessionEpoch _epoch;
  final Map<String, List<DeviceCommandEntity>> _listCache = {};
  final Map<String, DeviceCommandEntity> _detailCache = {};

  String _detailKey(String deviceId, String commandId) =>
      '$deviceId::$commandId';

  bool get hasCachedHistory => _listCache.isNotEmpty || _detailCache.isNotEmpty;

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
      Failure_(:final failure) => () {
        if (_accessDropped(failure)) dropDevice(deviceId);
        return Failure_<DeviceCommandEntity>(failure);
      }(),
    };
  }

  @override
  Future<Result<DeviceCommandEntity>> getById({
    required String deviceId,
    required String commandId,
  }) async {
    final captured = _epoch.capture();
    final result = await executeWithRetry(
      () => _remote.getById(deviceId: deviceId, commandId: commandId),
    );
    return switch (result) {
      Success(:final data) => () {
        final entity = DeviceCommandMapper.toEntity(data);
        if (_epoch.isCurrent(captured)) {
          _detailCache[_detailKey(deviceId, entity.commandId)] = entity;
        }
        return Success(entity);
      }(),
      Failure_(:final failure) => () {
        if (_epoch.isCurrent(captured) && _accessDropped(failure)) {
          dropDevice(deviceId);
        }
        return Failure_<DeviceCommandEntity>(failure);
      }(),
    };
  }

  @override
  Future<Result<List<DeviceCommandEntity>>> listByDevice(
    String deviceId,
  ) async {
    final captured = _epoch.capture();
    final result = await executeWithRetry(() => _remote.listByDevice(deviceId));
    return switch (result) {
      Success(:final data) => () {
        final entities = data
            .map(DeviceCommandMapper.toEntity)
            .toList(growable: false);
        if (_epoch.isCurrent(captured)) {
          _listCache[deviceId] = entities;
          for (final item in entities) {
            _detailCache[_detailKey(deviceId, item.commandId)] = item;
          }
        }
        return Success(entities);
      }(),
      Failure_(:final failure) => () {
        if (_epoch.isCurrent(captured) && _accessDropped(failure)) {
          dropDevice(deviceId);
        }
        return Failure_<List<DeviceCommandEntity>>(failure);
      }(),
    };
  }

  @override
  void clearCache() {
    _listCache.clear();
    _detailCache.clear();
  }

  @override
  void dropDevice(String deviceId) {
    _listCache.remove(deviceId);
    _detailCache.removeWhere((key, _) => key.startsWith('$deviceId::'));
  }

  bool _accessDropped(Failure failure) =>
      failure.requiresReauthentication ||
      failure is ForbiddenFailure ||
      failure is NotFoundFailure;
}

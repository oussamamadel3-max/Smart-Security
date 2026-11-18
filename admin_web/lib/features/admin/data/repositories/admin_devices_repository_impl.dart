import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/session/session_epoch.dart';
import '../../domain/entities/admin_device_entity.dart';
import '../../domain/entities/admin_mutation_entities.dart';
import '../../domain/register_device_input.dart';
import '../../domain/repositories/admin_devices_repository.dart';
import '../datasources/admin_devices_remote_datasource.dart';
import '../mappers/admin_mapper.dart';

class AdminDevicesRepositoryImpl implements AdminDevicesRepository {
  AdminDevicesRepositoryImpl(this._remote, {SessionEpoch? epoch})
    : _epoch = epoch ?? SessionEpoch();

  final AdminDevicesRemoteDataSource _remote;
  final SessionEpoch _epoch;
  List<AdminDeviceEntity>? _listCache;

  bool _current(int captured) => _epoch.isCurrent(captured);

  @override
  Future<Result<List<AdminDeviceEntity>>> list() async {
    final captured = _epoch.capture();
    final result = await executeWithRetry(_remote.list);
    return switch (result) {
      Failure_(:final failure) => Failure_(failure),
      Success(:final data) => () {
        final entities = data.map(AdminDeviceMapper.toEntity).toList();
        if (_current(captured)) _listCache = entities;
        return Success(entities);
      }(),
    };
  }

  @override
  Future<Result<AdminDeviceEntity>> getById(String deviceId) async {
    final result = await executeWithRetry(() => _remote.getById(deviceId));
    return switch (result) {
      Failure_(:final failure) => Failure_(failure),
      Success(:final data) => Success(AdminDeviceMapper.toEntity(data)),
    };
  }

  @override
  Future<Result<RegisteredDeviceEntity>> register(
    RegisterDeviceInput input,
  ) async {
    final captured = _epoch.capture();
    final result = await _remote.register(input);
    if (result is Success && _current(captured)) _listCache = null;
    return switch (result) {
      Failure_(:final failure) => Failure_(failure),
      Success(:final data) => Success(AdminDeviceMapper.registered(data)),
    };
  }

  @override
  Future<Result<LinkedDeviceEntity>> link({
    required String deviceId,
    required String ownerUserId,
  }) async {
    final captured = _epoch.capture();
    final result = await _remote.link(
      deviceId: deviceId,
      ownerUserId: ownerUserId,
    );
    if (result is Success && _current(captured)) _listCache = null;
    return switch (result) {
      Failure_(:final failure) => Failure_(failure),
      Success(:final data) => Success(AdminDeviceMapper.linked(data)),
    };
  }

  @override
  Future<Result<TransferredDeviceEntity>> transfer({
    required String deviceId,
    required String newOwnerUserId,
  }) async {
    final captured = _epoch.capture();
    final result = await _remote.transfer(
      deviceId: deviceId,
      newOwnerUserId: newOwnerUserId,
    );
    if (result is Success && _current(captured)) _listCache = null;
    return switch (result) {
      Failure_(:final failure) => Failure_(failure),
      Success(:final data) => Success(AdminDeviceMapper.transferred(data)),
    };
  }

  @override
  Future<Result<UnlinkedDeviceEntity>> unlink(String deviceId) async {
    final captured = _epoch.capture();
    final result = await _remote.unlink(deviceId);
    if (result is Success && _current(captured)) _listCache = null;
    return switch (result) {
      Failure_(:final failure) => Failure_(failure),
      Success(:final data) => Success(AdminDeviceMapper.unlinked(data)),
    };
  }

  @override
  Future<Result<ArchivedDeviceEntity>> archive(String deviceId) async {
    final captured = _epoch.capture();
    final result = await _remote.archive(deviceId);
    if (result is Success && _current(captured)) _listCache = null;
    return switch (result) {
      Failure_(:final failure) => Failure_(failure),
      Success(:final data) => Success(AdminDeviceMapper.archived(data)),
    };
  }

  @override
  void clearCache() => _listCache = null;

  bool get hasCachedList => _listCache != null;
}

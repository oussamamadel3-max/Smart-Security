import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/session/session_epoch.dart';
import '../../domain/entities/owner_device_entity.dart';
import '../../domain/repositories/devices_repository.dart';
import '../datasources/devices_remote_datasource.dart';
import '../mappers/owner_device_mapper.dart';

class DevicesRepositoryImpl implements DevicesRepository {
  DevicesRepositoryImpl(this._remote, {SessionEpoch? epoch})
    : _epoch = epoch ?? SessionEpoch();

  final DevicesRemoteDataSource _remote;
  final SessionEpoch _epoch;

  List<OwnerDeviceEntity>? _listCache;
  final Map<String, OwnerDeviceEntity> _detailCache = {};

  @override
  Future<Result<List<OwnerDeviceEntity>>> listMine() async {
    final captured = _epoch.capture();
    final result = await executeWithRetry(_remote.listMine);
    return switch (result) {
      Failure_(:final failure) => Failure_(failure),
      Success(:final data) => () {
        final entities = data
            .map(OwnerDeviceMapper.toEntity)
            .toList(growable: false);
        if (_epoch.isCurrent(captured)) {
          _listCache = entities;
          for (final item in entities) {
            _detailCache[item.deviceId] = item;
          }
        }
        return Success(entities);
      }(),
    };
  }

  @override
  Future<Result<OwnerDeviceEntity>> getMine(String deviceId) async {
    final captured = _epoch.capture();
    final result = await executeWithRetry(() => _remote.getMine(deviceId));
    return switch (result) {
      Failure_(:final failure) => () {
        if (_epoch.isCurrent(captured) &&
            (failure.requiresReauthentication ||
                failure is ForbiddenFailure ||
                failure is NotFoundFailure)) {
          _detailCache.remove(deviceId);
          _listCache = _listCache
              ?.where((e) => e.deviceId != deviceId)
              .toList();
        }
        return Failure_<OwnerDeviceEntity>(failure);
      }(),
      Success(:final data) => () {
        final entity = OwnerDeviceMapper.toEntity(data);
        if (_epoch.isCurrent(captured)) {
          _detailCache[entity.deviceId] = entity;
        }
        return Success(entity);
      }(),
    };
  }

  @override
  void clearCache() {
    _listCache = null;
    _detailCache.clear();
  }

  /// Test helper — exposes whether list cache still holds rows.
  bool get hasCachedList => _listCache != null && _listCache!.isNotEmpty;
}

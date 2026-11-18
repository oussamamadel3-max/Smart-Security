import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../domain/entities/alarm_event_entity.dart';
import '../../domain/repositories/device_alarm_events_repository.dart';
import '../datasources/device_alarm_events_remote_datasource.dart';
import '../mappers/alarm_event_mapper.dart';

class DeviceAlarmEventsRepositoryImpl implements DeviceAlarmEventsRepository {
  DeviceAlarmEventsRepositoryImpl(this._remote);

  final DeviceAlarmEventsRemoteDataSource _remote;

  final Map<String, List<AlarmEventEntity>> _listCache = {};
  final Map<String, AlarmEventEntity> _detailCache = {};

  String _detailKey(String deviceId, String alarmEventId) =>
      '$deviceId::$alarmEventId';

  @override
  Future<Result<List<AlarmEventEntity>>> listByDevice(String deviceId) async {
    final result =
        await executeWithRetry(() => _remote.listByDevice(deviceId));
    return switch (result) {
      Failure_(:final failure) => () {
          if (_shouldDrop(failure)) {
            dropDevice(deviceId);
          }
          return Failure_<List<AlarmEventEntity>>(failure);
        }(),
      Success(:final data) => () {
          final entities =
              data.map(AlarmEventMapper.toEntity).toList(growable: false);
          _listCache[deviceId] = entities;
          for (final item in entities) {
            _detailCache[_detailKey(deviceId, item.alarmEventId)] = item;
          }
          return Success(entities);
        }(),
    };
  }

  @override
  Future<Result<AlarmEventEntity>> getById({
    required String deviceId,
    required String alarmEventId,
  }) async {
    final result = await executeWithRetry(
      () => _remote.getById(deviceId: deviceId, alarmEventId: alarmEventId),
    );
    return switch (result) {
      Failure_(:final failure) => () {
          if (_shouldDrop(failure)) {
            _detailCache.remove(_detailKey(deviceId, alarmEventId));
            _listCache.remove(deviceId);
          }
          return Failure_<AlarmEventEntity>(failure);
        }(),
      Success(:final data) => () {
          final entity = AlarmEventMapper.toEntity(data);
          _detailCache[_detailKey(deviceId, entity.alarmEventId)] = entity;
          return Success(entity);
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

  bool _shouldDrop(Failure failure) =>
      failure.requiresReauthentication ||
      failure is ForbiddenFailure ||
      failure is NotFoundFailure;
}

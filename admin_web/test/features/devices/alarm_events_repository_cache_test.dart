import 'package:admin_web/features/devices/data/datasources/device_alarm_events_remote_datasource.dart';
import 'package:admin_web/features/devices/data/models/alarm_event_dto.dart';
import 'package:admin_web/features/devices/data/repositories/device_alarm_events_repository_impl.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

class _Remote extends DeviceAlarmEventsRemoteDataSource {
  _Remote() : super(Dio());

  Result<List<AlarmEventDto>> listResult = const Success([]);
  Result<AlarmEventDto> detailResult = const Failure_(Failure.notFound());

  @override
  Future<Result<List<AlarmEventDto>>> listByDevice(String deviceId) async {
    return listResult;
  }

  @override
  Future<Result<AlarmEventDto>> getById({
    required String deviceId,
    required String alarmEventId,
  }) async {
    return detailResult;
  }
}

AlarmEventDto _dto({String id = 'owned-event'}) {
  return AlarmEventDto(
    alarmEventId: id,
    deviceId: 'device-1',
    eventUid: 'uid-$id',
    ownershipId: 'own-1',
    ownershipVersion: 2,
    status: 'ACTIVE',
    receivedAt: '2026-11-17T10:00:00Z',
    clockTrusted: true,
    wasBuffered: false,
    sensorKeySnapshot: 'vibration-1',
  );
}

void main() {
  test(
    '404 drops cached history and does not return the previous row',
    () async {
      final remote = _Remote()..listResult = Success([_dto()]);
      final repo = DeviceAlarmEventsRepositoryImpl(remote);

      final listed = await repo.listByDevice('device-1');
      expect(listed.isSuccess, isTrue);
      expect(repo.hasCachedHistory, isTrue);

      remote.detailResult = const Failure_(
        Failure.notFound(message: 'الحادثة غير موجودة.'),
      );
      final detail = await repo.getById(
        deviceId: 'device-1',
        alarmEventId: 'foreign-event',
      );

      expect(detail.isFailure, isTrue);
      expect(detail.failureOrNull, isA<NotFoundFailure>());
      expect(repo.hasCachedHistory, isFalse);
    },
  );

  test('mismatched event id is not stored as the requested row', () async {
    final remote = _Remote()..detailResult = Success(_dto(id: 'other-event'));
    final repo = DeviceAlarmEventsRepositoryImpl(remote);

    final detail = await repo.getById(
      deviceId: 'device-1',
      alarmEventId: 'foreign-event',
    );

    expect(detail.isFailure, isTrue);
    expect(repo.hasCachedHistory, isFalse);
  });

  test('clearCache drops retained rows', () async {
    final remote = _Remote()..listResult = Success([_dto()]);
    final repo = DeviceAlarmEventsRepositoryImpl(remote);
    await repo.listByDevice('device-1');
    repo.clearCache();
    expect(repo.hasCachedHistory, isFalse);
  });
}

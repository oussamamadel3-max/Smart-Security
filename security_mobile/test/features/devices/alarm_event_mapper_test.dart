import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/core/domain/device_status_codes.dart';
import 'package:security_mobile/features/devices/data/mappers/alarm_event_mapper.dart';
import 'package:security_mobile/features/devices/data/models/alarm_event_dto.dart';

void main() {
  test('clockTrusted=false drops occurrence time even if wire has a value', () {
    final entity = AlarmEventMapper.toEntity(
      AlarmEventDto(
        alarmEventId: 'a1',
        deviceId: 'd1',
        eventUid: 'e1',
        ownershipId: 'o1',
        ownershipVersion: 1,
        status: 'ACTIVE',
        receivedAt: '2026-11-17T10:00:00Z',
        occurredDeviceAt: '2026-11-17T09:59:00Z',
        clockTrusted: false,
        wasBuffered: true,
        sensorKeySnapshot: 'vibration-1',
      ),
    );

    expect(entity.clockTrusted, isFalse);
    expect(entity.occurredDeviceAt, isNull);
    expect(entity.hasTrustedOccurrence, isFalse);
    expect(entity.showBufferedNotice, isTrue);
    expect(entity.status, AlarmEventStatus.active);
  });

  test('clockTrusted=true keeps parsed occurrence', () {
    final entity = AlarmEventMapper.toEntity(
      const AlarmEventDto(
        alarmEventId: 'a2',
        deviceId: 'd1',
        eventUid: 'e2',
        ownershipId: 'o1',
        ownershipVersion: 1,
        status: 'ENDED',
        receivedAt: '2026-11-17T10:00:00Z',
        occurredDeviceAt: '2026-11-17T09:59:00Z',
        clockTrusted: true,
        wasBuffered: false,
        sensorKeySnapshot: 'vibration-1',
      ),
    );

    expect(entity.hasTrustedOccurrence, isTrue);
    expect(entity.occurredDeviceAt, isNotNull);
    expect(entity.showBufferedNotice, isFalse);
  });
}

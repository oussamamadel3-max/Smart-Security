import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/features/devices/domain/entities/alarm_event_entity.dart';
import 'package:security_mobile/features/devices/domain/entities/device_command_entity.dart';
import 'package:security_mobile/features/devices/presentation/viewmodels/device_history_view_models.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

void main() {
  final events = [
    AlarmEventEntity(
      alarmEventId: 'a1',
      deviceId: 'd1',
      eventUid: 'e1',
      ownershipId: 'o1',
      ownershipVersion: 1,
      status: AlarmEventStatus.active,
      receivedAt: DateTime.now(),
      clockTrusted: true,
      wasBuffered: true,
      sensorKeySnapshot: 'vibration-1',
    ),
    AlarmEventEntity(
      alarmEventId: 'a2',
      deviceId: 'd1',
      eventUid: 'e2',
      ownershipId: 'o1',
      ownershipVersion: 1,
      status: AlarmEventStatus.ended,
      receivedAt: DateTime.now(),
      clockTrusted: false,
      wasBuffered: false,
      sensorKeySnapshot: 'vibration-1',
    ),
  ];

  final commands = [
    DeviceCommandEntity(
      commandId: 'c-pending',
      deviceId: 'd1',
      type: CommandType.arm,
      status: CommandStatus.pending,
      commandSequence: 1,
      ownershipVersion: 1,
      observedStateSequence: 0,
      createdAt: DateTime.now(),
      expiresAt: DateTime.now(),
      actualStateChanged: false,
      idempotentReplay: false,
    ),
    DeviceCommandEntity(
      commandId: 'c-timeout',
      deviceId: 'd1',
      type: CommandType.disarm,
      status: CommandStatus.timedOut,
      commandSequence: 2,
      ownershipVersion: 1,
      observedStateSequence: 1,
      createdAt: DateTime.now(),
      expiresAt: DateTime.now(),
      actualStateChanged: false,
      idempotentReplay: false,
    ),
  ];

  test('bufferedOnly keeps wasBuffered events', () {
    final filtered = filterAlarmEvents(
      events,
      query: '',
      bufferedOnly: true,
    );
    expect(filtered.map((e) => e.alarmEventId), ['a1']);
  });

  test('query matches event id', () {
    final filtered = filterAlarmEvents(
      events,
      query: 'a2',
      bufferedOnly: false,
    );
    expect(filtered.single.alarmEventId, 'a2');
  });

  test('command status filter is local only', () {
    final filtered = filterCommands(
      commands,
      query: '',
      statusFilter: 'TIMED_OUT',
    );
    expect(filtered.single.commandId, 'c-timeout');
  });
}

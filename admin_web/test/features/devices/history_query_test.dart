import 'package:admin_web/features/devices/domain/entities/alarm_event_entity.dart';
import 'package:admin_web/features/devices/domain/entities/device_command_entity.dart';
import 'package:admin_web/features/devices/domain/history_query.dart';
import 'package:admin_web/features/devices/domain/history_session_policy.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

AlarmEventEntity _event({
  required String id,
  bool wasBuffered = false,
  bool clockTrusted = true,
}) {
  return AlarmEventEntity(
    alarmEventId: id,
    deviceId: 'd1',
    eventUid: 'uid-$id',
    ownershipId: 'o1',
    ownershipVersion: 1,
    status: AlarmEventStatus.active,
    receivedAt: DateTime.utc(2026, 11, 17, 10),
    clockTrusted: clockTrusted,
    wasBuffered: wasBuffered,
    sensorKeySnapshot: 'vibration-1',
  );
}

DeviceCommandEntity _command({
  required String id,
  required CommandType type,
  required CommandStatus status,
}) {
  return DeviceCommandEntity(
    commandId: id,
    deviceId: 'd1',
    type: type,
    status: status,
    commandSequence: 1,
    ownershipVersion: 1,
    observedStateSequence: 0,
    createdAt: DateTime.utc(2026, 11, 17, 10),
    expiresAt: DateTime.utc(2026, 11, 17, 10, 1),
    actualStateChanged: false,
    idempotentReplay: false,
  );
}

void main() {
  test('bufferedOnly keeps wasBuffered events', () {
    final filtered = filterAlarmEvents(
      [_event(id: 'a1', wasBuffered: true), _event(id: 'a2')],
      query: '',
      bufferedOnly: true,
    );
    expect(filtered.map((e) => e.alarmEventId), ['a1']);
  });

  test('query matches event id', () {
    final filtered = filterAlarmEvents(
      [_event(id: 'a1'), _event(id: 'a2')],
      query: 'a2',
      bufferedOnly: false,
    );
    expect(filtered.single.alarmEventId, 'a2');
  });

  test('page window slices the filtered list without a server query', () {
    final source = [for (var i = 0; i < 10; i++) _event(id: 'e$i')];
    final page = sliceHistoryPage(source, pageIndex: 1);
    expect(page.total, 10);
    expect(page.pageCount, 2);
    expect(page.items.map((e) => e.alarmEventId), ['e8', 'e9']);
  });

  test('command status filter is local and marks siren tests', () {
    final commands = [
      _command(id: 'c1', type: CommandType.arm, status: CommandStatus.pending),
      _command(
        id: 'c2',
        type: CommandType.testSiren,
        status: CommandStatus.timedOut,
      ),
    ];
    final filtered = filterCommands(
      commands,
      query: '',
      statusFilter: 'TIMED_OUT',
    );
    expect(filtered.single.commandId, 'c2');
    expect(isSirenTestCommand(filtered.single), isTrue);
    expect(isSirenTestCommand(commands.first), isFalse);
  });

  test('auth loss and user switch clear history cache', () {
    expect(
      historyCacheMustClear(
        wasAuthenticated: true,
        isAuthenticated: false,
        previousUserId: 'a',
        nextUserId: null,
      ),
      isTrue,
    );
    expect(
      historyCacheMustClear(
        wasAuthenticated: true,
        isAuthenticated: true,
        previousUserId: 'a',
        nextUserId: 'b',
      ),
      isTrue,
    );
    expect(
      historyCacheMustClear(
        wasAuthenticated: true,
        isAuthenticated: true,
        previousUserId: 'a',
        nextUserId: 'a',
      ),
      isFalse,
    );
  });

  test('403 and 404 drop history access', () {
    expect(historyAccessDropped(const Failure.forbidden()), isTrue);
    expect(historyAccessDropped(const Failure.notFound()), isTrue);
    expect(historyAccessDropped(const Failure.network()), isFalse);
  });
}

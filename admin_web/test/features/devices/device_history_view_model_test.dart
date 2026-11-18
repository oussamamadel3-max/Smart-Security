import 'dart:async';

import 'package:admin_web/core/session/auth_session_controller.dart';
import 'package:admin_web/core/session/auth_session_state.dart';
import 'package:admin_web/features/auth/domain/entities/auth_user_entity.dart';
import 'package:admin_web/features/devices/domain/entities/alarm_event_entity.dart';
import 'package:admin_web/features/devices/domain/entities/device_command_entity.dart';
import 'package:admin_web/features/devices/domain/repositories/device_alarm_events_repository.dart';
import 'package:admin_web/features/devices/domain/repositories/device_commands_repository.dart';
import 'package:admin_web/features/devices/presentation/providers/devices_providers.dart';
import 'package:admin_web/features/devices/presentation/viewmodels/device_history_view_models.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

class _OwnerSession extends AuthSessionController {
  @override
  AuthSessionState build() {
    return const AuthSessionState(
      status: AuthStatus.authenticated,
      user: AuthUserEntity(id: 'owner-1', roles: ['OWNER']),
    );
  }
}

class _SwitchableSession extends AuthSessionController {
  @override
  AuthSessionState build() {
    return const AuthSessionState(
      status: AuthStatus.authenticated,
      user: AuthUserEntity(id: 'owner-1', roles: ['OWNER']),
    );
  }

  void loseAuth() {
    state = const AuthSessionState(status: AuthStatus.unauthenticated);
  }

  void switchUser() {
    state = const AuthSessionState(
      status: AuthStatus.authenticated,
      user: AuthUserEntity(id: 'owner-2', roles: ['OWNER']),
    );
  }
}

class _EventsRepo implements DeviceAlarmEventsRepository {
  _EventsRepo(this.listResults);

  final List<Result<List<AlarmEventEntity>>> listResults;
  Result<AlarmEventEntity> detailResult = const Failure_(
    Failure.notFound(message: 'الحادثة غير موجودة.'),
  );
  int clears = 0;
  int drops = 0;
  int lists = 0;

  @override
  Future<Result<List<AlarmEventEntity>>> listByDevice(String deviceId) async {
    final index = lists < listResults.length ? lists : listResults.length - 1;
    lists += 1;
    return listResults[index];
  }

  @override
  Future<Result<AlarmEventEntity>> getById({
    required String deviceId,
    required String alarmEventId,
  }) async {
    return detailResult;
  }

  @override
  void clearCache() => clears += 1;

  @override
  void dropDevice(String deviceId) => drops += 1;
}

class _CommandsRepo implements DeviceCommandsRepository {
  Result<List<DeviceCommandEntity>> listResult = const Success([]);
  Result<DeviceCommandEntity> detailResult = const Failure_(
    Failure.notFound(message: 'الأمر غير موجود.'),
  );
  int clears = 0;

  @override
  Future<Result<DeviceCommandEntity>> create({
    required String deviceId,
    required CommandType type,
    required String idempotencyKey,
    String? targetAlarmEventUid,
  }) async {
    return const Failure_(Failure.network());
  }

  @override
  Future<Result<DeviceCommandEntity>> getById({
    required String deviceId,
    required String commandId,
  }) async {
    return detailResult;
  }

  @override
  Future<Result<List<DeviceCommandEntity>>> listByDevice(
    String deviceId,
  ) async {
    return listResult;
  }

  @override
  void clearCache() => clears += 1;

  @override
  void dropDevice(String deviceId) {}
}

AlarmEventEntity _event(String id) {
  return AlarmEventEntity(
    alarmEventId: id,
    deviceId: 'device-1',
    eventUid: 'uid-$id',
    ownershipId: 'own-1',
    ownershipVersion: 1,
    status: AlarmEventStatus.active,
    receivedAt: DateTime.utc(2026, 11, 17),
    clockTrusted: false,
    wasBuffered: true,
    sensorKeySnapshot: 'vibration-1',
  );
}

void main() {
  test('stale list response does not replace a newer load', () async {
    final first = Completer<Result<List<AlarmEventEntity>>>();
    final second = Completer<Result<List<AlarmEventEntity>>>();
    final events = _GateEvents([first, second]);
    final commands = _CommandsRepo();
    final container = ProviderContainer(
      overrides: [
        authSessionProvider.overrideWith(_OwnerSession.new),
        deviceAlarmEventsRepositoryProvider.overrideWithValue(events),
        deviceCommandsRepositoryProvider.overrideWithValue(commands),
      ],
    );
    addTearDown(container.dispose);

    container.read(deviceHistoryListViewModelProvider('device-1'));
    await Future<void>.delayed(Duration.zero);
    expect(events.started, 1);

    final vm = container.read(
      deviceHistoryListViewModelProvider('device-1').notifier,
    );
    final secondLoad = vm.load();
    await Future<void>.delayed(Duration.zero);
    expect(events.started, 2);

    first.complete(Success([_event('stale')]));
    await Future<void>.delayed(Duration.zero);
    expect(
      container.read(deviceHistoryListViewModelProvider('device-1')).events,
      isEmpty,
    );

    second.complete(Success([_event('fresh')]));
    await secondLoad;
    final state = container.read(
      deviceHistoryListViewModelProvider('device-1'),
    );
    expect(state.events.single.alarmEventId, 'fresh');
    expect(state.eventsWindow.items.single.alarmEventId, 'fresh');
  });

  test('foreign event id shows an error and no row', () async {
    final events = _EventsRepo([const Success([])]);
    final container = ProviderContainer(
      overrides: [
        authSessionProvider.overrideWith(_OwnerSession.new),
        deviceAlarmEventsRepositoryProvider.overrideWithValue(events),
        deviceCommandsRepositoryProvider.overrideWithValue(_CommandsRepo()),
      ],
    );
    addTearDown(container.dispose);

    final key = const AlarmEventDetailArgs(
      deviceId: 'device-1',
      alarmEventId: 'foreign-event',
    );
    container.read(alarmEventDetailViewModelProvider(key));
    await Future<void>.delayed(Duration.zero);
    await Future<void>.delayed(Duration.zero);

    final state = container.read(alarmEventDetailViewModelProvider(key));
    expect(state.event, isNull);
    expect(state.error?.message, 'الحادثة غير موجودة.');
    expect(state.event?.eventUid, isNot(contains('secret')));
  });

  test('logout clears displayed history', () async {
    late _SwitchableSession session;
    final events = _EventsRepo([
      Success([_event('owned')]),
      const Success([]),
    ]);
    final commands = _CommandsRepo();
    final container = ProviderContainer(
      overrides: [
        authSessionProvider.overrideWith(() {
          session = _SwitchableSession();
          return session;
        }),
        deviceAlarmEventsRepositoryProvider.overrideWithValue(events),
        deviceCommandsRepositoryProvider.overrideWithValue(commands),
      ],
    );
    addTearDown(container.dispose);

    container.read(deviceHistoryListViewModelProvider('device-1'));
    await Future<void>.delayed(Duration.zero);
    await Future<void>.delayed(Duration.zero);
    expect(
      container
          .read(deviceHistoryListViewModelProvider('device-1'))
          .events
          .single
          .alarmEventId,
      'owned',
    );

    session.loseAuth();
    await Future<void>.delayed(Duration.zero);

    final state = container.read(
      deviceHistoryListViewModelProvider('device-1'),
    );
    expect(state.events, isEmpty);
    expect(state.commands, isEmpty);
    expect(events.clears, greaterThan(0));
    expect(commands.clears, greaterThan(0));
  });

  test('user switch clears the previous owner history', () async {
    late _SwitchableSession session;
    final events = _EventsRepo([
      Success([_event('owned')]),
      const Success([]),
    ]);
    final commands = _CommandsRepo();
    final container = ProviderContainer(
      overrides: [
        authSessionProvider.overrideWith(() {
          session = _SwitchableSession();
          return session;
        }),
        deviceAlarmEventsRepositoryProvider.overrideWithValue(events),
        deviceCommandsRepositoryProvider.overrideWithValue(commands),
      ],
    );
    addTearDown(container.dispose);

    container.read(deviceHistoryListViewModelProvider('device-1'));
    await Future<void>.delayed(Duration.zero);
    await Future<void>.delayed(Duration.zero);

    final clearsBefore = events.clears;
    session.switchUser();
    await Future<void>.delayed(Duration.zero);
    await Future<void>.delayed(Duration.zero);

    expect(events.clears, greaterThan(clearsBefore));
    expect(commands.clears, greaterThan(0));
    expect(
      container
          .read(deviceHistoryListViewModelProvider('device-1'))
          .events
          .any((event) => event.alarmEventId == 'owned'),
      isFalse,
    );
  });

  test('403 on the list drops both histories from the screen', () async {
    final events = _EventsRepo([
      const Failure_(Failure.forbidden(message: 'لا تملك هذا الجهاز.')),
    ]);
    final commands = _CommandsRepo()
      ..listResult = Success([
        DeviceCommandEntity(
          commandId: 'secret-command',
          deviceId: 'device-1',
          type: CommandType.arm,
          status: CommandStatus.pending,
          commandSequence: 1,
          ownershipVersion: 1,
          observedStateSequence: 0,
          createdAt: DateTime.utc(2026),
          expiresAt: DateTime.utc(2026),
          actualStateChanged: false,
          idempotentReplay: false,
        ),
      ]);
    final container = ProviderContainer(
      overrides: [
        authSessionProvider.overrideWith(_OwnerSession.new),
        deviceAlarmEventsRepositoryProvider.overrideWithValue(events),
        deviceCommandsRepositoryProvider.overrideWithValue(commands),
      ],
    );
    addTearDown(container.dispose);

    container.read(deviceHistoryListViewModelProvider('device-1'));
    await Future<void>.delayed(Duration.zero);
    await Future<void>.delayed(Duration.zero);

    final state = container.read(
      deviceHistoryListViewModelProvider('device-1'),
    );
    expect(state.events, isEmpty);
    expect(state.commands, isEmpty);
    expect(state.error?.message, 'لا تملك هذا الجهاز.');
    expect(events.drops, greaterThan(0));
  });
}

class _GateEvents implements DeviceAlarmEventsRepository {
  _GateEvents(this._gates);

  final List<Completer<Result<List<AlarmEventEntity>>>> _gates;
  int started = 0;

  @override
  Future<Result<List<AlarmEventEntity>>> listByDevice(String deviceId) {
    final gate = _gates[started];
    started += 1;
    return gate.future;
  }

  @override
  Future<Result<AlarmEventEntity>> getById({
    required String deviceId,
    required String alarmEventId,
  }) async {
    return const Failure_(Failure.notFound());
  }

  @override
  void clearCache() {}

  @override
  void dropDevice(String deviceId) {}
}

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/session/auth_session_controller.dart';
import '../../../../core/session/auth_session_state.dart';
import '../../domain/entities/alarm_event_entity.dart';
import '../../domain/entities/device_command_entity.dart';
import '../../domain/history_query.dart';
import '../../domain/history_session_policy.dart';
import '../providers/devices_providers.dart';

class DeviceHistoryListState {
  const DeviceHistoryListState({
    this.isLoading = false,
    this.events = const [],
    this.commands = const [],
    this.query = '',
    this.bufferedOnly = false,
    this.commandStatusFilter,
    this.eventsPage = 0,
    this.commandsPage = 0,
    this.error,
  });

  final bool isLoading;
  final List<AlarmEventEntity> events;
  final List<DeviceCommandEntity> commands;
  final String query;
  final bool bufferedOnly;
  final String? commandStatusFilter;
  final int eventsPage;
  final int commandsPage;
  final ErrorState? error;

  List<AlarmEventEntity> get filteredEvents =>
      filterAlarmEvents(events, query: query, bufferedOnly: bufferedOnly);

  List<DeviceCommandEntity> get filteredCommands =>
      filterCommands(commands, query: query, statusFilter: commandStatusFilter);

  HistoryPage<AlarmEventEntity> get eventsWindow =>
      sliceHistoryPage(filteredEvents, pageIndex: eventsPage);

  HistoryPage<DeviceCommandEntity> get commandsWindow =>
      sliceHistoryPage(filteredCommands, pageIndex: commandsPage);

  DeviceHistoryListState copyWith({
    bool? isLoading,
    List<AlarmEventEntity>? events,
    List<DeviceCommandEntity>? commands,
    String? query,
    bool? bufferedOnly,
    String? commandStatusFilter,
    bool clearCommandStatusFilter = false,
    int? eventsPage,
    int? commandsPage,
    ErrorState? error,
    bool clearError = false,
  }) {
    return DeviceHistoryListState(
      isLoading: isLoading ?? this.isLoading,
      events: events ?? this.events,
      commands: commands ?? this.commands,
      query: query ?? this.query,
      bufferedOnly: bufferedOnly ?? this.bufferedOnly,
      commandStatusFilter: clearCommandStatusFilter
          ? null
          : (commandStatusFilter ?? this.commandStatusFilter),
      eventsPage: eventsPage ?? this.eventsPage,
      commandsPage: commandsPage ?? this.commandsPage,
      error: clearError ? null : (error ?? this.error),
    );
  }
}

class DeviceHistoryListViewModel
    extends FamilyNotifier<DeviceHistoryListState, String> {
  int _generation = 0;

  @override
  DeviceHistoryListState build(String deviceId) {
    ref.listen<AuthSessionState>(authSessionProvider, (previous, next) {
      if (!_sessionClears(previous, next)) return;
      _generation++;
      _clearRepositories();
      state = const DeviceHistoryListState();
      if (next.isAuthenticated) Future.microtask(load);
    });
    Future.microtask(load);
    return const DeviceHistoryListState(isLoading: true);
  }

  Future<void> load() async {
    final generation = ++_generation;
    final deviceId = arg;
    if (!ref.read(authSessionProvider).isAuthenticated) {
      state = const DeviceHistoryListState();
      return;
    }

    state = state.copyWith(isLoading: true, clearError: true);
    final eventsResult = await ref.read(listDeviceAlarmEventsUseCaseProvider)(
      deviceId,
    );
    if (generation != _generation) return;
    final commandsResult = await ref.read(listDeviceCommandsUseCaseProvider)(
      deviceId,
    );
    if (generation != _generation) return;

    var events = <AlarmEventEntity>[];
    var commands = <DeviceCommandEntity>[];
    Failure? failure;

    switch (eventsResult) {
      case Success(:final data):
        events = data;
      case Failure_(failure: final eventFailure):
        failure = eventFailure;
    }
    switch (commandsResult) {
      case Success(:final data):
        commands = data;
      case Failure_(failure: final commandFailure):
        failure ??= commandFailure;
    }

    final denied =
        (eventsResult.failureOrNull != null &&
            historyAccessDropped(eventsResult.failureOrNull!)) ||
        (commandsResult.failureOrNull != null &&
            historyAccessDropped(commandsResult.failureOrNull!));
    if (denied) {
      _clearRepositories(deviceId: deviceId);
      events = const [];
      commands = const [];
    }

    if (generation != _generation) return;
    state = state.copyWith(
      isLoading: false,
      events: events,
      commands: commands,
      error: failure == null
          ? null
          : FailureHandler.handle(failure, onRetry: load),
      clearError: failure == null,
    );
  }

  void setQuery(String value) {
    state = state.copyWith(query: value, eventsPage: 0, commandsPage: 0);
  }

  void setBufferedOnly(bool value) {
    state = state.copyWith(bufferedOnly: value, eventsPage: 0);
  }

  void setCommandStatusFilter(String? statusCode) {
    if (statusCode == null || statusCode.isEmpty) {
      state = state.copyWith(clearCommandStatusFilter: true, commandsPage: 0);
      return;
    }
    state = state.copyWith(commandStatusFilter: statusCode, commandsPage: 0);
  }

  void setEventsPage(int page) {
    state = state.copyWith(eventsPage: page);
  }

  void setCommandsPage(int page) {
    state = state.copyWith(commandsPage: page);
  }

  bool _sessionClears(AuthSessionState? previous, AuthSessionState next) {
    return historyCacheMustClear(
      wasAuthenticated: previous?.isAuthenticated == true,
      isAuthenticated: next.isAuthenticated,
      previousUserId: previous?.user?.id,
      nextUserId: next.user?.id,
    );
  }

  void _clearRepositories({String? deviceId}) {
    final events = ref.read(deviceAlarmEventsRepositoryProvider);
    final commands = ref.read(deviceCommandsRepositoryProvider);
    if (deviceId == null) {
      events.clearCache();
      commands.clearCache();
      return;
    }
    events.dropDevice(deviceId);
    commands.dropDevice(deviceId);
  }
}

final deviceHistoryListViewModelProvider =
    NotifierProvider.family<
      DeviceHistoryListViewModel,
      DeviceHistoryListState,
      String
    >(DeviceHistoryListViewModel.new);

class AlarmEventDetailArgs {
  const AlarmEventDetailArgs({
    required this.deviceId,
    required this.alarmEventId,
  });

  final String deviceId;
  final String alarmEventId;

  @override
  bool operator ==(Object other) =>
      other is AlarmEventDetailArgs &&
      other.deviceId == deviceId &&
      other.alarmEventId == alarmEventId;

  @override
  int get hashCode => Object.hash(deviceId, alarmEventId);
}

class AlarmEventDetailState {
  const AlarmEventDetailState({this.isLoading = false, this.event, this.error});

  final bool isLoading;
  final AlarmEventEntity? event;
  final ErrorState? error;
}

class AlarmEventDetailViewModel
    extends FamilyNotifier<AlarmEventDetailState, AlarmEventDetailArgs> {
  int _generation = 0;

  @override
  AlarmEventDetailState build(AlarmEventDetailArgs args) {
    ref.listen<AuthSessionState>(authSessionProvider, (previous, next) {
      if (!historyCacheMustClear(
        wasAuthenticated: previous?.isAuthenticated == true,
        isAuthenticated: next.isAuthenticated,
        previousUserId: previous?.user?.id,
        nextUserId: next.user?.id,
      )) {
        return;
      }
      _generation++;
      ref.read(deviceAlarmEventsRepositoryProvider).clearCache();
      state = const AlarmEventDetailState();
    });
    Future.microtask(load);
    return const AlarmEventDetailState(isLoading: true);
  }

  Future<void> load() async {
    final generation = ++_generation;
    final deviceId = arg.deviceId;
    final alarmEventId = arg.alarmEventId;
    if (!ref.read(authSessionProvider).isAuthenticated) {
      state = const AlarmEventDetailState();
      return;
    }
    state = const AlarmEventDetailState(isLoading: true);
    final result = await ref.read(getDeviceAlarmEventUseCaseProvider)(
      deviceId: deviceId,
      alarmEventId: alarmEventId,
    );
    if (generation != _generation) return;
    state = switch (result) {
      Success(:final data) =>
        _matches(data, deviceId, alarmEventId)
            ? AlarmEventDetailState(event: data)
            : AlarmEventDetailState(
                error: FailureHandler.handle(
                  const Failure.notFound(message: 'الحادثة غير موجودة.'),
                  onRetry: load,
                ),
              ),
      Failure_(:final failure) => AlarmEventDetailState(
        error: FailureHandler.handle(failure, onRetry: load),
      ),
    };
  }

  bool _matches(AlarmEventEntity event, String deviceId, String alarmEventId) {
    return event.deviceId == deviceId && event.alarmEventId == alarmEventId;
  }
}

final alarmEventDetailViewModelProvider =
    NotifierProvider.family<
      AlarmEventDetailViewModel,
      AlarmEventDetailState,
      AlarmEventDetailArgs
    >(AlarmEventDetailViewModel.new);

class CommandDetailArgs {
  const CommandDetailArgs({required this.deviceId, required this.commandId});

  final String deviceId;
  final String commandId;

  @override
  bool operator ==(Object other) =>
      other is CommandDetailArgs &&
      other.deviceId == deviceId &&
      other.commandId == commandId;

  @override
  int get hashCode => Object.hash(deviceId, commandId);
}

class CommandDetailState {
  const CommandDetailState({this.isLoading = false, this.command, this.error});

  final bool isLoading;
  final DeviceCommandEntity? command;
  final ErrorState? error;
}

class CommandDetailViewModel
    extends FamilyNotifier<CommandDetailState, CommandDetailArgs> {
  int _generation = 0;

  @override
  CommandDetailState build(CommandDetailArgs args) {
    ref.listen<AuthSessionState>(authSessionProvider, (previous, next) {
      if (!historyCacheMustClear(
        wasAuthenticated: previous?.isAuthenticated == true,
        isAuthenticated: next.isAuthenticated,
        previousUserId: previous?.user?.id,
        nextUserId: next.user?.id,
      )) {
        return;
      }
      _generation++;
      ref.read(deviceCommandsRepositoryProvider).clearCache();
      state = const CommandDetailState();
    });
    Future.microtask(load);
    return const CommandDetailState(isLoading: true);
  }

  Future<void> load() async {
    final generation = ++_generation;
    final deviceId = arg.deviceId;
    final commandId = arg.commandId;
    if (!ref.read(authSessionProvider).isAuthenticated) {
      state = const CommandDetailState();
      return;
    }
    state = const CommandDetailState(isLoading: true);
    final result = await ref.read(getDeviceCommandUseCaseProvider)(
      deviceId: deviceId,
      commandId: commandId,
    );
    if (generation != _generation) return;
    state = switch (result) {
      Success(:final data) =>
        data.deviceId == deviceId && data.commandId == commandId
            ? CommandDetailState(command: data)
            : CommandDetailState(
                error: FailureHandler.handle(
                  const Failure.notFound(message: 'الأمر غير موجود.'),
                  onRetry: load,
                ),
              ),
      Failure_(:final failure) => CommandDetailState(
        error: FailureHandler.handle(failure, onRetry: load),
      ),
    };
  }
}

final commandDetailViewModelProvider =
    NotifierProvider.family<
      CommandDetailViewModel,
      CommandDetailState,
      CommandDetailArgs
    >(CommandDetailViewModel.new);

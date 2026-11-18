import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../domain/entities/alarm_event_entity.dart';
import '../../domain/entities/device_command_entity.dart';
import '../providers/devices_providers.dart';

class DeviceHistoryListState {
  const DeviceHistoryListState({
    this.isLoading = false,
    this.events = const [],
    this.commands = const [],
    this.query = '',
    this.bufferedOnly = false,
    this.commandStatusFilter,
    this.error,
  });

  final bool isLoading;
  final List<AlarmEventEntity> events;
  final List<DeviceCommandEntity> commands;
  final String query;
  final bool bufferedOnly;
  final String? commandStatusFilter;
  final Failure? error;

  List<AlarmEventEntity> get filteredEvents => filterAlarmEvents(
        events,
        query: query,
        bufferedOnly: bufferedOnly,
      );

  List<DeviceCommandEntity> get filteredCommands => filterCommands(
        commands,
        query: query,
        statusFilter: commandStatusFilter,
      );

  DeviceHistoryListState copyWith({
    bool? isLoading,
    List<AlarmEventEntity>? events,
    List<DeviceCommandEntity>? commands,
    String? query,
    bool? bufferedOnly,
    String? commandStatusFilter,
    bool clearCommandStatusFilter = false,
    Failure? error,
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
      error: clearError ? null : (error ?? this.error),
    );
  }
}

/// Pure local filters — Backend has no server-side pagination/search yet.
List<AlarmEventEntity> filterAlarmEvents(
  List<AlarmEventEntity> source, {
  required String query,
  required bool bufferedOnly,
}) {
  final q = query.trim().toLowerCase();
  return source
      .where((e) {
        if (bufferedOnly && !e.wasBuffered) return false;
        if (q.isEmpty) return true;
        return e.alarmEventId.toLowerCase().contains(q) ||
            e.eventUid.toLowerCase().contains(q) ||
            e.sensorKeySnapshot.toLowerCase().contains(q) ||
            e.status.code.toLowerCase().contains(q);
      })
      .toList(growable: false);
}

List<DeviceCommandEntity> filterCommands(
  List<DeviceCommandEntity> source, {
  required String query,
  String? statusFilter,
}) {
  final q = query.trim().toLowerCase();
  final status = statusFilter?.trim().toUpperCase();
  return source
      .where((c) {
        if (status != null &&
            status.isNotEmpty &&
            c.status.code.toUpperCase() != status) {
          return false;
        }
        if (q.isEmpty) return true;
        return c.commandId.toLowerCase().contains(q) ||
            c.type.code.toLowerCase().contains(q) ||
            c.status.code.toLowerCase().contains(q) ||
            (c.rejectCode?.toLowerCase().contains(q) ?? false);
      })
      .toList(growable: false);
}

class DeviceHistoryListViewModel
    extends StateNotifier<DeviceHistoryListState> {
  DeviceHistoryListViewModel(this._ref, this.deviceId)
      : super(const DeviceHistoryListState()) {
    load();
  }

  final Ref _ref;
  final String deviceId;

  Future<void> load() async {
    state = state.copyWith(isLoading: true, clearError: true);
    final eventsResult = await _ref
        .read(deviceAlarmEventsRepositoryProvider)
        .listByDevice(deviceId);
    final commandsResult =
        await _ref.read(deviceCommandsRepositoryProvider).listByDevice(deviceId);

    Failure? error;
    var events = <AlarmEventEntity>[];
    var commands = <DeviceCommandEntity>[];

    switch (eventsResult) {
      case Success(:final data):
        events = data;
      case Failure_(:final failure):
        error = failure;
        _ref.read(deviceAlarmEventsRepositoryProvider).dropDevice(deviceId);
    }

    switch (commandsResult) {
      case Success(:final data):
        commands = data;
      case Failure_(:final failure):
        error ??= failure;
    }

    state = state.copyWith(
      isLoading: false,
      events: events,
      commands: commands,
      error: error,
      clearError: error == null,
    );
  }

  void setQuery(String value) {
    state = state.copyWith(query: value);
  }

  void setBufferedOnly(bool value) {
    state = state.copyWith(bufferedOnly: value);
  }

  void setCommandStatusFilter(String? statusCode) {
    if (statusCode == null || statusCode.isEmpty) {
      state = state.copyWith(clearCommandStatusFilter: true);
      return;
    }
    state = state.copyWith(commandStatusFilter: statusCode);
  }
}

final deviceHistoryListViewModelProvider = StateNotifierProvider.autoDispose
    .family<DeviceHistoryListViewModel, DeviceHistoryListState, String>(
  (ref, deviceId) => DeviceHistoryListViewModel(ref, deviceId),
);

class AlarmEventDetailState {
  const AlarmEventDetailState({
    this.isLoading = false,
    this.event,
    this.error,
  });

  final bool isLoading;
  final AlarmEventEntity? event;
  final Failure? error;

  AlarmEventDetailState copyWith({
    bool? isLoading,
    AlarmEventEntity? event,
    Failure? error,
    bool clearError = false,
  }) {
    return AlarmEventDetailState(
      isLoading: isLoading ?? this.isLoading,
      event: event ?? this.event,
      error: clearError ? null : (error ?? this.error),
    );
  }
}

class AlarmEventDetailViewModel extends StateNotifier<AlarmEventDetailState> {
  AlarmEventDetailViewModel(this._ref, this.deviceId, this.alarmEventId)
      : super(const AlarmEventDetailState()) {
    load();
  }

  final Ref _ref;
  final String deviceId;
  final String alarmEventId;

  Future<void> load() async {
    state = state.copyWith(isLoading: true, clearError: true);
    final result = await _ref.read(deviceAlarmEventsRepositoryProvider).getById(
          deviceId: deviceId,
          alarmEventId: alarmEventId,
        );
    state = switch (result) {
      Success(:final data) => AlarmEventDetailState(event: data),
      Failure_(:final failure) => () {
          _ref
              .read(deviceAlarmEventsRepositoryProvider)
              .dropDevice(deviceId);
          return AlarmEventDetailState(error: failure);
        }(),
    };
  }
}

typedef _AlarmEventDetailKey = ({String deviceId, String alarmEventId});

final alarmEventDetailViewModelProvider = StateNotifierProvider.autoDispose
    .family<AlarmEventDetailViewModel, AlarmEventDetailState,
        _AlarmEventDetailKey>(
  (ref, key) =>
      AlarmEventDetailViewModel(ref, key.deviceId, key.alarmEventId),
);

class CommandDetailState {
  const CommandDetailState({
    this.isLoading = false,
    this.command,
    this.error,
  });

  final bool isLoading;
  final DeviceCommandEntity? command;
  final Failure? error;

  CommandDetailState copyWith({
    bool? isLoading,
    DeviceCommandEntity? command,
    Failure? error,
    bool clearError = false,
  }) {
    return CommandDetailState(
      isLoading: isLoading ?? this.isLoading,
      command: command ?? this.command,
      error: clearError ? null : (error ?? this.error),
    );
  }
}

class CommandDetailViewModel extends StateNotifier<CommandDetailState> {
  CommandDetailViewModel(this._ref, this.deviceId, this.commandId)
      : super(const CommandDetailState()) {
    load();
  }

  final Ref _ref;
  final String deviceId;
  final String commandId;

  Future<void> load() async {
    state = state.copyWith(isLoading: true, clearError: true);
    final result = await _ref.read(deviceCommandsRepositoryProvider).getById(
          deviceId: deviceId,
          commandId: commandId,
        );
    state = switch (result) {
      Success(:final data) => CommandDetailState(command: data),
      Failure_(:final failure) => CommandDetailState(error: failure),
    };
  }
}

typedef _CommandDetailKey = ({String deviceId, String commandId});

final commandDetailViewModelProvider = StateNotifierProvider.autoDispose
    .family<CommandDetailViewModel, CommandDetailState, _CommandDetailKey>(
  (ref, key) => CommandDetailViewModel(ref, key.deviceId, key.commandId),
);

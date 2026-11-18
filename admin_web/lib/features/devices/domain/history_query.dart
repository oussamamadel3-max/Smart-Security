import 'package:smart_security_app_core/smart_security_app_core.dart';

import 'entities/alarm_event_entity.dart';
import 'entities/device_command_entity.dart';

/// UI window over the full list returned by the owner history API.
///
/// `OwnerDeviceAlarmEventsController` and `OwnerDeviceCommandsController`
/// accept no page or search query. Search and paging stay on the client.
const int historyPageSize = 8;

class HistoryPage<T> {
  const HistoryPage({
    required this.items,
    required this.pageIndex,
    required this.pageCount,
    required this.total,
  });

  final List<T> items;
  final int pageIndex;
  final int pageCount;
  final int total;
}

HistoryPage<T> sliceHistoryPage<T>(
  List<T> source, {
  required int pageIndex,
  int pageSize = historyPageSize,
}) {
  if (source.isEmpty) {
    return HistoryPage<T>(
      items: List<T>.empty(),
      pageIndex: 0,
      pageCount: 1,
      total: 0,
    );
  }
  final size = pageSize < 1 ? source.length : pageSize;
  final pageCount = (source.length / size).ceil();
  final safeIndex = pageIndex < 0
      ? 0
      : (pageIndex >= pageCount ? pageCount - 1 : pageIndex);
  final start = safeIndex * size;
  final end = start + size > source.length ? source.length : start + size;
  return HistoryPage<T>(
    items: source.sublist(start, end),
    pageIndex: safeIndex,
    pageCount: pageCount,
    total: source.length,
  );
}

List<AlarmEventEntity> filterAlarmEvents(
  List<AlarmEventEntity> source, {
  required String query,
  required bool bufferedOnly,
}) {
  final q = query.trim().toLowerCase();
  return source
      .where((event) {
        if (bufferedOnly && !event.wasBuffered) return false;
        if (q.isEmpty) return true;
        return event.alarmEventId.toLowerCase().contains(q) ||
            event.eventUid.toLowerCase().contains(q) ||
            event.sensorKeySnapshot.toLowerCase().contains(q) ||
            event.status.code.toLowerCase().contains(q);
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
      .where((command) {
        if (status != null &&
            status.isNotEmpty &&
            command.status.code.toUpperCase() != status) {
          return false;
        }
        if (q.isEmpty) return true;
        return command.commandId.toLowerCase().contains(q) ||
            command.type.code.toLowerCase().contains(q) ||
            command.status.code.toLowerCase().contains(q) ||
            (command.rejectCode?.toLowerCase().contains(q) ?? false);
      })
      .toList(growable: false);
}

bool isSirenTestCommand(DeviceCommandEntity command) =>
    command.type == CommandType.testSiren ||
    command.type == CommandType.stopTest;

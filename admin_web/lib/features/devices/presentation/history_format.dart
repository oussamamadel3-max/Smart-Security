import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../domain/entities/alarm_event_entity.dart';
import '../domain/entities/device_command_entity.dart';
import '../domain/history_query.dart';

String formatHistoryTime(DateTime? value) {
  if (value == null) return '—';
  final local = value.toLocal();
  String two(int n) => n.toString().padLeft(2, '0');
  return '${local.year}-${two(local.month)}-${two(local.day)} '
      '${two(local.hour)}:${two(local.minute)}';
}

/// Occurrence text. Untrusted clocks stay a label; no delay is calculated.
String occurrenceText(AppLocalizations tr, AlarmEventEntity event) {
  if (!event.hasTrustedOccurrence) return tr.occurrenceUntrustedShort;
  return formatHistoryTime(event.occurredDeviceAt);
}

String commandHistoryTitle(AppLocalizations tr, DeviceCommandEntity command) {
  if (isSirenTestCommand(command)) {
    return '${tr.sirenTestCommand} · ${command.type.code}';
  }
  return command.type.code;
}

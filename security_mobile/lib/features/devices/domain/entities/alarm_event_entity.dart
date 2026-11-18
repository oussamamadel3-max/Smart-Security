import 'package:smart_security_app_core/smart_security_app_core.dart';

class AlarmEventEntity {
  const AlarmEventEntity({
    required this.alarmEventId,
    required this.deviceId,
    required this.eventUid,
    required this.ownershipId,
    required this.ownershipVersion,
    required this.status,
    required this.receivedAt,
    required this.clockTrusted,
    required this.wasBuffered,
    required this.sensorKeySnapshot,
    this.occurredDeviceAt,
    this.mutedAt,
  });

  final String alarmEventId;
  final String deviceId;
  final String eventUid;
  final String ownershipId;
  final int ownershipVersion;
  final AlarmEventStatus status;
  final DateTime receivedAt;
  final DateTime? occurredDeviceAt;
  final bool clockTrusted;
  final bool wasBuffered;
  final DateTime? mutedAt;
  final String sensorKeySnapshot;

  /// Show device occurrence only when the server trusted the clock.
  bool get hasTrustedOccurrence =>
      clockTrusted && occurredDeviceAt != null;

  /// Buffered delivery is a label only — never invent a delay duration.
  bool get showBufferedNotice => wasBuffered;
}

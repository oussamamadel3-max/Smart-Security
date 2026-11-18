import '../../../../core/domain/device_status_codes.dart';

class DeviceIndicatorsEntity {
  const DeviceIndicatorsEntity({
    required this.connection,
    required this.arm,
    required this.siren,
    required this.health,
    this.healthCode,
    this.lastSeenAt,
    this.lastStateAt,
    this.stateSequence = 0,
    this.clockTrusted = false,
    this.currentAlarmEventUid,
    this.powerSource,
    this.batteryPercent,
    this.heartbeatTimeoutSeconds = 0,
  });

  final ConnectionStatus connection;
  final ArmStatus arm;
  final SirenStatus siren;
  final DiagnosticStatus health;
  final String? healthCode;
  final DateTime? lastSeenAt;
  final DateTime? lastStateAt;
  final int stateSequence;
  final bool clockTrusted;
  final String? currentAlarmEventUid;
  final String? powerSource;
  final int? batteryPercent;
  final int heartbeatTimeoutSeconds;

  /// Offline must never be equated with disarm or healthy/fault.
  bool get showsStaleWarning => connection == ConnectionStatus.offline;
}

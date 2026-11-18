import 'package:smart_security_app_core/smart_security_app_core.dart';

/// Last known admin indicators. Offline is independent of arm and health.
class AdminDeviceIndicatorsEntity {
  const AdminDeviceIndicatorsEntity({
    required this.connection,
    required this.arm,
    required this.siren,
    required this.health,
    this.healthCode,
    this.lastSeenAt,
    this.lastStateAt,
    this.stateSequence = 0,
    this.clockTrusted = false,
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
  final String? powerSource;
  final int? batteryPercent;
  final int heartbeatTimeoutSeconds;
}

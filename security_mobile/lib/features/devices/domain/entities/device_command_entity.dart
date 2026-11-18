import 'package:smart_security_app_core/smart_security_app_core.dart';

class DeviceCommandEntity {
  const DeviceCommandEntity({
    required this.commandId,
    required this.deviceId,
    required this.type,
    required this.status,
    required this.commandSequence,
    required this.ownershipVersion,
    required this.observedStateSequence,
    required this.createdAt,
    required this.expiresAt,
    required this.actualStateChanged,
    required this.idempotentReplay,
    this.targetAlarmEventUid,
    this.rejectCode,
    this.resultStateSequence,
    this.sentAt,
    this.acknowledgedAt,
    this.reconciledAt,
    this.confirmedArmState,
    this.confirmedSirenState,
  });

  final String commandId;
  final String deviceId;
  final CommandType type;
  final CommandStatus status;
  final int commandSequence;
  final int ownershipVersion;
  final int observedStateSequence;
  final DateTime createdAt;
  final DateTime expiresAt;
  final String? targetAlarmEventUid;
  final String? rejectCode;
  final int? resultStateSequence;
  final DateTime? sentAt;
  final DateTime? acknowledgedAt;
  final DateTime? reconciledAt;
  final bool idempotentReplay;
  final bool actualStateChanged;
  final ArmStatus? confirmedArmState;
  final SirenStatus? confirmedSirenState;

  /// HTTP accept / PENDING is never device execution.
  bool get isPendingRequest => status == CommandStatus.pending;

  bool get isTerminal =>
      status == CommandStatus.executed ||
      status == CommandStatus.rejected ||
      status == CommandStatus.timedOut ||
      status == CommandStatus.cancelled;

  /// TIMED_OUT remains unresolved until reconciliation.
  bool get blocksConflictingUntilReconciled =>
      status == CommandStatus.timedOut && reconciledAt == null;
}

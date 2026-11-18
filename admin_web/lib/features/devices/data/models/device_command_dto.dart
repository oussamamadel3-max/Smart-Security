class CreateDeviceCommandRequestDto {
  const CreateDeviceCommandRequestDto({
    required this.type,
    required this.idempotencyKey,
    this.targetAlarmEventUid,
  });

  final String type;
  final String idempotencyKey;
  final String? targetAlarmEventUid;

  Map<String, dynamic> toJson() => {
    'type': type,
    'idempotencyKey': idempotencyKey,
    if (targetAlarmEventUid != null) 'targetAlarmEventUid': targetAlarmEventUid,
  };
}

class DeviceCommandDto {
  const DeviceCommandDto({
    required this.commandId,
    required this.deviceId,
    required this.type,
    required this.status,
    required this.commandSequence,
    required this.ownershipVersion,
    required this.observedStateSequence,
    required this.createdAt,
    required this.expiresAt,
    required this.idempotentReplay,
    required this.actualStateChanged,
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
  final String type;
  final String status;
  final int commandSequence;
  final int ownershipVersion;
  final int observedStateSequence;
  final String createdAt;
  final String expiresAt;
  final String? targetAlarmEventUid;
  final String? rejectCode;
  final int? resultStateSequence;
  final String? sentAt;
  final String? acknowledgedAt;
  final String? reconciledAt;
  final bool idempotentReplay;
  final bool actualStateChanged;
  final String? confirmedArmState;
  final String? confirmedSirenState;

  factory DeviceCommandDto.fromJson(Map<String, dynamic> json) {
    return DeviceCommandDto(
      commandId: (json['commandId'] as String? ?? '').trim(),
      deviceId: (json['deviceId'] as String? ?? '').trim(),
      type: (json['type'] as String? ?? '').trim(),
      status: (json['status'] as String? ?? '').trim(),
      commandSequence: (json['commandSequence'] as num?)?.toInt() ?? 0,
      ownershipVersion: (json['ownershipVersion'] as num?)?.toInt() ?? 0,
      observedStateSequence:
          (json['observedStateSequence'] as num?)?.toInt() ?? 0,
      createdAt: json['createdAt']?.toString() ?? '',
      expiresAt: json['expiresAt']?.toString() ?? '',
      targetAlarmEventUid: json['targetAlarmEventUid']?.toString(),
      rejectCode: (json['rejectCode'] as String?)?.trim(),
      resultStateSequence: (json['resultStateSequence'] as num?)?.toInt(),
      sentAt: json['sentAt']?.toString(),
      acknowledgedAt: json['acknowledgedAt']?.toString(),
      reconciledAt: json['reconciledAt']?.toString(),
      idempotentReplay: json['idempotentReplay'] as bool? ?? false,
      actualStateChanged: json['actualStateChanged'] as bool? ?? false,
      confirmedArmState: (json['confirmedArmState'] as String?)?.trim(),
      confirmedSirenState: (json['confirmedSirenState'] as String?)?.trim(),
    );
  }
}

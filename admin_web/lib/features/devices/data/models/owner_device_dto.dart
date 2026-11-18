class OwnerDeviceIndicatorsDto {
  const OwnerDeviceIndicatorsDto({
    required this.connectionStatus,
    required this.armState,
    required this.sirenState,
    required this.healthState,
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

  final String connectionStatus;
  final String armState;
  final String sirenState;
  final String healthState;
  final String? healthCode;
  final String? lastSeenAt;
  final String? lastStateAt;
  final int stateSequence;
  final bool clockTrusted;
  final String? currentAlarmEventUid;
  final String? powerSource;
  final int? batteryPercent;
  final int heartbeatTimeoutSeconds;

  factory OwnerDeviceIndicatorsDto.fromJson(Map<String, dynamic> json) {
    return OwnerDeviceIndicatorsDto(
      connectionStatus: (json['connectionStatus'] as String? ?? '').trim(),
      armState: (json['armState'] as String? ?? '').trim(),
      sirenState: (json['sirenState'] as String? ?? '').trim(),
      healthState: (json['healthState'] as String? ?? '').trim(),
      healthCode: (json['healthCode'] as String?)?.trim(),
      lastSeenAt: json['lastSeenAt']?.toString(),
      lastStateAt: json['lastStateAt']?.toString(),
      stateSequence: (json['stateSequence'] as num?)?.toInt() ?? 0,
      clockTrusted: json['clockTrusted'] as bool? ?? false,
      currentAlarmEventUid: json['currentAlarmEventUid']?.toString(),
      powerSource: (json['powerSource'] as String?)?.trim(),
      batteryPercent: (json['batteryPercent'] as num?)?.toInt(),
      heartbeatTimeoutSeconds:
          (json['heartbeatTimeoutSeconds'] as num?)?.toInt() ?? 0,
    );
  }
}

class OwnerDeviceDto {
  const OwnerDeviceDto({
    required this.deviceId,
    required this.displayName,
    required this.ownershipStatus,
    required this.ownershipVersion,
    required this.confirmedOwnershipVersion,
    required this.awaitingOwnershipAck,
    required this.canControl,
    required this.linkStatusMessage,
    this.indicators,
    this.deviceLifecycleStatus,
  });

  final String deviceId;
  final String displayName;
  final String ownershipStatus;
  final int ownershipVersion;
  final int confirmedOwnershipVersion;
  final bool awaitingOwnershipAck;
  final bool canControl;
  final String linkStatusMessage;
  final OwnerDeviceIndicatorsDto? indicators;
  final String? deviceLifecycleStatus;

  factory OwnerDeviceDto.fromJson(Map<String, dynamic> json) {
    final rawIndicators = json['indicators'];
    return OwnerDeviceDto(
      deviceId: (json['deviceId'] as String? ?? '').trim(),
      displayName: (json['displayName'] as String? ?? '').trim(),
      ownershipStatus: (json['ownershipStatus'] as String? ?? '').trim(),
      ownershipVersion: (json['ownershipVersion'] as num?)?.toInt() ?? 0,
      confirmedOwnershipVersion:
          (json['confirmedOwnershipVersion'] as num?)?.toInt() ?? 0,
      awaitingOwnershipAck: json['awaitingOwnershipAck'] as bool? ?? false,
      canControl: json['canControl'] as bool? ?? false,
      linkStatusMessage: (json['linkStatusMessage'] as String? ?? '').trim(),
      deviceLifecycleStatus: (json['deviceLifecycleStatus'] as String?)?.trim(),
      indicators: rawIndicators is Map
          ? OwnerDeviceIndicatorsDto.fromJson(
              Map<String, dynamic>.from(rawIndicators),
            )
          : null,
    );
  }
}

class AlarmEventDto {
  const AlarmEventDto({
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
  final String status;
  final String receivedAt;
  final String? occurredDeviceAt;
  final bool clockTrusted;
  final bool wasBuffered;
  final String? mutedAt;
  final String sensorKeySnapshot;

  factory AlarmEventDto.fromJson(Map<String, dynamic> json) {
    return AlarmEventDto(
      alarmEventId: (json['alarmEventId'] as String? ?? '').trim(),
      deviceId: (json['deviceId'] as String? ?? '').trim(),
      eventUid: (json['eventUid'] as String? ?? '').trim(),
      ownershipId: (json['ownershipId'] as String? ?? '').trim(),
      ownershipVersion: (json['ownershipVersion'] as num?)?.toInt() ?? 0,
      status: (json['status'] as String? ?? '').trim(),
      receivedAt: json['receivedAt']?.toString() ?? '',
      occurredDeviceAt: json['occurredDeviceAt']?.toString(),
      clockTrusted: json['clockTrusted'] as bool? ?? false,
      wasBuffered: json['wasBuffered'] as bool? ?? false,
      mutedAt: json['mutedAt']?.toString(),
      sensorKeySnapshot: (json['sensorKeySnapshot'] as String? ?? '').trim(),
    );
  }
}

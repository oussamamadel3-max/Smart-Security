class OwnerNotificationListItemDto {
  const OwnerNotificationListItemDto({
    required this.notificationId,
    required this.notificationType,
    required this.status,
    required this.createdAt,
    this.alarmEventId,
    this.deviceFaultId,
    this.adminAlertId,
  });

  final String notificationId;
  final String notificationType;
  final String status;
  final String createdAt;
  final String? alarmEventId;
  final String? deviceFaultId;
  final String? adminAlertId;

  factory OwnerNotificationListItemDto.fromJson(Map<String, dynamic> json) {
    return OwnerNotificationListItemDto(
      notificationId: (json['notificationId'] as String? ?? '').trim(),
      notificationType: (json['notificationType'] as String? ?? '').trim(),
      status: (json['status'] as String? ?? '').trim(),
      createdAt: json['createdAt']?.toString() ?? '',
      alarmEventId: (json['alarmEventId'] as String?)?.trim(),
      deviceFaultId: (json['deviceFaultId'] as String?)?.trim(),
      adminAlertId: (json['adminAlertId'] as String?)?.trim(),
    );
  }
}

class OwnerNotificationDetailDto {
  const OwnerNotificationDetailDto({
    required this.notificationId,
    required this.notificationType,
    required this.status,
    required this.createdAt,
    this.deviceId,
    this.alarmEventId,
    this.alarmEventDeviceId,
  });

  final String notificationId;
  final String notificationType;
  final String status;
  final String createdAt;
  final String? deviceId;
  final String? alarmEventId;
  final String? alarmEventDeviceId;

  factory OwnerNotificationDetailDto.fromJson(Map<String, dynamic> json) {
    final alarm = json['alarmEvent'];
    Map<String, dynamic>? alarmMap;
    if (alarm is Map<String, dynamic>) {
      alarmMap = alarm;
    } else if (alarm is Map) {
      alarmMap = Map<String, dynamic>.from(alarm);
    }

    return OwnerNotificationDetailDto(
      notificationId: (json['notificationId'] as String? ?? '').trim(),
      notificationType: (json['notificationType'] as String? ?? '').trim(),
      status: (json['status'] as String? ?? '').trim(),
      createdAt: json['createdAt']?.toString() ?? '',
      deviceId: (json['deviceId'] as String?)?.trim(),
      alarmEventId: (alarmMap?['alarmEventId'] as String?)?.trim(),
      alarmEventDeviceId: (alarmMap?['deviceId'] as String?)?.trim(),
    );
  }
}

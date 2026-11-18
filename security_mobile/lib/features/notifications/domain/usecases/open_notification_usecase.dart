import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../data/models/owner_notification_dto.dart';
import '../repositories/owner_notifications_repository.dart';

/// Result of opening an FCM data payload — never invents local alarm details.
sealed class OpenNotificationResult {
  const OpenNotificationResult();
}

class OpenNotificationNavigateAlarm extends OpenNotificationResult {
  const OpenNotificationNavigateAlarm({
    required this.deviceId,
    required this.alarmEventId,
  });

  final String deviceId;
  final String alarmEventId;
}

class OpenNotificationDenied extends OpenNotificationResult {
  const OpenNotificationDenied(this.failure);
  final Failure failure;
}

class OpenNotificationUnsupported extends OpenNotificationResult {
  const OpenNotificationUnsupported();
}

class OpenNotificationUseCase {
  OpenNotificationUseCase(this._repository);

  final OwnerNotificationsRepository _repository;

  Future<OpenNotificationResult> call(Map<String, String> data) async {
    final notificationId = (data['notificationId'] ?? '').trim();
    if (notificationId.isEmpty) {
      return const OpenNotificationUnsupported();
    }

    final result = await _repository.getById(notificationId);
    return switch (result) {
      Failure_(:final failure) => OpenNotificationDenied(failure),
      Success(:final data) => _mapDetail(data),
    };
  }

  OpenNotificationResult _mapDetail(OwnerNotificationDetailDto detail) {
    final deviceId =
        (detail.alarmEventDeviceId ?? detail.deviceId ?? '').trim();
    final alarmEventId = (detail.alarmEventId ?? '').trim();
    if (deviceId.isEmpty || alarmEventId.isEmpty) {
      return const OpenNotificationUnsupported();
    }
    return OpenNotificationNavigateAlarm(
      deviceId: deviceId,
      alarmEventId: alarmEventId,
    );
  }
}

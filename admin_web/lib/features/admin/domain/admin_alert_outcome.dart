import 'entities/admin_alert_entity.dart';

/// Separates a saved alert from a provider delivery attempt.
///
/// `ACCEPTED` or `FAILED` without [AdminAlertEntity.lastAttemptAt] is not a
/// completed attempt. Read is never inferred here.
abstract final class AdminAlertOutcome {
  static const pending = 'PENDING';
  static const failed = 'FAILED';
  static const accepted = 'ACCEPTED';
  static const cancelled = 'CANCELLED';

  static bool requestAccepted(AdminAlertEntity alert) =>
      alert.adminAlertId.trim().isNotEmpty;

  static bool attemptRecorded(AdminAlertEntity alert) =>
      alert.deliveryAttempted && alert.lastAttemptAt != null;

  static bool deliverySucceeded(AdminAlertEntity alert) =>
      attemptRecorded(alert) && alert.deliveryAttemptStatus == accepted;

  static bool deliveryFailed(AdminAlertEntity alert) =>
      attemptRecorded(alert) && alert.deliveryAttemptStatus == failed;

  static bool deliveryCancelled(AdminAlertEntity alert) =>
      alert.deliveryAttemptStatus == cancelled;
}

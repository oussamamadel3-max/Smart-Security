/// Admin alert as returned by the alerts API.
///
/// [isRead] and [readAt] stay null when the payload has no read receipt.
class AdminAlertEntity {
  const AdminAlertEntity({
    required this.adminAlertId,
    required this.senderUserId,
    required this.recipientUserId,
    required this.title,
    required this.body,
    required this.createdAt,
    required this.deliveryAttemptStatus,
    required this.deliveryAttempted,
    this.notificationId,
    this.notificationStatus,
    this.lastAttemptAt,
    this.isRead,
    this.readAt,
  });

  final String adminAlertId;
  final String senderUserId;
  final String recipientUserId;
  final String title;
  final String body;
  final DateTime createdAt;
  final String? notificationId;
  final String? notificationStatus;
  final String deliveryAttemptStatus;
  final bool deliveryAttempted;
  final DateTime? lastAttemptAt;

  /// Explicit server flag. Null means the field was absent.
  final bool? isRead;

  /// Explicit server timestamp. Null means the field was absent or empty.
  final DateTime? readAt;

  bool get hasReadEvidence => isRead != null || readAt != null;

  /// True only when the server payload itself carries read evidence.
  bool get readClaimed => isRead == true || readAt != null;
}

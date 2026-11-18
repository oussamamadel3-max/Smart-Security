/// Create payload for `POST /api/admin/alerts`. No push token is included.
class CreateAdminAlertInput {
  const CreateAdminAlertInput({
    required this.recipientUserId,
    required this.title,
    required this.body,
  });

  final String recipientUserId;
  final String title;
  final String body;
}

abstract final class AdminAlertRules {
  static const maxTitle = 160;
  static const maxBody = 4000;
  static final guid = RegExp(
    r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$',
  );

  static String? validate({
    required String recipientUserId,
    required String title,
    required String body,
  }) {
    if (!guid.hasMatch(recipientUserId.trim())) return 'recipientUserId';
    final normalizedTitle = title.trim();
    if (normalizedTitle.isEmpty || normalizedTitle.length > maxTitle) {
      return 'title';
    }
    final normalizedBody = body.trim();
    if (normalizedBody.isEmpty || normalizedBody.length > maxBody) {
      return 'body';
    }
    return null;
  }
}

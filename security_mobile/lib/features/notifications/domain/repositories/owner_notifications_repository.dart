import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../data/models/owner_notification_dto.dart';

abstract class OwnerNotificationsRepository {
  Future<Result<List<OwnerNotificationListItemDto>>> listMine();

  Future<Result<OwnerNotificationDetailDto>> getById(String notificationId);
}

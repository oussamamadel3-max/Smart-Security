import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../domain/repositories/owner_notifications_repository.dart';
import '../datasources/owner_notifications_remote_datasource.dart';
import '../models/owner_notification_dto.dart';

class OwnerNotificationsRepositoryImpl
    implements OwnerNotificationsRepository {
  OwnerNotificationsRepositoryImpl(this._remote);

  final OwnerNotificationsRemoteDataSource _remote;

  @override
  Future<Result<List<OwnerNotificationListItemDto>>> listMine() {
    return executeWithRetry(_remote.listMine);
  }

  @override
  Future<Result<OwnerNotificationDetailDto>> getById(
    String notificationId,
  ) {
    return executeWithRetry(() => _remote.getById(notificationId));
  }
}

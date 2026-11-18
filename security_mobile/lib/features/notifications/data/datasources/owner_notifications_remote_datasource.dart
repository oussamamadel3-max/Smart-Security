import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/network/app_endpoints.dart';
import '../models/owner_notification_dto.dart';

class OwnerNotificationsRemoteDataSource extends ApiClient {
  OwnerNotificationsRemoteDataSource(super.dio);

  Future<Result<List<OwnerNotificationListItemDto>>> listMine() {
    return get(
      AppEndpoints.ownerNotifications,
      parser: (json) {
        if (json is! List) {
          throw const FormatException('Expected notifications list');
        }
        return json
            .whereType<Map>()
            .map(
              (e) => OwnerNotificationListItemDto.fromJson(
                Map<String, dynamic>.from(e),
              ),
            )
            .toList(growable: false);
      },
    );
  }

  Future<Result<OwnerNotificationDetailDto>> getById(String notificationId) {
    return get(
      AppEndpoints.ownerNotification(notificationId),
      parser: (json) => OwnerNotificationDetailDto.fromJson(_asMap(json)),
    );
  }

  Map<String, dynamic> _asMap(dynamic json) {
    if (json is Map<String, dynamic>) return json;
    if (json is Map) return Map<String, dynamic>.from(json);
    throw const FormatException('Expected JSON object');
  }
}

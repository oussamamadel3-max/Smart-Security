import '../../domain/entities/admin_alert_entity.dart';
import '../../domain/entities/audit_log_entity.dart';
import '../../domain/entities/paged_page.dart';
import 'admin_json.dart';

abstract final class AuditAlertMapper {
  static PagedPage<AuditLogEntity> auditPage(Map<String, dynamic> json) {
    return _page(json, auditLog);
  }

  static PagedPage<AdminAlertEntity> alertPage(Map<String, dynamic> json) {
    return _page(json, adminAlert);
  }

  static AuditLogEntity auditLog(Map<String, dynamic> json) {
    return AuditLogEntity(
      id: readScalarId(json['id']),
      actorUserId: readOptionalString(json['actorUserId']),
      action: readString(json['action']),
      targetType: readString(json['targetType']),
      targetId: readOptionalString(json['targetId']),
      deviceId: readOptionalString(json['deviceId']),
      result: readString(json['result']),
      occurredAt: readRequiredDateTime(json['occurredAt']),
      details: readOptionalString(json['details']),
    );
  }

  static AdminAlertEntity adminAlert(Map<String, dynamic> json) {
    return AdminAlertEntity(
      adminAlertId: readString(json['adminAlertId']),
      senderUserId: readString(json['senderUserId']),
      recipientUserId: readString(json['recipientUserId']),
      title: readString(json['title']),
      body: readString(json['body']),
      createdAt: readRequiredDateTime(json['createdAt']),
      notificationId: readOptionalString(json['notificationId']),
      notificationStatus: readOptionalString(json['notificationStatus']),
      deliveryAttemptStatus: readString(json['deliveryAttemptStatus']),
      deliveryAttempted: readBool(json['deliveryAttempted']),
      lastAttemptAt: readDateTime(json['lastAttemptAt']),
      isRead: _optionalBool(json, 'isRead'),
      readAt: json.containsKey('readAt') ? readDateTime(json['readAt']) : null,
    );
  }

  static PagedPage<T> _page<T>(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic> json) mapItem,
  ) {
    return PagedPage<T>(
      items: readMapList(json['items']).map(mapItem).toList(),
      page: readInt(json['page']),
      pageSize: readInt(json['pageSize']),
      totalCount: readInt(json['totalCount']),
    );
  }

  static bool? _optionalBool(Map<String, dynamic> json, String key) {
    if (!json.containsKey(key) || json[key] == null) return null;
    return json[key] == true;
  }
}

String readScalarId(dynamic value) {
  if (value is String) return value.trim();
  if (value is int) return value.toString();
  if (value is num) return value.toInt().toString();
  return '';
}

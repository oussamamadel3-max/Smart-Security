/// One row from `GET /api/admin/audit-logs`.
class AuditLogEntity {
  const AuditLogEntity({
    required this.id,
    required this.action,
    required this.targetType,
    required this.result,
    required this.occurredAt,
    this.actorUserId,
    this.targetId,
    this.deviceId,
    this.details,
  });

  final String id;
  final String? actorUserId;
  final String action;
  final String targetType;
  final String? targetId;
  final String? deviceId;
  final String result;
  final DateTime occurredAt;
  final String? details;
}

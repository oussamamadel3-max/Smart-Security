/// Query accepted by `GET /api/admin/audit-logs`.
class AuditLogQuery {
  const AuditLogQuery({
    this.action,
    this.targetType,
    this.actorUserId,
    this.deviceId,
    this.targetId,
    this.result,
    this.from,
    this.to,
    this.q,
    this.page = 1,
    this.pageSize = 20,
    this.sortBy = 'occurredAt',
    this.sortDir = 'desc',
  });

  static const allowedSortBy = [
    'occurredAt',
    'action',
    'result',
    'actorUserId',
    'deviceId',
  ];

  final String? action;
  final String? targetType;
  final String? actorUserId;
  final String? deviceId;
  final String? targetId;
  final String? result;
  final DateTime? from;
  final DateTime? to;
  final String? q;
  final int page;
  final int pageSize;
  final String sortBy;
  final String sortDir;

  AuditLogQuery copyWith({
    String? action,
    String? targetType,
    String? actorUserId,
    String? deviceId,
    String? targetId,
    String? result,
    DateTime? from,
    DateTime? to,
    String? q,
    int? page,
    int? pageSize,
    String? sortBy,
    String? sortDir,
    bool clearAction = false,
    bool clearTargetType = false,
    bool clearActor = false,
    bool clearDevice = false,
    bool clearTarget = false,
    bool clearResult = false,
    bool clearFrom = false,
    bool clearTo = false,
    bool clearQ = false,
  }) {
    return AuditLogQuery(
      action: clearAction ? null : (action ?? this.action),
      targetType: clearTargetType ? null : (targetType ?? this.targetType),
      actorUserId: clearActor ? null : (actorUserId ?? this.actorUserId),
      deviceId: clearDevice ? null : (deviceId ?? this.deviceId),
      targetId: clearTarget ? null : (targetId ?? this.targetId),
      result: clearResult ? null : (result ?? this.result),
      from: clearFrom ? null : (from ?? this.from),
      to: clearTo ? null : (to ?? this.to),
      q: clearQ ? null : (q ?? this.q),
      page: page ?? this.page,
      pageSize: pageSize ?? this.pageSize,
      sortBy: sortBy ?? this.sortBy,
      sortDir: sortDir ?? this.sortDir,
    );
  }

  Map<String, dynamic> toQueryParameters() {
    final params = <String, dynamic>{
      'page': page < 1 ? 1 : page,
      'pageSize': pageSize < 1 ? 20 : pageSize,
      'sortBy': allowedSortBy.contains(sortBy) ? sortBy : 'occurredAt',
      'sortDir': sortDir == 'asc' ? 'asc' : 'desc',
    };
    _put(params, 'action', action);
    _put(params, 'targetType', targetType);
    _put(params, 'actorUserId', actorUserId);
    _put(params, 'deviceId', deviceId);
    _put(params, 'targetId', targetId);
    _put(params, 'result', result);
    _put(params, 'q', q);
    if (from != null) params['from'] = from!.toUtc().toIso8601String();
    if (to != null) params['to'] = to!.toUtc().toIso8601String();
    return params;
  }

  static void _put(Map<String, dynamic> params, String key, String? value) {
    final text = value?.trim();
    if (text != null && text.isNotEmpty) params[key] = text;
  }
}

abstract final class AuditLogQueryRules {
  static final guid = RegExp(
    r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$',
  );

  static String? optionalGuid(String raw) {
    final text = raw.trim();
    if (text.isEmpty) return null;
    return guid.hasMatch(text) ? null : 'guid';
  }

  static DateTime? optionalInstant(String raw) {
    final text = raw.trim();
    if (text.isEmpty) return null;
    return DateTime.tryParse(text);
  }
}

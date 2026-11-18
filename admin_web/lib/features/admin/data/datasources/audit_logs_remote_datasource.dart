import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/network/app_endpoints.dart';
import '../../domain/audit_log_query.dart';
import '../mappers/admin_json.dart';

class AuditLogsRemoteDataSource extends ApiClient {
  AuditLogsRemoteDataSource(super.dio);

  Future<Result<Map<String, dynamic>>> query(AuditLogQuery query) {
    return get(
      AppEndpoints.adminAuditLogs,
      queryParameters: query.toQueryParameters(),
      parser: readMap,
    );
  }
}

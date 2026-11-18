import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../audit_log_query.dart';
import '../entities/audit_log_entity.dart';
import '../entities/paged_page.dart';

abstract class AuditLogsRepository {
  Future<Result<PagedPage<AuditLogEntity>>> query(AuditLogQuery query);
}

import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../audit_log_query.dart';
import '../entities/audit_log_entity.dart';
import '../entities/paged_page.dart';
import '../repositories/audit_logs_repository.dart';

class QueryAuditLogsUseCase {
  const QueryAuditLogsUseCase(this._repository);

  final AuditLogsRepository _repository;

  Future<Result<PagedPage<AuditLogEntity>>> call(AuditLogQuery query) =>
      _repository.query(query);
}

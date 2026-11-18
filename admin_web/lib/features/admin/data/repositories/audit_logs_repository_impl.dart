import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../domain/audit_log_query.dart';
import '../../domain/entities/audit_log_entity.dart';
import '../../domain/entities/paged_page.dart';
import '../../domain/repositories/audit_logs_repository.dart';
import '../datasources/audit_logs_remote_datasource.dart';
import '../mappers/audit_alert_mapper.dart';

class AuditLogsRepositoryImpl implements AuditLogsRepository {
  AuditLogsRepositoryImpl(this._remote);

  final AuditLogsRemoteDataSource _remote;

  @override
  Future<Result<PagedPage<AuditLogEntity>>> query(AuditLogQuery query) async {
    final result = await executeWithRetry(() => _remote.query(query));
    return switch (result) {
      Failure_(:final failure) => Failure_(failure),
      Success(:final data) => Success(AuditAlertMapper.auditPage(data)),
    };
  }
}

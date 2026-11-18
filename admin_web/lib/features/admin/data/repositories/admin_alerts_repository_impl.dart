import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../domain/admin_alert_query.dart';
import '../../domain/create_admin_alert_input.dart';
import '../../domain/entities/admin_alert_entity.dart';
import '../../domain/entities/paged_page.dart';
import '../../domain/repositories/admin_alerts_repository.dart';
import '../datasources/admin_alerts_remote_datasource.dart';
import '../mappers/audit_alert_mapper.dart';

class AdminAlertsRepositoryImpl implements AdminAlertsRepository {
  AdminAlertsRepositoryImpl(this._remote);

  final AdminAlertsRemoteDataSource _remote;

  @override
  Future<Result<PagedPage<AdminAlertEntity>>> list(
    AdminAlertQuery query,
  ) async {
    final result = await executeWithRetry(() => _remote.list(query));
    return switch (result) {
      Failure_(:final failure) => Failure_(failure),
      Success(:final data) => Success(AuditAlertMapper.alertPage(data)),
    };
  }

  @override
  Future<Result<AdminAlertEntity>> getById(String alertId) async {
    final result = await executeWithRetry(() => _remote.getById(alertId));
    return _one(result);
  }

  @override
  Future<Result<AdminAlertEntity>> create(CreateAdminAlertInput input) async {
    final result = await _remote.create(input);
    return _one(result);
  }

  Result<AdminAlertEntity> _one(Result<Map<String, dynamic>> result) {
    return switch (result) {
      Failure_(:final failure) => Failure_(failure),
      Success(:final data) => Success(AuditAlertMapper.adminAlert(data)),
    };
  }
}

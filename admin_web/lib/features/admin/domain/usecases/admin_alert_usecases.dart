import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../admin_alert_query.dart';
import '../create_admin_alert_input.dart';
import '../entities/admin_alert_entity.dart';
import '../entities/paged_page.dart';
import '../repositories/admin_alerts_repository.dart';

class ListAdminAlertsUseCase {
  const ListAdminAlertsUseCase(this._repository);

  final AdminAlertsRepository _repository;

  Future<Result<PagedPage<AdminAlertEntity>>> call(AdminAlertQuery query) =>
      _repository.list(query);
}

class GetAdminAlertUseCase {
  const GetAdminAlertUseCase(this._repository);

  final AdminAlertsRepository _repository;

  Future<Result<AdminAlertEntity>> call(String alertId) =>
      _repository.getById(alertId);
}

class CreateAdminAlertUseCase {
  const CreateAdminAlertUseCase(this._repository);

  final AdminAlertsRepository _repository;

  Future<Result<AdminAlertEntity>> call(CreateAdminAlertInput input) =>
      _repository.create(input);
}

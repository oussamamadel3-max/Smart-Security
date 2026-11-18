import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../admin_alert_query.dart';
import '../create_admin_alert_input.dart';
import '../entities/admin_alert_entity.dart';
import '../entities/paged_page.dart';

abstract class AdminAlertsRepository {
  Future<Result<PagedPage<AdminAlertEntity>>> list(AdminAlertQuery query);

  Future<Result<AdminAlertEntity>> getById(String alertId);

  Future<Result<AdminAlertEntity>> create(CreateAdminAlertInput input);
}

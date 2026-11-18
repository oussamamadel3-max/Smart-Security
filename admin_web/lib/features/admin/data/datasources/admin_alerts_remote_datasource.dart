import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/network/app_endpoints.dart';
import '../../domain/admin_alert_query.dart';
import '../../domain/create_admin_alert_input.dart';
import '../mappers/admin_json.dart';

class AdminAlertsRemoteDataSource extends ApiClient {
  AdminAlertsRemoteDataSource(super.dio);

  Future<Result<Map<String, dynamic>>> list(AdminAlertQuery query) {
    return get(
      AppEndpoints.adminAlerts,
      queryParameters: query.toQueryParameters(),
      parser: readMap,
    );
  }

  Future<Result<Map<String, dynamic>>> getById(String alertId) {
    return get(AppEndpoints.adminAlert(alertId), parser: readMap);
  }

  Future<Result<Map<String, dynamic>>> create(CreateAdminAlertInput input) {
    return post(
      AppEndpoints.adminAlerts,
      data: {
        'recipientUserId': input.recipientUserId.trim(),
        'title': input.title.trim(),
        'body': input.body.trim(),
      },
      parser: readMap,
    );
  }
}

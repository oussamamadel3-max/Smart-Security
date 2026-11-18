import 'package:smart_security_app_core/smart_security_app_core.dart';

abstract class PushTokensRepository {
  Future<Result<String>> register({
    required String token,
    required String platform,
  });

  Future<Result<void>> revokeStoredOrAll();

  Future<void> clearLocalId();
}

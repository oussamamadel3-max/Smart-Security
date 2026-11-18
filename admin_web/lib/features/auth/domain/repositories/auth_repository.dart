import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../entities/auth_session_tokens.dart';
import '../entities/auth_user_entity.dart';

abstract class AuthRepository {
  Future<Result<AuthSessionTokens>> login({
    required String userName,
    required String password,
  });

  /// Cookie-channel refresh: HttpOnly cookie + CSRF header (no body token).
  Future<Result<AuthSessionTokens>> refresh();

  Future<Result<AuthUserEntity>> me();

  Future<Result<bool>> logout();

  Future<Result<bool>> logoutAll();
}

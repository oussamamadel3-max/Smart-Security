import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../entities/auth_session_tokens.dart';
import '../entities/auth_user_entity.dart';

abstract class AuthRepository {
  Future<Result<AuthSessionTokens>> login({
    required String userName,
    required String password,
  });

  Future<Result<AuthSessionTokens>> refresh({required String refreshToken});

  Future<Result<AuthUserEntity>> me();

  Future<Result<bool>> logout({String? refreshToken});

  Future<Result<bool>> logoutAll();
}

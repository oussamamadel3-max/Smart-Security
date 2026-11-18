import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../entities/auth_session_tokens.dart';
import '../repositories/auth_repository.dart';

class LoginUseCase {
  const LoginUseCase(this._repository);

  final AuthRepository _repository;

  Future<Result<AuthSessionTokens>> call({
    required String userName,
    required String password,
  }) {
    return _repository.login(userName: userName, password: password);
  }
}

import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../entities/auth_session_tokens.dart';
import '../repositories/auth_repository.dart';

class RestoreSessionUseCase {
  const RestoreSessionUseCase(this._repository);

  final AuthRepository _repository;

  Future<Result<AuthSessionTokens>> call({required String refreshToken}) {
    return _repository.refresh(refreshToken: refreshToken);
  }
}

import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../repositories/auth_repository.dart';

class LogoutUseCase {
  const LogoutUseCase(this._repository);

  final AuthRepository _repository;

  Future<Result<bool>> call({String? refreshToken}) {
    return _repository.logout(refreshToken: refreshToken);
  }
}

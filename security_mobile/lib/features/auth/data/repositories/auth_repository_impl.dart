import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../domain/entities/auth_session_tokens.dart';
import '../../domain/entities/auth_user_entity.dart';
import '../../domain/repositories/auth_repository.dart';
import '../datasources/auth_remote_datasource.dart';
import '../mappers/auth_mapper.dart';
import '../models/auth_tokens_dto.dart';

class AuthRepositoryImpl implements AuthRepository {
  AuthRepositoryImpl({required AuthRemoteDataSource remote}) : _remote = remote;

  final AuthRemoteDataSource _remote;

  @override
  Future<Result<AuthSessionTokens>> login({
    required String userName,
    required String password,
  }) async {
    final result = await _remote.login(
      userName: userName,
      password: password,
    );
    return switch (result) {
      Success(:final data) => _mapTokens(data),
      Failure_(:final failure) => Failure_(failure),
    };
  }

  @override
  Future<Result<AuthSessionTokens>> refresh({
    required String refreshToken,
  }) async {
    final result = await _remote.refresh(refreshToken: refreshToken);
    return switch (result) {
      Success(:final data) => _mapTokens(data),
      Failure_(:final failure) => Failure_(failure),
    };
  }

  @override
  Future<Result<AuthUserEntity>> me() async {
    final result = await _remote.me();
    return switch (result) {
      Success(:final data) => Success(AuthMapper.toUser(data)),
      Failure_(:final failure) => Failure_(failure),
    };
  }

  @override
  Future<Result<bool>> logout({String? refreshToken}) {
    return _remote.logout(refreshToken: refreshToken);
  }

  @override
  Future<Result<bool>> logoutAll() => _remote.logoutAll();

  Result<AuthSessionTokens> _mapTokens(AuthTokensDto data) {
    final tokens = AuthMapper.toSession(data);
    if (tokens.accessToken.isEmpty) {
      return const Failure_(
        Failure.parsing(message: 'استجابة المصادقة ناقصة'),
      );
    }
    if (tokens.refreshToken.isEmpty) {
      return const Failure_(
        Failure.parsing(
          message: 'قناة التحديث body تتطلب refreshToken في الاستجابة',
        ),
      );
    }
    return Success(tokens);
  }
}

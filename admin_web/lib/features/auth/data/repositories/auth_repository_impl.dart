import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/network/web_auth_constants.dart';
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
    final result = await _remote.login(userName: userName, password: password);
    return switch (result) {
      Success(:final data) => _mapTokens(data),
      Failure_(:final failure) => Failure_(failure),
    };
  }

  @override
  Future<Result<AuthSessionTokens>> refresh() async {
    final result = await _remote.refresh();
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
  Future<Result<bool>> logout() => _remote.logout();

  @override
  Future<Result<bool>> logoutAll() => _remote.logoutAll();

  Result<AuthSessionTokens> _mapTokens(AuthTokensDto data) {
    final tokens = AuthMapper.toSession(data);
    if (tokens.accessToken.isEmpty) {
      return const Failure_(Failure.parsing(message: 'استجابة المصادقة ناقصة'));
    }

    final channel = tokens.refreshChannel.toUpperCase();
    if (channel == WebAuthConstants.refreshChannelCookie) {
      // Cookie channel must not expose refresh in the JSON body.
      if (data.refreshToken != null && data.refreshToken!.isNotEmpty) {
        return const Failure_(
          Failure.parsing(
            message: 'قناة Cookie لا يجوز أن تعيد refreshToken في الجسم',
          ),
        );
      }
      return Success(tokens);
    }

    // Unexpected channel for this web client — reject rather than store body.
    return const Failure_(
      Failure.parsing(message: 'قناة التحديث غير مدعومة في عميل الويب'),
    );
  }
}

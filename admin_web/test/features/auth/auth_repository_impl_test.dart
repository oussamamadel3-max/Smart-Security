import 'package:admin_web/core/session/access_token_holder.dart';
import 'package:admin_web/features/auth/data/datasources/auth_remote_datasource.dart';
import 'package:admin_web/features/auth/data/models/auth_tokens_dto.dart';
import 'package:admin_web/features/auth/data/models/me_user_dto.dart';
import 'package:admin_web/features/auth/data/repositories/auth_repository_impl.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

class _ScriptedAuthRemote extends AuthRemoteDataSource {
  _ScriptedAuthRemote(this.tokens)
    : super(bareDio: Dio(), accessTokenHolder: AccessTokenHolder());

  final Result<AuthTokensDto> tokens;

  @override
  Future<Result<AuthTokensDto>> login({
    required String userName,
    required String password,
  }) async => tokens;
}

AuthTokensDto _dto({
  required String accessToken,
  String? refreshToken,
  String refreshChannel = 'COOKIE',
}) {
  return AuthTokensDto(
    accessToken: accessToken,
    tokenType: 'Bearer',
    expiresIn: 60,
    refreshChannel: refreshChannel,
    refreshToken: refreshToken,
    user: const MeUserDto(id: 'u1', userName: 'owner1'),
  );
}

void main() {
  test('empty access token is a malformed auth response', () async {
    final repo = AuthRepositoryImpl(
      remote: _ScriptedAuthRemote(Success(_dto(accessToken: ''))),
    );
    final result = await repo.login(userName: 'owner1', password: 'secret');
    expect(result.isFailure, isTrue);
  });

  test('cookie channel rejects a refresh token in the body', () async {
    final repo = AuthRepositoryImpl(
      remote: _ScriptedAuthRemote(
        Success(_dto(accessToken: 'header.payload.sig', refreshToken: 'raw')),
      ),
    );
    final result = await repo.login(userName: 'owner1', password: 'secret');
    expect(result.isFailure, isTrue);
  });

  test('non-cookie refresh channel is rejected', () async {
    final repo = AuthRepositoryImpl(
      remote: _ScriptedAuthRemote(
        Success(
          _dto(accessToken: 'header.payload.sig', refreshChannel: 'BODY'),
        ),
      ),
    );
    final result = await repo.login(userName: 'owner1', password: 'secret');
    expect(result.isFailure, isTrue);
  });
}

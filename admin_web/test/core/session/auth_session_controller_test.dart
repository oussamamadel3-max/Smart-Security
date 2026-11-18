import 'dart:async';
import 'dart:convert';

import 'package:admin_web/core/network/auth_dependency_providers.dart';
import 'package:admin_web/core/session/access_token_holder.dart';
import 'package:admin_web/core/session/auth_session_controller.dart';
import 'package:admin_web/core/session/auth_session_state.dart';
import 'package:admin_web/core/session/csrf_token_reader.dart';
import 'package:admin_web/features/auth/domain/entities/auth_session_tokens.dart';
import 'package:admin_web/features/auth/domain/entities/auth_user_entity.dart';
import 'package:admin_web/features/auth/domain/repositories/auth_repository.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

String _jwtWithRoles(List<String> roles) {
  final header = base64Url
      .encode(utf8.encode('{"alg":"none","typ":"JWT"}'))
      .replaceAll('=', '');
  final payload = base64Url
      .encode(utf8.encode(jsonEncode({'role': roles})))
      .replaceAll('=', '');
  return '$header.$payload.sig';
}

class _FakeAuthRepository implements AuthRepository {
  _FakeAuthRepository({this.loginResult, this.refreshResult});

  Result<AuthSessionTokens>? loginResult;
  Result<AuthSessionTokens>? refreshResult;
  int refreshCalls = 0;

  @override
  Future<Result<AuthSessionTokens>> login({
    required String userName,
    required String password,
  }) async => loginResult!;

  @override
  Future<Result<bool>> logout() async => const Success(true);

  @override
  Future<Result<bool>> logoutAll() async => const Success(true);

  @override
  Future<Result<AuthUserEntity>> me() async =>
      const Failure_(Failure.unexpected());

  @override
  Future<Result<AuthSessionTokens>> refresh() async {
    refreshCalls += 1;
    return refreshResult ?? const Failure_(Failure.unauthorized());
  }
}

class _WaitingRefreshRepository implements AuthRepository {
  _WaitingRefreshRepository(this.gate);

  final Completer<Result<AuthSessionTokens>> gate;

  @override
  Future<Result<AuthSessionTokens>> login({
    required String userName,
    required String password,
  }) async => const Failure_(Failure.unexpected());

  @override
  Future<Result<bool>> logout() async => const Success(true);

  @override
  Future<Result<bool>> logoutAll() async => const Success(true);

  @override
  Future<Result<AuthUserEntity>> me() async =>
      const Failure_(Failure.unexpected());

  @override
  Future<Result<AuthSessionTokens>> refresh() => gate.future;
}

AuthSessionTokens _tokens({required List<String> roles, String? access}) {
  final jwt = access ?? _jwtWithRoles(roles);
  return AuthSessionTokens(
    accessToken: jwt,
    expiresIn: 60,
    refreshChannel: 'COOKIE',
    user: AuthUserEntity(id: 'u1', userName: 'user1', roles: roles),
  );
}

void main() {
  test('login stores access in memory only', () async {
    final access = AccessTokenHolder();
    final csrf = MemoryCsrfTokenReader('csrf');
    final repo = _FakeAuthRepository(
      loginResult: Success(_tokens(roles: const ['OWNER'])),
    );

    final container = ProviderContainer(
      overrides: [
        accessTokenHolderProvider.overrideWithValue(access),
        csrfTokenReaderProvider.overrideWithValue(csrf),
        authRepositoryProvider.overrideWithValue(repo),
      ],
    );
    addTearDown(container.dispose);

    final result = await container
        .read(authSessionProvider.notifier)
        .login(userName: 'owner1', password: 'x');

    expect(result, isA<Success<AuthUserEntity>>());
    expect(access.hasAccessToken, isTrue);
    expect(
      container.read(authSessionProvider).status,
      AuthStatus.authenticated,
    );
  });

  test('rejects roles other than OWNER/ADMIN', () async {
    final access = AccessTokenHolder();
    final csrf = MemoryCsrfTokenReader('csrf');
    final repo = _FakeAuthRepository(
      loginResult: Success(_tokens(roles: const ['GUEST'])),
    );

    final container = ProviderContainer(
      overrides: [
        accessTokenHolderProvider.overrideWithValue(access),
        csrfTokenReaderProvider.overrideWithValue(csrf),
        authRepositoryProvider.overrideWithValue(repo),
      ],
    );
    addTearDown(container.dispose);

    final result = await container
        .read(authSessionProvider.notifier)
        .login(userName: 'x', password: 'y');

    expect(result, isA<Failure_<AuthUserEntity>>());
    expect(access.hasAccessToken, isFalse);
    expect(
      container.read(authSessionProvider).status,
      isNot(AuthStatus.authenticated),
    );
  });

  test('accepts ADMIN role for web', () async {
    final access = AccessTokenHolder();
    final csrf = MemoryCsrfTokenReader('csrf');
    final repo = _FakeAuthRepository(
      loginResult: Success(_tokens(roles: const ['ADMIN'])),
    );

    final container = ProviderContainer(
      overrides: [
        accessTokenHolderProvider.overrideWithValue(access),
        csrfTokenReaderProvider.overrideWithValue(csrf),
        authRepositoryProvider.overrideWithValue(repo),
      ],
    );
    addTearDown(container.dispose);

    final result = await container
        .read(authSessionProvider.notifier)
        .login(userName: 'admin1', password: 'x');

    expect(result, isA<Success<AuthUserEntity>>());
    expect(container.read(authSessionProvider).user?.isAdmin, isTrue);
  });

  test('late refresh after logout does not restore session', () async {
    final access = AccessTokenHolder();
    final csrf = MemoryCsrfTokenReader('csrf');
    final gate = Completer<Result<AuthSessionTokens>>();
    final repo = _WaitingRefreshRepository(gate);

    final container = ProviderContainer(
      overrides: [
        accessTokenHolderProvider.overrideWithValue(access),
        csrfTokenReaderProvider.overrideWithValue(csrf),
        authRepositoryProvider.overrideWithValue(repo),
      ],
    );
    addTearDown(container.dispose);

    final session = container.read(authSessionProvider.notifier);
    final refreshFuture = session.refreshSession();
    await session.logout();
    gate.complete(Success(_tokens(roles: const ['OWNER'])));

    final ok = await refreshFuture;
    expect(ok, isFalse);
    expect(access.hasAccessToken, isFalse);
    expect(
      container.read(authSessionProvider).status,
      AuthStatus.unauthenticated,
    );
  });

  test('restore without csrf cookie stays unauthenticated', () async {
    final access = AccessTokenHolder();
    final csrf = MemoryCsrfTokenReader(null);
    final repo = _FakeAuthRepository(
      refreshResult: Success(_tokens(roles: const ['OWNER'])),
    );

    final container = ProviderContainer(
      overrides: [
        accessTokenHolderProvider.overrideWithValue(access),
        csrfTokenReaderProvider.overrideWithValue(csrf),
        authRepositoryProvider.overrideWithValue(repo),
      ],
    );
    addTearDown(container.dispose);

    await container.read(authSessionProvider.notifier).restoreSession();
    expect(repo.refreshCalls, 0);
    expect(
      container.read(authSessionProvider).status,
      AuthStatus.unauthenticated,
    );
  });

  test('markSessionExpired after logout does not revive the session', () async {
    final access = AccessTokenHolder();
    final csrf = MemoryCsrfTokenReader('csrf');
    final repo = _FakeAuthRepository(
      loginResult: Success(_tokens(roles: const ['OWNER'])),
    );
    final container = ProviderContainer(
      overrides: [
        accessTokenHolderProvider.overrideWithValue(access),
        csrfTokenReaderProvider.overrideWithValue(csrf),
        authRepositoryProvider.overrideWithValue(repo),
      ],
    );
    addTearDown(container.dispose);

    final session = container.read(authSessionProvider.notifier);
    await session.login(userName: 'owner1', password: 'x');
    await session.logout();
    final generation = container.read(authSessionProvider).generation;

    session.markSessionExpired();

    expect(
      container.read(authSessionProvider).status,
      AuthStatus.unauthenticated,
    );
    expect(container.read(authSessionProvider).user, isNull);
    expect(container.read(authSessionProvider).generation, generation);
    expect(access.hasAccessToken, isFalse);
  });

  test('markSessionExpired is single-shot', () async {
    final access = AccessTokenHolder();
    final container = ProviderContainer(
      overrides: [
        accessTokenHolderProvider.overrideWithValue(access),
        csrfTokenReaderProvider.overrideWithValue(
          MemoryCsrfTokenReader('csrf'),
        ),
        authRepositoryProvider.overrideWithValue(
          _FakeAuthRepository(
            loginResult: Success(_tokens(roles: const ['OWNER'])),
          ),
        ),
      ],
    );
    addTearDown(container.dispose);

    final session = container.read(authSessionProvider.notifier);
    await session.login(userName: 'owner1', password: 'x');
    session.markSessionExpired();
    final generation = container.read(authSessionProvider).generation;
    session.markSessionExpired();

    expect(
      container.read(authSessionProvider).status,
      AuthStatus.sessionExpired,
    );
    expect(container.read(authSessionProvider).user, isNull);
    expect(container.read(authSessionProvider).generation, generation);
    expect(access.hasAccessToken, isFalse);
  });
}

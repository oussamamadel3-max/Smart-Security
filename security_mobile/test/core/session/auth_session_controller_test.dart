import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/core/network/auth_dependency_providers.dart';
import 'package:security_mobile/core/session/access_token_holder.dart';
import 'package:security_mobile/core/session/auth_session_controller.dart';
import 'package:security_mobile/core/session/auth_session_state.dart';
import 'package:security_mobile/core/session/refresh_token_store.dart';
import 'package:security_mobile/features/auth/domain/entities/auth_session_tokens.dart';
import 'package:security_mobile/features/auth/domain/entities/auth_user_entity.dart';
import 'package:security_mobile/features/auth/domain/repositories/auth_repository.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

class _FakeRefreshStore implements RefreshTokenStore {
  String? value;

  @override
  Future<void> clear() async => value = null;

  @override
  Future<String?> read() async => value;

  @override
  Future<void> write(String token) async => value = token;
}

class _FakeAuthRepository implements AuthRepository {
  _FakeAuthRepository({this.loginResult});

  Result<AuthSessionTokens>? loginResult;

  @override
  Future<Result<AuthSessionTokens>> login({
    required String userName,
    required String password,
  }) async =>
      loginResult!;

  @override
  Future<Result<bool>> logout({String? refreshToken}) async =>
      const Success(true);

  @override
  Future<Result<bool>> logoutAll() async => const Success(true);

  @override
  Future<Result<AuthUserEntity>> me() async =>
      const Failure_(Failure.unexpected());

  @override
  Future<Result<AuthSessionTokens>> refresh({
    required String refreshToken,
  }) async =>
      const Failure_(Failure.unauthorized());
}

class _WaitingRefreshRepository implements AuthRepository {
  _WaitingRefreshRepository(this.gate);

  final Completer<Result<AuthSessionTokens>> gate;

  @override
  Future<Result<AuthSessionTokens>> login({
    required String userName,
    required String password,
  }) async =>
      const Failure_(Failure.unexpected());

  @override
  Future<Result<bool>> logout({String? refreshToken}) async =>
      const Success(true);

  @override
  Future<Result<bool>> logoutAll() async => const Success(true);

  @override
  Future<Result<AuthUserEntity>> me() async =>
      const Failure_(Failure.unexpected());

  @override
  Future<Result<AuthSessionTokens>> refresh({
    required String refreshToken,
  }) {
    return gate.future;
  }
}

AuthSessionTokens _tokens({
  String access = 'access',
  String refresh = 'refresh',
  List<String> roles = const ['OWNER'],
}) {
  return AuthSessionTokens(
    accessToken: access,
    refreshToken: refresh,
    expiresIn: 60,
    refreshChannel: 'body',
    user: AuthUserEntity(
      id: 'u1',
      userName: 'owner1',
      roles: roles,
    ),
  );
}

void main() {
  test('login stores access in memory and refresh in store', () async {
    final access = AccessTokenHolder();
    final refreshStore = _FakeRefreshStore();
    final repo = _FakeAuthRepository(
      loginResult: Success(_tokens(access: 'a1', refresh: 'r1')),
    );

    final container = ProviderContainer(
      overrides: [
        accessTokenHolderProvider.overrideWithValue(access),
        refreshTokenStoreProvider.overrideWithValue(refreshStore),
        authRepositoryProvider.overrideWithValue(repo),
      ],
    );
    addTearDown(container.dispose);

    final result = await container.read(authSessionProvider.notifier).login(
          userName: 'owner1',
          password: 'secret',
        );

    expect(result.isSuccess, isTrue);
    expect(access.accessToken, 'a1');
    expect(refreshStore.value, 'r1');
    expect(
      container.read(authSessionProvider).status,
      AuthStatus.authenticated,
    );
  });

  test('non-owner login is rejected without persisting refresh', () async {
    final access = AccessTokenHolder();
    final refreshStore = _FakeRefreshStore();
    final repo = _FakeAuthRepository(
      loginResult: Success(_tokens(roles: const ['ADMIN'])),
    );

    final container = ProviderContainer(
      overrides: [
        accessTokenHolderProvider.overrideWithValue(access),
        refreshTokenStoreProvider.overrideWithValue(refreshStore),
        authRepositoryProvider.overrideWithValue(repo),
      ],
    );
    addTearDown(container.dispose);

    final result = await container.read(authSessionProvider.notifier).login(
          userName: 'admin',
          password: 'secret',
        );

    expect(result.isFailure, isTrue);
    expect(access.accessToken, isNull);
    expect(refreshStore.value, isNull);
  });

  test('late refresh after logout does not restore tokens', () async {
    final access = AccessTokenHolder()..setToken('old');
    final refreshStore = _FakeRefreshStore()..value = 'r-old';
    final gate = Completer<Result<AuthSessionTokens>>();
    final waitingRepo = _WaitingRefreshRepository(gate);

    final container = ProviderContainer(
      overrides: [
        accessTokenHolderProvider.overrideWithValue(access),
        refreshTokenStoreProvider.overrideWithValue(refreshStore),
        authRepositoryProvider.overrideWithValue(waitingRepo),
      ],
    );
    addTearDown(container.dispose);

    final session = container.read(authSessionProvider.notifier);
    final refreshFuture = session.refreshSession();
    await session.logout();
    gate.complete(Success(_tokens(access: 'revived', refresh: 'r-new')));
    final refreshed = await refreshFuture;

    expect(refreshed, isFalse);
    expect(access.accessToken, isNull);
    expect(refreshStore.value, isNull);
    expect(
      container.read(authSessionProvider).status,
      AuthStatus.unauthenticated,
    );
  });
}

import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/core/network/auth_dependency_providers.dart';
import 'package:security_mobile/core/push/fcm_messaging_port.dart';
import 'package:security_mobile/core/session/access_token_holder.dart';
import 'package:security_mobile/core/session/auth_session_controller.dart';
import 'package:security_mobile/core/session/refresh_token_store.dart';
import 'package:security_mobile/features/auth/domain/entities/auth_session_tokens.dart';
import 'package:security_mobile/features/auth/domain/entities/auth_user_entity.dart';
import 'package:security_mobile/features/auth/domain/repositories/auth_repository.dart';
import 'package:security_mobile/features/notifications/domain/repositories/push_tokens_repository.dart';
import 'package:security_mobile/features/notifications/presentation/providers/notifications_providers.dart';
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
  @override
  Future<Result<AuthSessionTokens>> login({
    required String userName,
    required String password,
  }) async =>
      Success(_tokens(userName));

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

AuthSessionTokens _tokens(String userName) => AuthSessionTokens(
      accessToken: 'access-$userName',
      refreshToken: 'refresh-$userName',
      expiresIn: 60,
      refreshChannel: 'body',
      user: AuthUserEntity(id: userName, userName: userName, roles: const [
        'OWNER',
      ]),
    );

class _FakeMessaging implements FcmMessagingPort {
  String token = 'tok-0000000000000000';
  final _refreshController = StreamController<String>.broadcast();

  @override
  Future<bool> requestPermission() async => true;

  @override
  Future<String?> getToken() async => token;

  @override
  Stream<String> get onTokenRefresh => _refreshController.stream;

  @override
  Future<Map<String, String>?> getInitialMessageData() async => null;

  @override
  Stream<Map<String, String>> get onNotificationOpened => const Stream.empty();

  @override
  Stream<Map<String, String>> get onForegroundMessage => const Stream.empty();

  Future<void> dispose() => _refreshController.close();
}

class _FakePushTokensRepository implements PushTokensRepository {
  int registerCalls = 0;
  int clearLocalIdCalls = 0;
  final List<String> registeredTokens = [];

  /// When set, the FIRST `register()` call awaits this completer instead of
  /// resolving immediately — lets tests simulate a slow in-flight request.
  Completer<Result<String>>? firstCallGate;

  @override
  Future<Result<String>> register({
    required String token,
    required String platform,
  }) async {
    registerCalls++;
    registeredTokens.add(token);
    if (registerCalls == 1 && firstCallGate != null) {
      return firstCallGate!.future;
    }
    return Success('pt-$token-$registerCalls');
  }

  @override
  Future<Result<void>> revokeStoredOrAll() async => const Success<void>(null);

  @override
  Future<void> clearLocalId() async => clearLocalIdCalls++;
}

/// Flushes a short chain of unawaited `async`/`await` steps (the
/// fire-and-forget `syncRegistration()` kicked off by the auth-state
/// listener) without needing `fake_async`.
Future<void> _pump([int cycles = 6]) async {
  for (var i = 0; i < cycles; i++) {
    await Future<void>.delayed(Duration.zero);
  }
}

ProviderContainer _buildContainer({
  required _FakeAuthRepository authRepo,
  required _FakePushTokensRepository pushRepo,
  required _FakeMessaging messaging,
}) {
  final container = ProviderContainer(
    overrides: [
      accessTokenHolderProvider.overrideWithValue(AccessTokenHolder()),
      refreshTokenStoreProvider.overrideWithValue(_FakeRefreshStore()),
      authRepositoryProvider.overrideWithValue(authRepo),
      pushTokensRepositoryProvider.overrideWithValue(pushRepo),
      fcmMessagingPortProvider.overrideWithValue(messaging),
    ],
  );
  return container;
}

void main() {
  group('PushTokenLifecycle dedupe', () {
    test('does not re-register the same token for the same user', () async {
      final pushRepo = _FakePushTokensRepository();
      final messaging = _FakeMessaging();
      final container = _buildContainer(
        authRepo: _FakeAuthRepository(),
        pushRepo: pushRepo,
        messaging: messaging,
      );
      addTearDown(container.dispose);
      addTearDown(messaging.dispose);

      final lifecycle = container.read(pushTokenLifecycleProvider);
      await container
          .read(authSessionProvider.notifier)
          .login(userName: 'alice', password: 'x');
      await _pump();
      // Auto-triggered by the login transition above.
      expect(pushRepo.registerCalls, 1);

      await lifecycle.syncRegistration();
      expect(pushRepo.registerCalls, 1, reason: 'same token+user — deduped');

      messaging.token = 'tok-1111111111111111';
      await lifecycle.syncRegistration();
      expect(
        pushRepo.registerCalls,
        2,
        reason: 'token changed — must register again',
      );
    });
  });

  group('PushTokenLifecycle stale-response guard', () {
    test(
      'a register() response that resolves after the account switched is '
      'discarded and the device re-syncs for the CURRENT user instead',
      () async {
        final pushRepo = _FakePushTokensRepository();
        final messaging = _FakeMessaging();
        final container = _buildContainer(
          authRepo: _FakeAuthRepository(),
          pushRepo: pushRepo,
          messaging: messaging,
        );
        addTearDown(container.dispose);
        addTearDown(messaging.dispose);

        final lifecycle = container.read(pushTokenLifecycleProvider);
        final session = container.read(authSessionProvider.notifier);

        await session.login(userName: 'alice', password: 'x');
        await _pump();
        expect(pushRepo.registerCalls, 1, reason: 'auto-sync on login');

        // Gate the NEXT register() call so it stays in flight while the
        // session below moves on to a different user. A fresh token value
        // is used so the dedupe guard (same token+user already registered)
        // does not short-circuit this call before it reaches register().
        final gate = Completer<Result<String>>();
        pushRepo
          ..registerCalls = 0
          ..firstCallGate = gate;
        messaging.token = 'tok-refreshed-while-alice';

        final inFlight = lifecycle.syncRegistration();
        // Let the in-flight call actually start (reach register()):
        // requestPermission -> getToken -> register, each its own await.
        await _pump();
        expect(pushRepo.registerCalls, 1);

        // Switch accounts on the same device while alice's request is
        // still pending.
        await session.logout();
        await session.login(userName: 'bob', password: 'x');

        // Alice's slow response finally arrives.
        gate.complete(const Success('pt-alice-stale'));
        await inFlight;
        // Let the self-heal re-sync (unawaited: requestPermission ->
        // getToken -> register, each its own await) run to completion.
        for (var i = 0; i < 6; i++) {
          await Future<void>.delayed(Duration.zero);
        }

        expect(
          pushRepo.clearLocalIdCalls,
          greaterThanOrEqualTo(1),
          reason: 'stale result for alice must not stay as the local id',
        );
        expect(
          pushRepo.registerCalls,
          greaterThanOrEqualTo(2),
          reason: 'self-heal must re-register for whoever is current (bob)',
        );

        // A further explicit sync for the current user (bob, same token)
        // must now be deduped, proving the lifecycle settled on bob's
        // registration rather than alice's stale one.
        final callsBeforeExtraSync = pushRepo.registerCalls;
        await lifecycle.syncRegistration();
        expect(pushRepo.registerCalls, callsBeforeExtraSync);
      },
    );
  });
}

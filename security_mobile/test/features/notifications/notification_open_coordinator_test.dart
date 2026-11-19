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
import 'package:security_mobile/features/notifications/data/models/owner_notification_dto.dart';
import 'package:security_mobile/features/notifications/domain/repositories/owner_notifications_repository.dart';
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
      Success(
        AuthSessionTokens(
          accessToken: 'access-$userName',
          refreshToken: 'refresh-$userName',
          expiresIn: 60,
          refreshChannel: 'body',
          user: AuthUserEntity(
            id: userName,
            userName: userName,
            roles: const ['OWNER'],
          ),
        ),
      );

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

/// Minimal fake port — only `onNotificationOpened` is exercised here;
/// everything else is a harmless no-op.
class _FakeMessaging implements FcmMessagingPort {
  final _openedController = StreamController<Map<String, String>>.broadcast();

  @override
  Future<bool> requestPermission() async => true;

  @override
  Future<String?> getToken() async => null;

  @override
  Stream<String> get onTokenRefresh => const Stream.empty();

  @override
  Future<Map<String, String>?> getInitialMessageData() async => null;

  @override
  Stream<Map<String, String>> get onNotificationOpened =>
      _openedController.stream;

  @override
  Stream<Map<String, String>> get onForegroundMessage => const Stream.empty();

  void emitOpened(Map<String, String> data) => _openedController.add(data);

  Future<void> dispose() => _openedController.close();
}

/// Always resolves to a detail with no device/alarm ids, so
/// `OpenNotificationUseCase` maps it to `OpenNotificationUnsupported` —
/// this keeps these tests from needing a real GoRouter (only
/// `OpenNotificationNavigateAlarm` touches the router) while still
/// exercising whether the repository was actually called.
class _FakeNotificationsRepo implements OwnerNotificationsRepository {
  int getByIdCalls = 0;
  final List<String> requestedIds = [];

  @override
  Future<Result<List<OwnerNotificationListItemDto>>> listMine() async =>
      const Success([]);

  @override
  Future<Result<OwnerNotificationDetailDto>> getById(
    String notificationId,
  ) async {
    getByIdCalls++;
    requestedIds.add(notificationId);
    return Success(
      OwnerNotificationDetailDto(
        notificationId: notificationId,
        notificationType: 'ALARM',
        status: 'SENT',
        createdAt: '2026-11-19T10:00:00Z',
      ),
    );
  }
}

Future<void> _pump([int cycles = 6]) async {
  for (var i = 0; i < cycles; i++) {
    await Future<void>.delayed(Duration.zero);
  }
}

ProviderContainer _buildContainer({
  required _FakeMessaging messaging,
  required _FakeNotificationsRepo notificationsRepo,
}) {
  return ProviderContainer(
    overrides: [
      accessTokenHolderProvider.overrideWithValue(AccessTokenHolder()),
      refreshTokenStoreProvider.overrideWithValue(_FakeRefreshStore()),
      authRepositoryProvider.overrideWithValue(_FakeAuthRepository()),
      fcmMessagingPortProvider.overrideWithValue(messaging),
      ownerNotificationsRepositoryProvider.overrideWithValue(
        notificationsRepo,
      ),
    ],
  );
}

void main() {
  group('NotificationOpenCoordinator — bootstrap readiness', () {
    test(
      'a notification tap that arrives before auth restoration settles is '
      'held, then processed once the session status leaves "unknown"',
      () async {
        final messaging = _FakeMessaging();
        final notificationsRepo = _FakeNotificationsRepo();
        final container = _buildContainer(
          messaging: messaging,
          notificationsRepo: notificationsRepo,
        );
        addTearDown(container.dispose);
        addTearDown(messaging.dispose);

        // Constructing the coordinator is analogous to app startup, before
        // SplashPage's post-frame restoreSession() call runs — auth status
        // is still AuthStatus.unknown at this point.
        container.read(notificationOpenCoordinatorProvider);

        messaging.emitOpened({'notificationId': 'n1'});
        await _pump();

        expect(
          notificationsRepo.getByIdCalls,
          0,
          reason: 'must wait for bootstrap, not fail with a premature 401',
        );

        // Bootstrap settles (e.g. restoreSession() finds no session).
        await container
            .read(authSessionProvider.notifier)
            .login(userName: 'alice', password: 'x');
        await _pump();

        expect(notificationsRepo.getByIdCalls, 1);
        expect(notificationsRepo.requestedIds, ['n1']);
      },
    );
  });

  group('NotificationOpenCoordinator — dedupe', () {
    test('the same notificationId is never opened twice', () async {
      final messaging = _FakeMessaging();
      final notificationsRepo = _FakeNotificationsRepo();
      final container = _buildContainer(
        messaging: messaging,
        notificationsRepo: notificationsRepo,
      );
      addTearDown(container.dispose);
      addTearDown(messaging.dispose);

      await container
          .read(authSessionProvider.notifier)
          .login(userName: 'alice', password: 'x');
      container.read(notificationOpenCoordinatorProvider);
      await _pump();

      messaging.emitOpened({'notificationId': 'dup-1'});
      await _pump();
      messaging.emitOpened({'notificationId': 'dup-1'});
      await _pump();

      expect(notificationsRepo.getByIdCalls, 1);
    });

    test('different notificationIds are both opened', () async {
      final messaging = _FakeMessaging();
      final notificationsRepo = _FakeNotificationsRepo();
      final container = _buildContainer(
        messaging: messaging,
        notificationsRepo: notificationsRepo,
      );
      addTearDown(container.dispose);
      addTearDown(messaging.dispose);

      await container
          .read(authSessionProvider.notifier)
          .login(userName: 'alice', password: 'x');
      container.read(notificationOpenCoordinatorProvider);
      await _pump();

      messaging.emitOpened({'notificationId': 'a'});
      await _pump();
      messaging.emitOpened({'notificationId': 'b'});
      await _pump();

      expect(notificationsRepo.getByIdCalls, 2);
      expect(notificationsRepo.requestedIds, ['a', 'b']);
    });
  });
}

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
import 'package:security_mobile/features/notifications/presentation/viewmodels/notifications_list_view_model.dart';
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

class _FakeMessaging implements FcmMessagingPort {
  @override
  Future<bool> requestPermission() async => true;

  @override
  Future<String?> getToken() async => null;

  @override
  Stream<String> get onTokenRefresh => const Stream.empty();

  @override
  Future<Map<String, String>?> getInitialMessageData() async => null;

  @override
  Stream<Map<String, String>> get onNotificationOpened => const Stream.empty();

  @override
  Stream<Map<String, String>> get onForegroundMessage => const Stream.empty();
}

class _FakeNotificationsRepo implements OwnerNotificationsRepository {
  int listMineCalls = 0;

  @override
  Future<Result<List<OwnerNotificationListItemDto>>> listMine() async {
    listMineCalls++;
    return const Success([]);
  }

  @override
  Future<Result<OwnerNotificationDetailDto>> getById(
    String notificationId,
  ) async =>
      const Failure_(Failure.notFound());
}

Future<void> _pump([int cycles = 6]) async {
  for (var i = 0; i < cycles; i++) {
    await Future<void>.delayed(Duration.zero);
  }
}

void main() {
  // `ForegroundAlertCoordinator` reads `rootNavigatorKey.currentContext`,
  // which touches `WidgetsBinding.instance`. Plain `test()` (no widget is
  // pumped here — this suite exercises the dedupe/refresh logic, not the
  // SnackBar UI itself) needs the test binding initialized explicitly.
  TestWidgetsFlutterBinding.ensureInitialized();

  group('ForegroundAlertCoordinator', () {
    test(
      'refreshes the inbox for each distinct notificationId, deduping '
      'a repeated one, and never crashes with no attached UI context',
      () async {
        final notificationsRepo = _FakeNotificationsRepo();
        final container = ProviderContainer(
          overrides: [
            accessTokenHolderProvider.overrideWithValue(AccessTokenHolder()),
            refreshTokenStoreProvider.overrideWithValue(_FakeRefreshStore()),
            authRepositoryProvider.overrideWithValue(_FakeAuthRepository()),
            fcmMessagingPortProvider.overrideWithValue(_FakeMessaging()),
            ownerNotificationsRepositoryProvider.overrideWithValue(
              notificationsRepo,
            ),
          ],
        );
        addTearDown(container.dispose);

        await container
            .read(authSessionProvider.notifier)
            .login(userName: 'alice', password: 'x');

        // Pre-warm the inbox ViewModel so its own build()-time
        // `Future.microtask(load)` has already run before the coordinator
        // triggers any refresh itself — otherwise the very first foreground
        // message would race that initial microtask and double-count.
        container.read(notificationsListViewModelProvider);
        await _pump();

        final coordinator = container.read(foregroundAlertCoordinatorProvider);
        await _pump();
        final callsAfterSetup = notificationsRepo.listMineCalls;

        coordinator.handleForTest({'notificationId': 'n1'});
        await _pump();
        expect(notificationsRepo.listMineCalls, callsAfterSetup + 1);

        // Same id again — must not refresh a second time.
        coordinator.handleForTest({'notificationId': 'n1'});
        await _pump();
        expect(notificationsRepo.listMineCalls, callsAfterSetup + 1);

        // A different id — refreshes again.
        coordinator.handleForTest({'notificationId': 'n2'});
        await _pump();
        expect(notificationsRepo.listMineCalls, callsAfterSetup + 2);
      },
    );
  });
}

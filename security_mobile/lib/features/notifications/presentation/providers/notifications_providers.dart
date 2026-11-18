import 'dart:async';
import 'dart:io' show Platform;

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/push/fcm_messaging_port.dart';
import '../../../../core/router/app_router.dart';
import '../../../../core/router/app_routes.dart';
import '../../../../core/session/auth_session_controller.dart';
import '../../../../core/session/auth_session_state.dart';
import '../../domain/usecases/open_notification_usecase.dart';
import 'push_tokens_providers.dart';

export 'push_tokens_providers.dart';

final fcmMessagingPortProvider = Provider<FcmMessagingPort>((ref) {
  return const NoOpFcmMessaging();
});

/// Coordinates push-token HTTP lifecycle with the auth session.
final pushTokenLifecycleProvider = Provider<PushTokenLifecycle>((ref) {
  final lifecycle = PushTokenLifecycle(ref);
  ref.onDispose(lifecycle.dispose);
  return lifecycle;
});

class PushTokenLifecycle {
  PushTokenLifecycle(this._ref) {
    _authSub = _ref.listen<AuthSessionState>(authSessionProvider, (
      previous,
      next,
    ) {
      if (next.isAuthenticated && previous?.user?.id != next.user?.id) {
        unawaited(syncRegistration());
      }
      if (previous?.isAuthenticated == true && !next.isAuthenticated) {
        unawaited(_ref.read(pushTokensRepositoryProvider).clearLocalId());
      }
    });
    _tokenSub =
        _ref.read(fcmMessagingPortProvider).onTokenRefresh.listen((token) {
      unawaited(_registerToken(token));
    });
  }

  final Ref _ref;
  ProviderSubscription<AuthSessionState>? _authSub;
  StreamSubscription<String>? _tokenSub;

  Future<void> syncRegistration() async {
    final session = _ref.read(authSessionProvider);
    if (!session.isAuthenticated) return;

    final messaging = _ref.read(fcmMessagingPortProvider);
    await messaging.requestPermission();
    final token = await messaging.getToken();
    if (token == null || token.length < 16) return;
    await _registerToken(token);
  }

  Future<Result<void>> revokeBeforeLogout() {
    return _ref.read(pushTokensRepositoryProvider).revokeStoredOrAll();
  }

  Future<void> _registerToken(String token) async {
    if (!_ref.read(authSessionProvider).isAuthenticated) return;
    final platform = Platform.isIOS
        ? 'IOS'
        : Platform.isAndroid
            ? 'ANDROID'
            : 'WEB';
    await _ref.read(pushTokensRepositoryProvider).register(
          token: token,
          platform: platform,
        );
  }

  void dispose() {
    _authSub?.close();
    unawaited(_tokenSub?.cancel());
  }
}

/// Handles notification open → API fetch → route (ids only).
final notificationOpenCoordinatorProvider =
    Provider<NotificationOpenCoordinator>((ref) {
  final coordinator = NotificationOpenCoordinator(ref);
  ref.onDispose(coordinator.dispose);
  return coordinator;
});

class NotificationOpenCoordinator {
  NotificationOpenCoordinator(this._ref) {
    final messaging = _ref.read(fcmMessagingPortProvider);
    unawaited(_handleInitial(messaging));
    _openSub = messaging.onNotificationOpened.listen(_handleData);
  }

  final Ref _ref;
  StreamSubscription<Map<String, String>>? _openSub;

  OpenNotificationResult? lastResult;

  Future<void> handlePayloadForTest(Map<String, String> data) =>
      _handleData(data);

  Future<void> _handleInitial(FcmMessagingPort messaging) async {
    final data = await messaging.getInitialMessageData();
    if (data != null) await _handleData(data);
  }

  Future<void> _handleData(Map<String, String> data) async {
    final result = await _ref.read(openNotificationUseCaseProvider)(data);
    lastResult = result;
    switch (result) {
      case OpenNotificationNavigateAlarm(:final deviceId, :final alarmEventId):
        _ref.read(appRouterProvider).go(
              AppRoutes.deviceAlarmEventDetailPath(deviceId, alarmEventId),
            );
      case OpenNotificationDenied() || OpenNotificationUnsupported():
        break;
    }
  }

  void dispose() {
    unawaited(_openSub?.cancel());
  }
}

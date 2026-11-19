import 'dart:async';
import 'dart:convert';
import 'dart:io' show Platform;

import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/push/fcm_messaging_port.dart';
import '../../../../core/push/firebase_bootstrap.dart';
import '../../../../core/push/firebase_fcm_messaging.dart';
import '../../../../core/push/local_notifications_service.dart';
import '../../../../core/router/app_router.dart';
import '../../../../core/router/app_routes.dart';
import '../../../../core/session/auth_session_controller.dart';
import '../../../../core/session/auth_session_state.dart';
import '../../domain/usecases/open_notification_usecase.dart';
import '../viewmodels/notifications_list_view_model.dart';
import 'push_tokens_providers.dart';

export 'push_tokens_providers.dart';

/// Resolves by platform support only (never by init success/failure) so a
/// Firebase startup error on Android stays visible from the real
/// implementation's own logs instead of silently downgrading to
/// [NoOpFcmMessaging] — FCM-C-03.
final fcmMessagingPortProvider = Provider<FcmMessagingPort>((ref) {
  if (FirebaseBootstrap.isSupportedPlatform) {
    return FirebaseFcmMessaging();
  }
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
        // Do NOT unconditionally clear the stored pushTokenId here:
        // AuthSessionController.logout() already calls
        // revokeStoredOrAll() — which clears it only once the server
        // confirms the revoke, and otherwise keeps it so a later attempt
        // can retry revoking the SAME token (FCM-C-04). Clearing it
        // unconditionally on every unauthenticated transition (including
        // session-expiry, which never calls revoke at all) would silently
        // orphan an unrevoked token on the server with no local record of
        // it to retry.
        _lastRegisteredToken = null;
        _lastRegisteredForUserId = null;
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

  // Dedupe guard — FCM-C-04: avoids a redundant register() call when the
  // token and the owning user have not changed since the last successful
  // registration (e.g. a no-op onTokenRefresh fire).
  String? _lastRegisteredToken;
  String? _lastRegisteredForUserId;

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
    final startSession = _ref.read(authSessionProvider);
    if (!startSession.isAuthenticated) return;
    final userId = startSession.user?.id;

    if (_lastRegisteredToken == token && _lastRegisteredForUserId == userId) {
      return;
    }

    final platform = Platform.isIOS
        ? 'IOS'
        : Platform.isAndroid
            ? 'ANDROID'
            : 'WEB';
    final result = await _ref.read(pushTokensRepositoryProvider).register(
          token: token,
          platform: platform,
        );

    // The auth session may have logged out or switched to another user
    // while the HTTP call above was in flight. A late response must never
    // leave a previous account's pushTokenId stored as if it belonged to
    // whoever is signed in now — self-heal by clearing it and re-syncing
    // for the CURRENT session instead of trusting the stale result.
    if (!_isSameSession(startSession)) {
      _lastRegisteredToken = null;
      _lastRegisteredForUserId = null;
      await _ref.read(pushTokensRepositoryProvider).clearLocalId();
      unawaited(syncRegistration());
      return;
    }

    if (result is Success<String>) {
      _lastRegisteredToken = token;
      _lastRegisteredForUserId = userId;
    }
  }

  bool _isSameSession(AuthSessionState started) {
    final current = _ref.read(authSessionProvider);
    return current.isAuthenticated &&
        current.generation == started.generation &&
        current.user?.id == started.user?.id;
  }

  void dispose() {
    _authSub?.close();
    unawaited(_tokenSub?.cancel());
  }
}

/// Handles notification open → API fetch → route (ids only).
///
/// Two independent signal sources feed the same pipeline — FCM-C-05:
///  1. `FcmMessagingPort` (`onNotificationOpened` / `getInitialMessageData`,
///     built on `FirebaseMessaging.onMessageOpenedApp` / `getInitialMessage`)
///     — only fires for a message Firebase itself displayed, i.e. one with a
///     `notification` block. Kept for forward compatibility; `FcmPayloadBuilder`
///     does not currently send one.
///  2. `LocalNotificationsService` tap / cold-start launch payload — fires
///     for the generic alert THIS app posts for the current data-only
///     payload shape, which is what actually happens in practice today.
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
    _openSub = messaging.onNotificationOpened.listen(_queueOrHandle);

    unawaited(
      LocalNotificationsService.instance.initialize(
        onTap: (payload) => _queueOrHandle(_decodeLocalPayload(payload)),
      ),
    );
    unawaited(_consumeLocalLaunchPayload());

    // Bootstrap readiness (FCM-C-05): SplashPage only calls
    // AuthSessionController.restoreSession() from a post-frame callback,
    // which runs AFTER this coordinator is constructed. A cold-start open
    // request that arrives while the session is still `unknown` must wait
    // for restoration to settle instead of hitting the API with no access
    // token yet and losing the deep link to a false 401.
    _authSub = _ref.listen<AuthSessionState>(authSessionProvider, (
      previous,
      next,
    ) {
      if (previous?.status == AuthStatus.unknown &&
          next.status != AuthStatus.unknown) {
        _flushPending();
      }
    });
  }

  final Ref _ref;
  StreamSubscription<Map<String, String>>? _openSub;
  ProviderSubscription<AuthSessionState>? _authSub;

  Map<String, String>? _pending;
  final Set<String> _handledNotificationIds = {};

  OpenNotificationResult? lastResult;

  Future<void> handlePayloadForTest(Map<String, String> data) =>
      _queueOrHandle(data);

  Future<void> _handleInitial(FcmMessagingPort messaging) async {
    final data = await messaging.getInitialMessageData();
    if (data != null) unawaited(_queueOrHandle(data));
  }

  Future<void> _consumeLocalLaunchPayload() async {
    final payload = await LocalNotificationsService.instance
        .consumeLaunchPayload();
    final data = _decodeLocalPayload(payload);
    if (data != null) unawaited(_queueOrHandle(data));
  }

  Map<String, String>? _decodeLocalPayload(String? payload) {
    if (payload == null || payload.isEmpty) return null;
    try {
      final decoded = jsonDecode(payload);
      if (decoded is Map) return Map<String, String>.from(decoded);
    } catch (error) {
      debugPrint('NotificationOpenCoordinator: bad payload: $error');
    }
    return null;
  }

  Future<void> _queueOrHandle(Map<String, String>? data) async {
    if (data == null) return;
    final notificationId = data['notificationId'];
    // Prevent opening the same notification more than once (cold-start
    // launch details + a live tap signal could otherwise double-fire).
    if (notificationId != null &&
        !_handledNotificationIds.add(notificationId)) {
      return;
    }

    if (_ref.read(authSessionProvider).status == AuthStatus.unknown) {
      _pending = data;
      return;
    }
    await _handleData(data);
  }

  void _flushPending() {
    final data = _pending;
    _pending = null;
    if (data != null) unawaited(_handleData(data));
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
    _authSub?.close();
  }
}

/// In-app alert for a message received while the app is in the foreground —
/// FCM-C-05. Android never auto-displays a data-only payload, and a system
/// tray entry would be redundant while the user is already looking at the
/// app, so this reuses the existing SnackBar pattern (`showMySnackBar`) via
/// the router's root navigator context instead of introducing a new visual.
final foregroundAlertCoordinatorProvider =
    Provider<ForegroundAlertCoordinator>((ref) {
  final coordinator = ForegroundAlertCoordinator(ref);
  ref.onDispose(coordinator.dispose);
  return coordinator;
});

class ForegroundAlertCoordinator {
  ForegroundAlertCoordinator(this._ref) {
    _messageSub = _ref
        .read(fcmMessagingPortProvider)
        .onForegroundMessage
        .listen(_handleForeground);
  }

  final Ref _ref;
  StreamSubscription<Map<String, String>>? _messageSub;
  String? _lastShownNotificationId;

  void handleForTest(Map<String, String> data) => _handleForeground(data);

  void _handleForeground(Map<String, String> data) {
    final notificationId = data['notificationId'];
    // Prevent duplicate display for the same notificationId (e.g. a
    // re-delivered message).
    if (notificationId != null && notificationId == _lastShownNotificationId) {
      return;
    }
    _lastShownNotificationId = notificationId;

    // Refresh the inbox opportunistically so it reflects the new item
    // without the user needing to pull-to-refresh — never blocks on it.
    unawaited(
      _ref.read(notificationsListViewModelProvider.notifier).load(),
    );

    final context = rootNavigatorKey.currentContext;
    if (context == null) return;
    showMySnackBar(
      context,
      message: 'لديك تنبيه جديد — افتح التطبيق للتفاصيل',
      type: MySnackType.info,
    );
  }

  void dispose() {
    unawaited(_messageSub?.cancel());
  }
}

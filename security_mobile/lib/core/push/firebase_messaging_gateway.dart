import 'package:firebase_messaging/firebase_messaging.dart';

/// Narrow seam over the Firebase Messaging SDK.
///
/// [FirebaseFcmMessaging] depends on this instead of calling
/// `FirebaseMessaging.instance` directly, so unit tests can supply a fake
/// implementation without platform channels, a device, or an emulator —
/// FCM-C-03 test requirement.
abstract class FirebaseMessagingGateway {
  Future<AuthorizationStatus> requestPermission();

  Future<String?> getToken();

  Stream<String> get onTokenRefresh;

  Future<RemoteMessage?> getInitialMessage();

  Stream<RemoteMessage> get onMessageOpenedApp;

  Stream<RemoteMessage> get onMessage;
}

class DefaultFirebaseMessagingGateway implements FirebaseMessagingGateway {
  FirebaseMessaging get _messaging => FirebaseMessaging.instance;

  @override
  Future<AuthorizationStatus> requestPermission() async {
    final settings = await _messaging.requestPermission(
      alert: true,
      badge: true,
      sound: true,
    );
    return settings.authorizationStatus;
  }

  // `getToken()` / `onTokenRefresh` are deprecated in firebase_messaging
  // 17.x in favor of `register()` / `onRegistered`, but that new API
  // delivers a Firebase Installation ID (FID) for a "direct-send" flow, not
  // the classic FCM registration token the Backend's `/api/me/push-tokens`
  // contract and `FirebaseFcmProvider.SendAsync` (BE-T-09) already send
  // through the Admin SDK's `token`-targeted `messages:send`. Switching here
  // would silently change what the client registers without integration
  // review of the Backend/Mobile contract, so the deprecated-but-functional
  // classic token APIs are kept deliberately — FCM-C-03 decision.
  @override
  Future<String?> getToken() =>
      // ignore: deprecated_member_use
      _messaging.getToken();

  @override
  Stream<String> get onTokenRefresh =>
      // ignore: deprecated_member_use
      _messaging.onTokenRefresh;

  @override
  Future<RemoteMessage?> getInitialMessage() => _messaging.getInitialMessage();

  @override
  Stream<RemoteMessage> get onMessageOpenedApp =>
      FirebaseMessaging.onMessageOpenedApp;

  @override
  Stream<RemoteMessage> get onMessage => FirebaseMessaging.onMessage;
}

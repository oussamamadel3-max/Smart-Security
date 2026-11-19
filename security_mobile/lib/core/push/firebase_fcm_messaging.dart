import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';

import 'fcm_messaging_port.dart';
import 'firebase_messaging_gateway.dart';

/// Real [FcmMessagingPort] backed by the Firebase Messaging SDK — FCM-C-03.
///
/// Never silently substitutes [NoOpFcmMessaging] on error: resolving to this
/// class on a supported platform is unconditional (see
/// `firebase_bootstrap.dart`). A failure inside any call here is caught,
/// logged without secret/raw-token content, and surfaced to the caller as
/// the port's documented empty/false/null result — the class itself never
/// swaps to a different implementation.
class FirebaseFcmMessaging implements FcmMessagingPort {
  FirebaseFcmMessaging({FirebaseMessagingGateway? gateway})
      : _gateway = gateway ?? DefaultFirebaseMessagingGateway();

  final FirebaseMessagingGateway _gateway;

  @override
  Future<bool> requestPermission() async {
    try {
      final status = await _gateway.requestPermission();
      return status == AuthorizationStatus.authorized ||
          status == AuthorizationStatus.provisional;
    } catch (error) {
      debugPrint('FirebaseFcmMessaging.requestPermission failed: $error');
      return false;
    }
  }

  @override
  Future<String?> getToken() async {
    try {
      final token = await _gateway.getToken();
      if (token == null || token.isEmpty) return null;
      return token;
    } catch (error) {
      debugPrint('FirebaseFcmMessaging.getToken failed: $error');
      return null;
    }
  }

  @override
  Stream<String> get onTokenRefresh => _gateway.onTokenRefresh.handleError(
        (Object error) =>
            debugPrint('FirebaseFcmMessaging.onTokenRefresh error: $error'),
      );

  @override
  Future<Map<String, String>?> getInitialMessageData() async {
    try {
      final message = await _gateway.getInitialMessage();
      if (message == null) return null;
      return Map<String, String>.from(message.data);
    } catch (error) {
      debugPrint('FirebaseFcmMessaging.getInitialMessageData failed: $error');
      return null;
    }
  }

  @override
  Stream<Map<String, String>> get onNotificationOpened => _gateway
      .onMessageOpenedApp
      .map((message) => Map<String, String>.from(message.data))
      .handleError(
        (Object error) => debugPrint(
          'FirebaseFcmMessaging.onNotificationOpened error: $error',
        ),
      );

  @override
  Stream<Map<String, String>> get onForegroundMessage => _gateway.onMessage
      .map((message) => Map<String, String>.from(message.data))
      .handleError(
        (Object error) => debugPrint(
          'FirebaseFcmMessaging.onForegroundMessage error: $error',
        ),
      );
}

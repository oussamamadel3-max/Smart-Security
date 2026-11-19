import 'dart:async';

import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:security_mobile/core/push/firebase_fcm_messaging.dart';
import 'package:security_mobile/core/push/firebase_messaging_gateway.dart';

class _FakeGateway implements FirebaseMessagingGateway {
  AuthorizationStatus permissionResult = AuthorizationStatus.authorized;
  Object? permissionError;

  String? tokenResult = 'fake-token-1234567890';
  Object? tokenError;

  RemoteMessage? initialMessageResult;
  Object? initialMessageError;

  final _tokenRefreshController = StreamController<String>.broadcast();
  final _onMessageOpenedController =
      StreamController<RemoteMessage>.broadcast();
  final _onMessageController = StreamController<RemoteMessage>.broadcast();

  @override
  Future<AuthorizationStatus> requestPermission() async {
    if (permissionError != null) throw permissionError!;
    return permissionResult;
  }

  @override
  Future<String?> getToken() async {
    if (tokenError != null) throw tokenError!;
    return tokenResult;
  }

  @override
  Stream<String> get onTokenRefresh => _tokenRefreshController.stream;

  @override
  Future<RemoteMessage?> getInitialMessage() async {
    if (initialMessageError != null) throw initialMessageError!;
    return initialMessageResult;
  }

  @override
  Stream<RemoteMessage> get onMessageOpenedApp =>
      _onMessageOpenedController.stream;

  @override
  Stream<RemoteMessage> get onMessage => _onMessageController.stream;

  void emitTokenRefresh(String token) =>
      _tokenRefreshController.add(token);

  void emitTokenRefreshError(Object error) =>
      _tokenRefreshController.addError(error);

  void emitMessageOpened(RemoteMessage message) =>
      _onMessageOpenedController.add(message);

  void emitMessageOpenedError(Object error) =>
      _onMessageOpenedController.addError(error);

  void emitMessage(RemoteMessage message) => _onMessageController.add(message);

  void emitMessageError(Object error) => _onMessageController.addError(error);

  Future<void> dispose() async {
    await _tokenRefreshController.close();
    await _onMessageOpenedController.close();
    await _onMessageController.close();
  }
}

RemoteMessage _messageWithData(Map<String, String> data) =>
    RemoteMessage(data: data);

void main() {
  group('FirebaseFcmMessaging.requestPermission', () {
    test('authorized maps to true', () async {
      final gateway = _FakeGateway()
        ..permissionResult = AuthorizationStatus.authorized;
      final port = FirebaseFcmMessaging(gateway: gateway);
      expect(await port.requestPermission(), isTrue);
      await gateway.dispose();
    });

    test('provisional maps to true', () async {
      final gateway = _FakeGateway()
        ..permissionResult = AuthorizationStatus.provisional;
      final port = FirebaseFcmMessaging(gateway: gateway);
      expect(await port.requestPermission(), isTrue);
      await gateway.dispose();
    });

    test('denied maps to false', () async {
      final gateway = _FakeGateway()
        ..permissionResult = AuthorizationStatus.denied;
      final port = FirebaseFcmMessaging(gateway: gateway);
      expect(await port.requestPermission(), isFalse);
      await gateway.dispose();
    });

    test('gateway throwing returns false, never crashes', () async {
      final gateway = _FakeGateway()
        ..permissionError = Exception('platform channel failure');
      final port = FirebaseFcmMessaging(gateway: gateway);
      expect(await port.requestPermission(), isFalse);
      await gateway.dispose();
    });
  });

  group('FirebaseFcmMessaging.getToken', () {
    test('returns the token', () async {
      final gateway = _FakeGateway()..tokenResult = 'abc-token-0000000000';
      final port = FirebaseFcmMessaging(gateway: gateway);
      expect(await port.getToken(), 'abc-token-0000000000');
      await gateway.dispose();
    });

    test('empty token maps to null', () async {
      final gateway = _FakeGateway()..tokenResult = '';
      final port = FirebaseFcmMessaging(gateway: gateway);
      expect(await port.getToken(), isNull);
      await gateway.dispose();
    });

    test('null token stays null', () async {
      final gateway = _FakeGateway()..tokenResult = null;
      final port = FirebaseFcmMessaging(gateway: gateway);
      expect(await port.getToken(), isNull);
      await gateway.dispose();
    });

    test('gateway throwing returns null, never crashes', () async {
      final gateway = _FakeGateway()
        ..tokenError = Exception('SERVICE_NOT_AVAILABLE');
      final port = FirebaseFcmMessaging(gateway: gateway);
      expect(await port.getToken(), isNull);
      await gateway.dispose();
    });
  });

  group('FirebaseFcmMessaging.onTokenRefresh', () {
    test('forwards refreshed tokens', () async {
      final gateway = _FakeGateway();
      final port = FirebaseFcmMessaging(gateway: gateway);
      final events = <String>[];
      final sub = port.onTokenRefresh.listen(events.add);

      gateway.emitTokenRefresh('rotated-token-1');
      await Future<void>.delayed(Duration.zero);

      expect(events, ['rotated-token-1']);
      await sub.cancel();
      await gateway.dispose();
    });

    test('swallows stream errors instead of propagating them', () async {
      final gateway = _FakeGateway();
      final port = FirebaseFcmMessaging(gateway: gateway);
      final events = <String>[];
      var errorSeenByListener = false;
      final sub = port.onTokenRefresh.listen(
        events.add,
        onError: (_) => errorSeenByListener = true,
      );

      gateway.emitTokenRefreshError(Exception('token refresh failed'));
      gateway.emitTokenRefresh('after-error-token');
      await Future<void>.delayed(Duration.zero);

      expect(errorSeenByListener, isFalse);
      expect(events, ['after-error-token']);
      await sub.cancel();
      await gateway.dispose();
    });
  });

  group('FirebaseFcmMessaging.getInitialMessageData', () {
    test('null initial message maps to null', () async {
      final gateway = _FakeGateway()..initialMessageResult = null;
      final port = FirebaseFcmMessaging(gateway: gateway);
      expect(await port.getInitialMessageData(), isNull);
      await gateway.dispose();
    });

    test('maps RemoteMessage.data to a plain string map', () async {
      final gateway = _FakeGateway()
        ..initialMessageResult =
            _messageWithData({'notificationId': 'n1', 'type': 'ALARM'});
      final port = FirebaseFcmMessaging(gateway: gateway);
      final data = await port.getInitialMessageData();
      expect(data, {'notificationId': 'n1', 'type': 'ALARM'});
      await gateway.dispose();
    });

    test('gateway throwing returns null, never crashes', () async {
      final gateway = _FakeGateway()
        ..initialMessageError = Exception('no activity attached yet');
      final port = FirebaseFcmMessaging(gateway: gateway);
      expect(await port.getInitialMessageData(), isNull);
      await gateway.dispose();
    });
  });

  group('FirebaseFcmMessaging.onNotificationOpened', () {
    test('forwards tap payloads as string maps', () async {
      final gateway = _FakeGateway();
      final port = FirebaseFcmMessaging(gateway: gateway);
      final events = <Map<String, String>>[];
      final sub = port.onNotificationOpened.listen(events.add);

      gateway.emitMessageOpened(_messageWithData({'notificationId': 'n2'}));
      await Future<void>.delayed(Duration.zero);

      expect(events, [
        {'notificationId': 'n2'},
      ]);
      await sub.cancel();
      await gateway.dispose();
    });

    test('swallows stream errors instead of propagating them', () async {
      final gateway = _FakeGateway();
      final port = FirebaseFcmMessaging(gateway: gateway);
      final events = <Map<String, String>>[];
      var errorSeenByListener = false;
      final sub = port.onNotificationOpened.listen(
        events.add,
        onError: (_) => errorSeenByListener = true,
      );

      gateway.emitMessageOpenedError(Exception('platform error'));
      gateway.emitMessageOpened(_messageWithData({'notificationId': 'n3'}));
      await Future<void>.delayed(Duration.zero);

      expect(errorSeenByListener, isFalse);
      expect(events, [
        {'notificationId': 'n3'},
      ]);
      await sub.cancel();
      await gateway.dispose();
    });
  });

  group('FirebaseFcmMessaging.onForegroundMessage', () {
    test('forwards foreground data payloads as string maps', () async {
      final gateway = _FakeGateway();
      final port = FirebaseFcmMessaging(gateway: gateway);
      final events = <Map<String, String>>[];
      final sub = port.onForegroundMessage.listen(events.add);

      gateway.emitMessage(_messageWithData({'notificationId': 'fg-1'}));
      await Future<void>.delayed(Duration.zero);

      expect(events, [
        {'notificationId': 'fg-1'},
      ]);
      await sub.cancel();
      await gateway.dispose();
    });

    test('swallows stream errors instead of propagating them', () async {
      final gateway = _FakeGateway();
      final port = FirebaseFcmMessaging(gateway: gateway);
      final events = <Map<String, String>>[];
      var errorSeenByListener = false;
      final sub = port.onForegroundMessage.listen(
        events.add,
        onError: (_) => errorSeenByListener = true,
      );

      gateway.emitMessageError(Exception('platform error'));
      gateway.emitMessage(_messageWithData({'notificationId': 'fg-2'}));
      await Future<void>.delayed(Duration.zero);

      expect(errorSeenByListener, isFalse);
      expect(events, [
        {'notificationId': 'fg-2'},
      ]);
      await sub.cancel();
      await gateway.dispose();
    });
  });
}
